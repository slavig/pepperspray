using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RSG;
using pepperspray.CIO;
using pepperspray.ChatServer.Game;
using pepperspray.ChatServer.Protocol;
using pepperspray.Utils;
using pepperspray.SharedServices;

namespace pepperspray.ChatServer.Services
{
  internal class UserRoomService: IDIService
  {
    private ChatManager server;
    private FriendsService friendsService;

    public void Inject()
    {
      this.server = DI.Get<ChatManager>();
      this.friendsService = DI.Get<FriendsService>();
    }

    internal bool PlayerCanJoinRoom(PlayerHandle player, UserRoom room)
    {
      switch (room.Access)
      {
        case UserRoom.AccessType.ForAll:
          return true;
        case UserRoom.AccessType.ForFriends:
          return player == room.User || this.friendsService.GetFriendIDs(room.User.Character).Contains(player.Character.Id);
        case UserRoom.AccessType.ForGroup:
          return player.CurrentGroup == room.User.CurrentGroup;
        default:
          return false;
      }
    }

    internal IPromise<Nothing> OpenRoom(PlayerHandle sender, ChatManager server, UserRoom room) 
    {
      lock(server)
      {
        server.World.AddUserRoom(room);
      }

      return Nothing.Resolved();
    }

    internal IPromise<Nothing> CloseRoom(PlayerHandle sender, ChatManager server, UserRoom room)
    {
      PlayerHandle[] lobbyPlayers = new PlayerHandle[] { };

      lock (server)
      {
        var lobby = server.World.FindLobby(room.Identifier);
        if (lobby != null)
        {
          lobbyPlayers = lobby.Players.ToArray();
        }

        server.World.RemoveUserRoom(room.Identifier);
      }

      return new CombinedPromise<Nothing>(lobbyPlayers.Select(a => a.Stream.Write(Responses.UserRoomClosed(room))));
    }

    internal IPromise<Nothing> ListRooms(PlayerHandle sender)
    {
      var list = new List<UserRoom>();
      lock(this.server)
      {
        foreach (var room in this.server.World.UserRooms)
        {
          if (this.PlayerCanJoinRoom(sender, room))
          {
            list.Add(room);
          }
        }
      }

      return sender.Stream.Write(Responses.UserRoomList(list));
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
        if (room != null)
        {
          return this.CloseRoom(sender, this.server, room);
        }
        else
        {
          return Nothing.Resolved();
        }
      }
    }
  }
}
