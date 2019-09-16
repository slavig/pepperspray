using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RSG;
using pepperspray.CIO;
using pepperspray.ChatServer.Game;
using pepperspray.LoginServer;
using pepperspray.SharedServices;
using pepperspray.ChatServer.Protocol;
using pepperspray.Resources;

namespace pepperspray.ChatServer.Shell
{
  internal class AdminBroadcast: AShellCommand
  {
    private LoginServerListener loginServer = DI.Get<LoginServerListener>();

    internal override bool RequireAdmin()
    {
      return true;
    }

    internal override bool WouldDispatch(string tag)
    {
      return tag.Equals("abroadcast") || tag.Equals("aalert");
    }

    internal override IPromise<Nothing> Dispatch(ShellDispatcher dispatcher, PlayerHandle sender, ChatManager server, string tag, IEnumerable<string> arguments)
    {
      if (arguments.Count() == 0 || tag.Equals("aalert") && arguments.Count() < 2)
      {
        return dispatcher.InvalidUsage(sender, server);
      }

      string message = "";
      PlayerHandle[] players = null;
      lock(server)
      {
        if (tag.Equals("aalert"))
        {
          var player = server.World.FindPlayer(arguments.First());
          if (player == null)
          {
            return dispatcher.Error(sender, server, Strings.PLAYER_NOT_FOUND, arguments.First());
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

      var promises = new List<IPromise<Nothing>>();
      foreach (var player in players)
      {

        try
        {
          promises.Add(this.loginServer.Emit(player.Token, "alert", message));
        }
        catch (LoginServerListener.NotFoundException)
        {
          promises.Add(player.Stream.Write(Responses.MakeshiftAlert(message)));
        }
      }

      return new CombinedPromise<Nothing>(promises);
    }
  }
}
