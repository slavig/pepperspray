using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RSG;
using pepperspray.CIO;
using pepperspray.ChatServer.Game;
using pepperspray.SharedServices;
using pepperspray.ChatServer.Protocol;

namespace pepperspray.ChatServer.Shell
{
  internal class AdminPlayers: AShellCommand
  {
    private ShellDispatcher dispatcher = DI.Get<ShellDispatcher>();
    private ChatManager manager = DI.Get<ChatManager>();

    internal override bool HasPermissionToExecute(PlayerHandle sender)
    {
      return sender.AdminOptions.HasFlag(AdminFlags.OnlinePlayerLookup);
    }

    internal override bool WouldDispatch(string tag, IEnumerable<string> arguments)
    {
      return tag.Equals("/aplayers");
    }

    internal override IPromise<Nothing> Dispatch(PlayerHandle sender, CommandDomain domain, string tag, IEnumerable<string> arguments)
    {
      string query = null;
      if (arguments.Count() > 0)
      {
        query = arguments.First();
      }

      var builder = new StringBuilder("Online players: ");
      var output = new List<IPromise<Nothing>>();

      List<PlayerHandle> players;
      lock (this.manager)
      {
        players = this.manager.World.Players.ToList();
      }

      players.Sort((a, b) => a.CurrentLobbyIdentifier.CompareTo(b.CurrentLobbyIdentifier));

      builder.AppendFormat("(total {0})", players.Count());
      string lastLobbyIdentifier = null;
      foreach (var player in players)
      {
        if (query != null && !player.Name.Contains(query))
        {
          continue;
        }

        if (player.CurrentLobbyIdentifier != lastLobbyIdentifier)
        {
          output.Add(this.dispatcher.Output(domain, builder.ToString()));
          builder.Clear();

          builder.AppendFormat("{0}: ", player.CurrentLobbyIdentifier);
        }
        lastLobbyIdentifier = player.CurrentLobbyIdentifier;

        builder.AppendFormat(" {0},", player.Name);
        if (builder.Length > 200)
        {
          output.Add(this.dispatcher.Output(domain, builder.ToString()));
          builder.Clear();
        }
      }

      if (builder.ToString().Trim().Count() > 0)
      {
        output.Add(this.dispatcher.Output(domain, builder.ToString()));
      }
      return new CombinedPromise<Nothing>(output);
    }
  }
}
