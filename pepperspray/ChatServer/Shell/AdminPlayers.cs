using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RSG;
using pepperspray.CIO;
using pepperspray.ChatServer.Game;
using pepperspray.ChatServer.Protocol;

namespace pepperspray.ChatServer.Shell
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

    internal override IPromise<Nothing> Dispatch(ShellDispatcher dispatcher, PlayerHandle sender, ChatManager server, string tag, IEnumerable<string> arguments)
    {
      string query = null;
      if (arguments.Count() > 0)
      {
        query = arguments.First();
      }

      var builder = new StringBuilder("Online players: ");
      var output = new List<IPromise<Nothing>>();

      List<PlayerHandle> players;
      lock (server)
      {
        players = server.World.Players.ToList();
      }

      players.Sort((a, b) => a.CurrentLobbyName.CompareTo(b.CurrentLobbyName));

      builder.AppendFormat("(total {0})", players.Count());
      foreach (var player in players)
      {
        if (query != null && !player.Name.Contains(query))
        {
          continue;
        }

        builder.AppendFormat(" {0} (at {1}),", player.Name, player.CurrentLobby != null ? player.CurrentLobby.Identifier : "editor");
        if (builder.Length > 200)
        {
          output.Add(dispatcher.Output(sender, server, builder.ToString()));
          builder.Clear();
        }
      }

      if (builder.ToString().Trim().Count() > 0)
      {
        output.Add(dispatcher.Output(sender, server, builder.ToString()));
      }
      return new CombinedPromise<Nothing>(output);
    }
  }
}
