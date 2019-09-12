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
  internal class LobbyService: IDIService
  {
    private ChatManager server;
    private UserRoomService userRoomService;

    public void Inject()
    {
      this.server = DI.Get<ChatManager>();
      this.userRoomService = DI.Get<UserRoomService>();
    }

    internal bool PlayerCanJoinLobby(PlayerHandle player, Lobby lobby)
    {
      if (player.User.IsAdmin)
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
      lock (server)
      {
        otherPlayers = lobby.Players.ToArray();
        lobby.AddPlayer(player);
        player.CurrentLobby = lobby;
      }

      var notifyExistingAboutNew = otherPlayers.Select(a => a.Stream.Write(Responses.NewPlayer(player)));
      var notifyNewAboutExisting = otherPlayers.Select(a => player.Stream.Write(Responses.NewPlayer(a)));

      Log.Information("Player {name} joined lobby {id}, total {total} players.", player.Name, lobby.Identifier, otherPlayers.Count());

      return player.Stream.Write(Responses.JoinedRoom(lobby))
        .Then(a => new CombinedPromise<Nothing>(notifyExistingAboutNew))
        .Then(a => new CombinedPromise<Nothing>(notifyNewAboutExisting))
      as IPromise<Nothing>;
    }

    internal IPromise<Nothing> Leave(PlayerHandle player, Lobby lobby)
    {
      PlayerHandle[] lobbyPlayers = new PlayerHandle[] { };

      lock (server)
      {
        lobby.RemovePlayer(player);
        player.CurrentLobby = null;
        if (lobby.Players.Count() == 0)
        {
          server.World.RemoveLobby(lobby);
        }
      }

      Log.Information("Player {name} leaving lobby {id}, notifying {total} players.", player.Name, lobby != null ? lobby.Identifier : "INVALID", lobbyPlayers.Count());
      return player.Stream.Write(Responses.JoinedLobby())
        .Then((a) => this.NotifyLobbyAboutLeavingPlayer(player, lobby));
    }

    internal IPromise<Nothing> NotifyLobbyAboutLeavingPlayer(PlayerHandle player, Lobby lobby)
    {
      PlayerHandle[] lobbyPlayers = new PlayerHandle[] { };

      lock (server)
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
  }
}
