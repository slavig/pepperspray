using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RSG;
using pepperspray.CIO;
using pepperspray.CoreServer.Game;
using pepperspray.CoreServer.Protocol;

namespace pepperspray.CoreServer.Shell
{
  internal class AdminPlayers: AShellCommand
  {
    internal override bool RequireAdmin()
    {
      return true;
    }

    internal override bool WouldDispatch(string tag)
    {
      return tag.Equals("aplayers");
    }

    internal override IPromise<Nothing> Dispatch(ShellDispatcher dispatcher, PlayerHandle sender, CoreServer server, string tag, IEnumerable<string> arguments)
    {
      var builder = new StringBuilder("Online players: ");
      var output = new List<IPromise<Nothing>>();

      lock (server)
      {
        foreach (var player in server.World.Players)
        {
          builder.AppendFormat(" {0} (at {1}),", player.Name, player.CurrentLobby != null ? player.CurrentLobby.Identifier : "editor");
          if (builder.Length > 200)
          {
            output.Add(dispatcher.Output(sender, server, output.ToString()));
            builder.Clear();
          }
        }
      }

      output.Add(dispatcher.Output(sender, server, builder.ToString()));
      return new CombinedPromise<Nothing>(output);
    }
  }
}
