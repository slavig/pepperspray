using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RSG;
using Serilog;
using pepperspray.CIO;
using pepperspray.CoreServer.Game;
using pepperspray.SharedServices;
using pepperspray.CoreServer.Services;

namespace pepperspray.CoreServer.Protocol.Requests
{
  internal class LobbyLeave: ARequest
  {
    private LobbyService lobbyService = DI.Auto<LobbyService>();

    internal static LobbyLeave Parse(Message ev)
    {
      return new LobbyLeave();
    }

    internal override IPromise<Nothing> Process(PlayerHandle sender, CoreServer server)
    {
      {
        var lobby = sender.CurrentLobby;
        PlayerHandle[] lobbyPlayers = new PlayerHandle[] { };

        if (lobby != null) 
        {
          lock (server)
          {
            lobby.RemovePlayer(sender);
            sender.CurrentLobby = null;
            lobbyPlayers = lobby.Players.ToArray();

            if (lobby.Players.Count() == 0)
            {
              server.World.RemoveLobby(lobby);
            }
          }
        }

        Log.Information("Player {name} leaving lobby {id}, notifying {total} players.", sender.Name, lobby != null ? lobby.Identifier : "INVALID", lobbyPlayers.Count());
        return sender.Stream.Write(Responses.JoinedLobby())
          .Then(a => new CombinedPromise<Nothing>(lobbyPlayers.Select(b => b.Stream.Write(Responses.PlayerLeave(sender)))))
        as IPromise<Nothing>;
      }
    }
  }
}
