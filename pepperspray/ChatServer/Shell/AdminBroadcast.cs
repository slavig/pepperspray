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
    private ShellDispatcher dispatcher = DI.Get<ShellDispatcher>();
    private ChatManager manager = DI.Get<ChatManager>();

    internal override bool HasPermissionToExecute(PlayerHandle sender)
    {
      return sender.AdminOptions.HasFlag(AdminFlags.AdminBroadcast);
    }

    internal override bool WouldDispatch(string tag, IEnumerable<string> arguments)
    {
      return tag.Equals("/abroadcast") || tag.Equals("/aalert");
    }

    internal override IPromise<Nothing> Dispatch(PlayerHandle sender, CommandDomain domain, string tag, IEnumerable<string> arguments)
    {
      if (arguments.Count() == 0 || tag.Equals("/aalert") && arguments.Count() < 2)
      {
        return this.dispatcher.InvalidUsage(domain);
      }

      string message = "";
      PlayerHandle[] players = null;
      lock(this.manager)
      {
        if (tag.Equals("/aalert"))
        {
          var player = CommandUtils.GetPlayer(arguments.First(), domain, this.manager);
          if (player == null)
          {
            return this.dispatcher.Error(domain, Strings.PLAYER_NOT_FOUND, arguments.First());
          }

          players = new PlayerHandle[] { player };
          message = String.Join(" ", arguments.Skip(1));
        }
        else
        {
          players = this.manager.World.Players.ToArray();
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
