using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RSG;
using Serilog;
using pepperspray.CIO;
using pepperspray.ChatServer.Game;
using pepperspray.ChatServer.Protocol;
using pepperspray.ChatServer.Services.Events;
using pepperspray.Utils;
using pepperspray.SharedServices;
using pepperspray.Resources;

namespace pepperspray.ChatServer.Services
{
  internal class LobbyService: IDIService, PlayerLoggedOffEvent.IListener
  {
    private TimeSpan WorldChatIntermessageInterval = TimeSpan.FromSeconds(5);
    private TimeSpan LobbyChatIntermessageInterval = TimeSpan.FromSeconds(1);

    private Configuration config;
    private ChatManager manager;
    private UserRoomService userRoomService;
    private ConcurrentDictionary<PlayerHandle, ConcurrentDictionary<string, DateTime>> chatPlayerDates = new ConcurrentDictionary<PlayerHandle, ConcurrentDictionary<string, DateTime>>();

    public void Inject()
    {
      this.config = DI.Get<Configuration>();
      this.manager = DI.Get<ChatManager>();
      this.userRoomService = DI.Get<UserRoomService>();
    }

    public void PlayerLoggedOff(PlayerLoggedOffEvent ev)
    {
      if (ev.Handle.CurrentLobby != null)
      {
        this.manager.Sink(this.Leave(ev.Handle, ev.Handle.CurrentLobby));
      }
    }

    internal bool PlayerCanJoinLobby(PlayerHandle player, Lobby lobby)
    {
      if (player.AdminOptions.HasFlag(AdminFlags.RoomManagement))
      {
        return true;
      }

      if (lobby.IsUserRoom)
      {
        return this.userRoomService.PlayerCanJoinRoom(player, lobby.UserRoom);
      }
      else
      {
        return true;
      }
    }

    internal IPromise<Nothing> Join(PlayerHandle player, Lobby lobby)
    {
      PlayerHandle[] otherPlayers = null;
      lock (this.manager)
      {
        otherPlayers = lobby.Players.ToArray();
        if (player.CurrentLobby != null)
        {
          player.CurrentLobby.RemovePlayer(player);
        }

        lobby.AddPlayer(player);
        player.CurrentLobby = lobby;
      }

      var notifyExistingAboutNew = otherPlayers.Select(a => a.Stream.Write(Responses.NewPlayer(player)));
      var notifyNewAboutExisting = otherPlayers.Select(a => player.Stream.Write(Responses.NewPlayer(a)));

      Log.Information("Player {sender} joined lobby {id}, total {total} players.", player.Digest, lobby.Identifier, otherPlayers.Count());
      this.manager.DispatchEvent(new PlayerJoinedLobbyEvent { Handle = player, Lobby = lobby });

      return player.Stream.Write(Responses.JoinedRoom(lobby))
        .Then(a => new CombinedPromise<Nothing>(notifyExistingAboutNew))
        .Then(a => new CombinedPromise<Nothing>(notifyNewAboutExisting))
      as IPromise<Nothing>;
    }

    internal IPromise<Nothing> Leave(PlayerHandle player, Lobby lobby)
    {
      lock (this.manager)
      {
        lobby.RemovePlayer(player);
        player.CurrentLobby = null;
        if (lobby.Players.Count() == 0)
        {
          this.manager.World.RemoveLobby(lobby);
        }
      }

      Log.Information("Player {sender} leaving lobby {id}", player.Digest, lobby != null ? lobby.Identifier : "INVALID");
      if (lobby != null)
      {
        this.manager.DispatchEvent(new PlayerLeftLobbyEvent { Handle = player, Lobby = lobby });
      }

      return this.NotifyLobbyAboutLeavingPlayer(player, lobby).Then(_ => player.Stream.Write(Responses.JoinedLobby()));
    }

    internal IPromise<Nothing> NotifyLobbyAboutLeavingPlayer(PlayerHandle player, Lobby lobby)
    {
      PlayerHandle[] lobbyPlayers = new PlayerHandle[] { };

      lock (this.manager)
      {
        lobbyPlayers = lobby.Players.ToArray();
      }

      if (lobbyPlayers.Count() > 0)
      {
        return new CombinedPromise<Nothing>(lobbyPlayers.Select(b => b.Stream.Write(Responses.PlayerLeave(player))));
      }
      else
      {
        return Nothing.Resolved();
      }
    }

    internal bool CheckSlowmodeTimer(Lobby lobby, PlayerHandle handle)
    {
      var canModerate = handle.AdminOptions.HasFlag(AdminFlags.ChatManagement);
      if (lobby.IsUserRoom && this.userRoomService.PlayerCanModerateRoom(handle, lobby.UserRoom))
      {
        canModerate = true;
      }

      if (canModerate)
      {
        return true;
      }

      if (lobby.IsUserRoom && lobby.UserRoom.IsMuted)
      {
        this.manager.Sink(handle.Stream.Write(Responses.ServerLocalMessage(this.manager, Strings.SORRY_CHAT_IS_MUTED)));
        return false;
      }

      var interval = lobby.UserRoom?.SlowmodeInterval ?? this.LobbyChatIntermessageInterval;
      if (this.checkSlowmodeTimer(lobby.Identifier, handle, interval))
      {
        return true;
      }
      else
      {
        this.manager.Sink(handle.Stream.Write(Responses.ServerLocalMessage(this.manager, String.Format(Strings.TOO_FAST_CHAT_IS_IN_SLOWMODE, interval.TotalSeconds))));
        return false;
      }
    }

    internal bool CheckSlowmodeTimerInWorld(PlayerHandle handle)
    {
      if (handle.AdminOptions.HasFlag(AdminFlags.ChatManagement))
      {
        return true;
      }

      if (this.checkSlowmodeTimer("world", handle, this.WorldChatIntermessageInterval))
      {
        return true;
      }
      else
      {
        this.manager.Sink(handle.Stream.Write(Responses.ServerWorldMessage(this.manager, String.Format(Strings.TOO_FAST_CHAT_IS_IN_SLOWMODE, this.WorldChatIntermessageInterval))));
        return false;
      }
    }

    private bool checkSlowmodeTimer(string identifier, PlayerHandle handle, TimeSpan interval)
    {
      if (!this.chatPlayerDates.TryGetValue(handle, out ConcurrentDictionary<string, DateTime> dateTimes)) {
        dateTimes = new ConcurrentDictionary<string, DateTime>();
        this.chatPlayerDates[handle] = dateTimes;
      }

      if (!dateTimes.TryGetValue(identifier, out DateTime date))
      {
        date = DateTime.MinValue;
      }

      if ((DateTime.Now - date) > interval)
      {
        dateTimes[identifier] = DateTime.Now;
        return true;
      }
      else
      {
        return false;
      }
    }
  }
}
