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
  internal class AdminBroadcast: AShellCommand
  {
    internal override bool RequireAdmin()
    {
      return true;
    }

    internal override bool WouldDispatch(string tag)
    {
      return tag.Equals("abroadcast") || tag.Equals("aalert");
    }

    internal override IPromise<Nothing> Dispatch(ShellDispatcher dispatcher, PlayerHandle sender, CoreServer server, string tag, IEnumerable<string> arguments)
    {
      if (arguments.Count() == 0 || tag.Equals("aalert") && arguments.Count() < 2)
      {
        return dispatcher.Error(sender, server, "Invalid usage");
      }

      string message = "";
      PlayerHandle[] players = null;
      lock(server)
      {
        if (tag.Equals("aalert"))
        {
          var player = server.World.FindPlayer(arguments.ElementAt(0));
          if (player == null)
          {
            return dispatcher.Error(sender, server, "Unable to find player");
          }

          players = new PlayerHandle[] { player };
          message = String.Join(" ", arguments.Skip(1));
        }
        else
        {
          players = server.World.Players.ToArray();
          message = String.Join(" ", arguments);
        }
      }

      return new CombinedPromise<Nothing>(players.Select(a => a.Stream.Write(Responses.FriendAlert("Alert: " + message))));
    }
  }
}
