﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RSG;
using Serilog;
using pepperspray.CIO;
using pepperspray.CoreServer.Game;
using pepperspray.CoreServer.Protocol;
using pepperspray.Utils;
using pepperspray.SharedServices;

namespace pepperspray.CoreServer.Services
{
  internal class LobbyService
  {
    private CoreServer server = DI.Get<CoreServer>();

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
        lobbyPlayers = lobby.Players.ToArray();

        if (lobby.Players.Count() == 0)
        {
          server.World.RemoveLobby(lobby);
        }
      }

      Log.Information("Player {name} leaving lobby {id}, notifying {total} players.", player.Name, lobby != null ? lobby.Identifier : "INVALID", lobbyPlayers.Count());
      return player.Stream.Write(Responses.JoinedLobby())
        .Then(a => new CombinedPromise<Nothing>(lobbyPlayers.Select(b => b.Stream.Write(Responses.PlayerLeave(player)))))
      as IPromise<Nothing>;
    }
  }
}
