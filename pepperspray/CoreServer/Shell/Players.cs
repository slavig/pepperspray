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
  internal class Players: ACommand
  {
    internal override bool WouldDispatch(string tag)
    {
      return tag.Equals("players");
    }

    internal override IPromise<Nothing> Dispatch(ShellDispatcher dispatcher, PlayerHandle sender, CoreServer server, string tag, IEnumerable<string> arguments)
    {
      if (arguments.Count() == 0)
      {
        var builder = new StringBuilder("Online players: ");

        lock (server)
        {
          foreach (var item in server.World.Lobbies)
          {
            if (item.Value.IsUserRoom)
            {
              continue;
            }

            builder.AppendFormat(" {0} ({1}),", item.Key, item.Value.Players.Count());
          }
        }

        var response = builder.ToString();
        return dispatcher.Output(sender, server, response.Substring(0, response.Length - 1));
      }
      else 
      {
        var query = arguments.First();
        if (query.Count() < 3)
        {
          return dispatcher.Error(sender, server, "Query should be at least 3 characters long");
        }

        var builder = new StringBuilder("Found players: ");
        lock (server)
        {
          foreach (var player in server.World.Players)
          {
            if (player.Name.Contains(query))
            {
              builder.AppendFormat(" {0} ({1}),", player.Name, player.CurrentLobby != null ? "at " + player.CurrentLobby.Identifier : "not in lobby");
            }
          }
        }

        var response = builder.ToString();
        return dispatcher.Output(sender, server, response.Substring(0, response.Length - 1));
      }
    }
  }
}
