using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RSG;
using Serilog;
using pepperspray.CIO;
using pepperspray.ChatServer.Game;
using pepperspray.SharedServices;
using pepperspray.ChatServer.Services;

namespace pepperspray.ChatServer.Protocol.Requests
{
  internal class LobbyLeave: ARequest
  {
    private LobbyService lobbyService = DI.Get<LobbyService>();

    internal static LobbyLeave Parse(Message ev)
    {
      return new LobbyLeave();
    }

    internal override IPromise<Nothing> Process(PlayerHandle sender, ChatManager server)
    {
      {
        if (sender.CurrentLobby != null)
        {
          return this.lobbyService.Leave(sender, sender.CurrentLobby);
        } 
        else
        {
          Log.Debug("Player {name} requested to leave the lobby with null lobby!", sender.Digest);
          return Nothing.Resolved();
        }
      }
    }
  }
}
