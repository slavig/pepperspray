using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Serilog;
using RSG;
using pepperspray.CIO;
using pepperspray.ChatServer.Game;
using pepperspray.ChatServer.Protocol;
using pepperspray.Utils;
using pepperspray.SharedServices;

namespace pepperspray.ChatServer.Shell
{
  internal class Kick: AShellCommand
  {
    private Configuration config = DI.Get<Configuration>();

    internal override bool RequireAdmin()
    {
      return true;
    }

    internal override bool WouldDispatch(string tag)
    {
      return tag.Equals("kick");
    }

    internal override IPromise<Nothing> Dispatch(ShellDispatcher dispatcher, PlayerHandle sender, ChatManager server, string tag, IEnumerable<string> arguments)
    {
      if (arguments.Count() < 1)
      {
        return dispatcher.Error(sender, server, "Invalid arguments");
      }

      var player = server.World.FindPlayer(arguments.ElementAt(0));
      if (player == null)
      {
        return dispatcher.Error(sender, server, "Player not found: \"{0}\"", arguments.First());
      }

      var reason = "None.";
      if (arguments.Count() > 1)
      {
        reason = String.Join(" ", arguments.Skip(1));
      }

      return server.KickPlayer(player, reason)
        .Then(a => dispatcher.Output(sender, server, "Player kicked"));
    }
  }
}
