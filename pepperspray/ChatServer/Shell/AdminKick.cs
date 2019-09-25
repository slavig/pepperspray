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
    private ShellDispatcher dispatcher = DI.Get<ShellDispatcher>();
    private ChatManager manager = DI.Get<ChatManager>();

    internal override bool HasPermissionToExecute(PlayerHandle sender)
    {
      return sender.AdminOptions.HasFlag(AdminFlags.PlayerKick);
    }

    internal override bool WouldDispatch(string tag, IEnumerable<string> arguments)
    {
      return tag.Equals("/akick");
    }

    internal override IPromise<Nothing> Dispatch(PlayerHandle sender, CommandDomain domain, string tag, IEnumerable<string> arguments)
    {
      if (arguments.Count() < 1)
      {
        return this.dispatcher.InvalidUsage(domain);
      }

      var name = arguments.ElementAt(0);
      var reason = CommandUtils.GetText(arguments.Skip(1)) ?? Strings.REASON_NONE;

      if (name.Equals("\\all"))
      {
        List<PlayerHandle> players;
        lock (this.manager)
        {
          players = new List<PlayerHandle>(this.manager.World.Players);
        }

        foreach (var handle in players)
        {
          try
          {
            this.manager.Sink(this.manager.KickPlayer(handle, reason));
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
        PlayerHandle player = CommandUtils.GetPlayer(arguments.FirstOrDefault() ?? ".", domain, this.manager);
        if (player == null)
        {
          return this.dispatcher.Error(domain, Strings.PLAYER_NOT_FOUND, arguments.First());
        }

        return this.manager.KickPlayer(player, reason)
          .Then(a => this.dispatcher.Output(domain, Strings.PLAYER_HAS_BEEN_KICKED, arguments.First()));
      }
    }
  }
}
