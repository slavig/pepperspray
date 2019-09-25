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
using pepperspray.Resources;

namespace pepperspray.ChatServer.Shell
{
  internal class Players: AShellCommand
  {
    private LobbyService lobbyService = DI.Get<LobbyService>();
    private ShellDispatcher dispatcher = DI.Get<ShellDispatcher>();
    private ChatManager manager = DI.Get<ChatManager>();
    
    internal override bool WouldDispatch(string tag, IEnumerable<string> arguments)
    {
      return tag.Equals("/players");
    }

    internal override IPromise<Nothing> Dispatch(PlayerHandle sender, CommandDomain domain, string tag, IEnumerable<string> arguments)
    {
      var builder = new StringBuilder(Strings.ONLINE_PLAYERS_COLON);

      lock (this.manager)
      {
        foreach (var item in this.manager.World.Lobbies)
        {
          string name = item.Value.Name;
          if (name != null)
          {
            builder.AppendFormat(" {0} ({1}),", name, item.Value.Players.Count());
          }
        }
      }

      var response = builder.ToString();
      return this.dispatcher.Output(domain, response.Substring(0, response.Length - 1));
    }
  }
}
