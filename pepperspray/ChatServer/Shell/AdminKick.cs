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
using pepperspray.Resources;

namespace pepperspray.ChatServer.Shell
{
  internal class AdminKick: AShellCommand
  {
    private Configuration config = DI.Get<Configuration>();

    internal override bool RequireAdmin()
    {
      return true;
    }

    internal override bool WouldDispatch(string tag)
    {
      return tag.Equals("akick");
    }

    internal override IPromise<Nothing> Dispatch(ShellDispatcher dispatcher, PlayerHandle sender, ChatManager server, string tag, IEnumerable<string> arguments)
    {
      if (arguments.Count() < 1)
      {
        return dispatcher.InvalidUsage(sender, server);
      }

      var name = arguments.ElementAt(0);
      var reason = Strings.REASON_NONE;
      if (arguments.Count() > 1)
      {
        reason = String.Join(" ", arguments.Skip(1));
      }

      if (name.Equals("\\all"))
      {
        List<PlayerHandle> players;
        lock (server)
        {
          players = new List<PlayerHandle>(server.World.Players);
        }

        foreach (var handle in players)
        {
          try
          {
            server.Sink(server.KickPlayer(handle, reason));
          }
          catch (Exception e)
          {
            Log.Warning("Failed to kick player during kick-all: {exception}", e);
          }
        }

        return Nothing.Resolved();
      }
      else
      {
        PlayerHandle player;
        lock (server)
        {
          player = server.World.FindPlayer(arguments.ElementAt(0));
        }

        if (player == null)
        {
          return dispatcher.Error(sender, server, Strings.PLAYER_NOT_FOUND, arguments.First());
        }

        return server.KickPlayer(player, reason)
          .Then(a => dispatcher.Output(sender, server, Strings.PLAYER_HAS_BEEN_KICKED, arguments.First()));
      }
    }
  }
}
