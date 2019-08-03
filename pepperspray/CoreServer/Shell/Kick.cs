using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Serilog;
using RSG;
using pepperspray.CIO;
using pepperspray.CoreServer.Game;
using pepperspray.CoreServer.Protocol;
using pepperspray.Utils;

namespace pepperspray.CoreServer.Shell
{
  internal class Kick: ACommand
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

    internal override IPromise<Nothing> Dispatch(ShellDispatcher dispatcher, PlayerHandle sender, CoreServer server, string tag, IEnumerable<string> arguments)
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

      var promise = Nothing.Resolved();
      if (arguments.Count() > 1)
      {
        var reason = String.Join(" ", arguments.Skip(1));
        promise = player.Stream.Write(Responses.FriendAlert("KICKED: You are kicked by admin. Reason: " + reason));
      }

      return promise
        .Then(a => player.Stream.Terminate())
        .Then(a => dispatcher.Output(sender, server, "Player kicked"));
    }
  }
}
