using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RSG;
using pepperspray.CIO;
using pepperspray.ChatServer.Game;
using pepperspray.ChatServer.Protocol;
using pepperspray.ChatServer.Services;
using pepperspray.SharedServices;

namespace pepperspray.ChatServer.Shell
{
  internal class Players: AShellCommand
  {
    private LobbyService lobbyService = DI.Auto<LobbyService>();
    
    internal override bool WouldDispatch(string tag)
    {
      return tag.Equals("players");
    }

    internal override IPromise<Nothing> Dispatch(ShellDispatcher dispatcher, PlayerHandle sender, ChatManager server, string tag, IEnumerable<string> arguments)
    {
      var builder = new StringBuilder("Online players: ");

      lock (server)
      {
        foreach (var item in server.World.Lobbies)
        {
          string name = item.Value.Name;
          if (name != null)
          {
            builder.AppendFormat(" {0} ({1}),", name, item.Value.Players.Count());
          }
        }
      }

      var response = builder.ToString();
      return dispatcher.Output(sender, server, response.Substring(0, response.Length - 1));
    }
  }
}
