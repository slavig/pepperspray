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
  internal class LobbyJoin: ARequest
  {
    private string lobbyIdentifier;
    private Lobby lobby;
    private LobbyService lobbyService = DI.Auto<LobbyService>();

    internal static LobbyJoin Parse(Message ev)
    {
      return new LobbyJoin
      {
        lobbyIdentifier = ev.data.ToString()
      };
    }

    internal override bool Validate(PlayerHandle sender, CoreServer server)
    {
      if (!base.Validate(sender, server))
      {
        return false;
      }

      lock(server)
      {
        this.lobby = server.World.FindOrCreateLobby(this.lobbyIdentifier);
      }

      return lobbyService.PlayerCanJoinLobby(sender, this.lobby);
    }

    internal override IPromise<Nothing> Process(PlayerHandle sender, CoreServer server)
    {
      return this.lobbyService.Join(sender, this.lobby);
    }
  }
}
