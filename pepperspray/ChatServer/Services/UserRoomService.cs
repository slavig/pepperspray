using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RSG;
using Serilog;
using pepperspray.CIO;
using pepperspray.ChatServer.Game;
using pepperspray.ChatServer.Protocol;
using pepperspray.Utils;
using pepperspray.SharedServices;

namespace pepperspray.ChatServer.Services
{
  internal class UserRoomService: IDIService
  {
    private class ExpelRecord
    {
      internal uint UserId;
      internal UserRoom UserRoom;
      internal DateTime UntilDate;

      internal bool HasExpired()
      {
        return DateTime.Now - this.UntilDate > TimeSpan.Zero;
      }
    }

    private ChatManager server;
    private FriendsService friendsService;
    private CharacterService characterService;
    private LobbyService lobbyService;
    private Configuration config;

    private Dictionary<uint, List<ExpelRecord>> expelRecords = new Dictionary<uint, List<ExpelRecord>>();

    public void Inject()
    {
      this.config = DI.Get<Configuration>();
      this.server = DI.Get<ChatManager>();
      this.characterService = DI.Get<CharacterService>();
      this.friendsService = DI.Get<FriendsService>();
      this.lobbyService = DI.Get<LobbyService>();
    }

    internal bool PlayerCanJoinRoom(PlayerHandle player, UserRoom room)
    {
      lock(this)
      {
        if (this.expelRecords.ContainsKey(player.User.Id))
        {
          foreach (var record in this.expelRecordsFor(player.User.Id))
          {
            if (record.UserRoom.Equals(room) && !record.HasExpired())
            {
              return false;
            }
          }
        }
      }

      switch (room.Access)
      {
        case UserRoom.AccessType.ForAll:
          return true;
        case UserRoom.AccessType.ForFriends:
          try
          {
            var character = this.characterService.Find(room.OwnerId);
            return player.Id == room.OwnerId || this.friendsService.GetFriendIDs(character).Contains(player.Character.Id);
          }
          catch (CharacterService.NotFoundException)
          {
            return false;
          }
        case UserRoom.AccessType.ForGroup:
          var owner = this.server.World.FindPlayerById(room.OwnerId);
          if (owner == null)
          {
            return false;
          }

          return player.CurrentGroup == owner.CurrentGroup;
        default:
          return false;
      }
    }

    internal bool PlayerCanModerateRoom(PlayerHandle player, UserRoom room)
    {
      if (room.OwnerId == player.Id || player.AdminOptions.IsEnabled)
      {
        return true;
      }
      else
      {
        return room.ModeratorNames.Contains(player.Name);
      }
    }

    internal string CleanupName(string name)
    {
      return name.Replace('=', ' ').Replace('|', ' ').Replace('+', ' ');
    }

    internal IPromise<Nothing> OpenRoom(UserRoom room) 
    {
      lock(this.server)
      {
        this.server.World.AddUserRoom(room);
      }

      return Nothing.Resolved();
    }

    internal IPromise<Nothing> CloseRoom(UserRoom room)
    {
      PlayerHandle[] lobbyPlayers = new PlayerHandle[] { };

      lock (this.server)
      {
        var lobby = this.server.World.FindLobby(room.Identifier);
        if (lobby != null)
        {
          lobbyPlayers = lobby.Players.ToArray();
        }

        this.server.World.RemoveUserRoom(room.Identifier);
      }

      if (lobbyPlayers.Count() > 0)
      {
        return new CombinedPromise<Nothing>(lobbyPlayers.Select(a => a.Stream.Write(Responses.UserRoomClosed(room))));
      }
      else
      {
        return Nothing.Resolved();
      }
    }

    internal IPromise<Nothing> ListRooms(PlayerHandle sender)
    {
      var list = new List<UserRoom>();

      List<UserRoom> rooms;
      lock (this.server)
      {
        rooms = new List<UserRoom>(this.server.World.UserRooms);
      }

      foreach (var room in rooms)
      {
        if (this.PlayerCanJoinRoom(sender, room))
        {
          list.Add(room);
        }
      }

      list.Sort((a, b) => a.IsPermanent.CompareTo(b.IsPermanent));

      return sender.Stream.Write(Responses.UserRoomList(list));
    }

    internal void LoadPermanentRooms()
    {
      List<Configuration.PermanentRoom> pendingRooms = new List<Configuration.PermanentRoom>(this.config.PermanentRooms);
      IEnumerable<UserRoom> existingRooms;
      lock(this.server)
      {
        existingRooms = new List<UserRoom>(this.server.World.UserRooms.Where((r) => r.IsPermanent));
      }

      foreach (var existingRoom in existingRooms)
      {
        var permanentRoom = pendingRooms.Find((r) => r.Identifier == existingRoom.Identifier);

        Character ownerCharacter = null;
        if (permanentRoom != null)
        {
          try
          {
            ownerCharacter = this.characterService.Find(permanentRoom.Owner);
          }
          catch (CharacterService.NotFoundException)
          {
            Log.Warning("Failed to reload permanent room: character {name} not found, existing room will be closed!", permanentRoom.Owner);
          }
        }

        if (permanentRoom != null && ownerCharacter != null)
        {
          Log.Debug("Updating existing permanent user room {identifier}", existingRoom.Identifier);

          pendingRooms.Remove(permanentRoom);
          existingRoom.Name = permanentRoom.Name;
          existingRoom.Identifier = permanentRoom.Identifier;
          existingRoom.ModeratorNames = permanentRoom.Moderators;
          existingRoom.OwnerName = permanentRoom.Owner;
          existingRoom.OwnerId = ownerCharacter.Id;
          existingRoom.RadioURL = permanentRoom.RadioURL;
          existingRoom.IsPermanent = true;

          if (existingRoom.Lobby != null)
          {
            existingRoom.Lobby.RadioURL = permanentRoom.RadioURL;
          }
        }
        else
        {
          lock (this.server)
          {
            Log.Debug("Removing existing user room {identifier}", existingRoom.Identifier);
            this.CloseRoom(existingRoom);
          }
        }
      }

      foreach (var permanentRoom in pendingRooms)
      {
        Character ownerCharacter;
        try
        {
          ownerCharacter = this.characterService.Find(permanentRoom.Owner);
          Log.Debug("Creating new permanent user room {identifier}", permanentRoom.Identifier);

          var room = new UserRoom
          {
            Name = permanentRoom.Name,
            Identifier = permanentRoom.Identifier,
            Access = UserRoom.AccessType.ForAll,
            OwnerName = permanentRoom.Owner,
            OwnerId = ownerCharacter.Id,
            ModeratorNames = permanentRoom.Moderators,
            RadioURL = permanentRoom.RadioURL,
            IsPermanent = true,
            IsPrioritized = true
          };

          lock (this.server)
          {
            this.server.World.AddUserRoom(room);
          }
        }
        catch (CharacterService.NotFoundException)
        {
          Log.Warning("Failed to load permanent room: character {name} not foundt!", permanentRoom.Owner);
        }
      }
    }

    internal IPromise<Nothing> ExpellAll(UserRoom userRoom)
    {
      Log.Debug("Expelling everyone from user room {identifier}", userRoom.Identifier);
      PlayerHandle[] lobbyPlayers = new PlayerHandle[] { };

      lock (this.server)
      {
        var lobby = this.server.World.FindLobby(userRoom.Identifier);
        if (lobby != null)
        {
          lobbyPlayers = lobby.Players.ToArray();
        }
      }

      return new CombinedPromise<Nothing>(lobbyPlayers.Select(a => a.Stream.Write(Responses.UserRoomClosed(userRoom))));
    }

    internal IPromise<Nothing> ExpelPlayer(PlayerHandle player, UserRoom userRoom, TimeSpan duration)
    {
      lock (this.server)
      {
        this.expelRecordsFor(player.User.Id).Add(new ExpelRecord
        {
          UserId = player.User.Id,
          UserRoom = userRoom,
          UntilDate = DateTime.Now + duration,
        });
      }

      lock (this.server)
      {
        if (player.CurrentLobby != null && player.CurrentLobby.Identifier == userRoom.Identifier)
        {
          var lobby = player.CurrentLobby;
          player.CurrentLobby = null;

          lobby.RemovePlayer(player);

          return this.lobbyService.NotifyLobbyAboutLeavingPlayer(player, lobby)
            .Then((a) => player.Stream.Write(Responses.UserRoomClosed(userRoom)));
        }
        else
        {
          Log.Debug("Player {name} is not currently in the target lobby ({identifier}), only timer added", player.Digest, userRoom.Identifier);
        }
      }

      return Nothing.Resolved();
    }

    internal IPromise<Nothing> PlayerLoggedIn(PlayerHandle handle)
    {
      lock(this.server)
      {
        var room = this.server.World.FindUserRoom(handle);
        if (room != null && room.IsDangling)
        {
          Log.Information("Player {player} logged back, room {identifier} is no longer dangling", handle.Digest, room.Identifier);
          room.IsDangling = false;
        }
      }

      return Nothing.Resolved();
    }

    internal IPromise<Nothing> PlayerLoggedOff(PlayerHandle sender)
    {
      if (!sender.IsLoggedIn)
      {
        return Nothing.Resolved();
      }

      lock(this.server)
      {
        var room = this.server.World.FindUserRoom(sender);

        if (room != null && !room.IsSemiPersistent && !room.IsPermanent)
        {
          return this.CloseRoom(room);
        }
        else if (room != null)
        {
          room.OwnerLastSeen = DateTime.Now;
          if (room.IsSemiPersistent)
          {
            Log.Information("Player {player} logged off and left his room {identifier} dangling", sender.Digest, room.Identifier);
            room.IsDangling = true;
          }
        }

        return Nothing.Resolved();
      }
    }

    internal void CleanupExpelRecords()
    {
      lock(this)
      {
        foreach (var entry in this.expelRecords)
        {
          foreach (var record in new List<ExpelRecord>(entry.Value))
          {
            if (record.HasExpired())
            {
              Log.Verbose("Cleanup - removing expel record of {id} at {userroom} - expired", entry.Key, record.UserRoom.Identifier);
              entry.Value.Remove(record);
            }
          }
        }
      }
    }

    internal void CleanupDanglingRooms()
    {
      List<UserRoom> userRooms;
      lock (this.server)
      {
        userRooms = new List<UserRoom>(this.server.World.UserRooms);
      }

      foreach (var userRoom in userRooms.Where((r) => r.IsDangling))
      {
        if (DateTime.Now - userRoom.OwnerLastSeen > this.config.DanglingRoom.Timeout)
        {
          Log.Debug("Room {identifier} is danging and owner last seen on {date} ({ago} ago) - closing", userRoom.Identifier, userRoom.OwnerLastSeen, DateTime.Now - userRoom.OwnerLastSeen);
          lock (this)
          {
            this.CloseRoom(userRoom);
          }
        }
      }
    }

    private List<ExpelRecord> expelRecordsFor(uint id)
    {
      lock (this)
      {
        List<ExpelRecord> list;
        if (this.expelRecords.TryGetValue(id, out list))
        {
          return list;
        }
        else
        {
          list = new List<ExpelRecord>();
          this.expelRecords[id] = list;
          return list;
        }
      }
    }
  }
}
