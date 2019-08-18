using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RSG;
using pepperspray.CIO;
using pepperspray.ChatServer.Game;
using pepperspray.SharedServices;

namespace pepperspray.ChatServer.Shell
{
  internal class AdminMoney : AShellCommand
  {
    private Configuration config = DI.Get<Configuration>();
    private GiftsService giftService = DI.Get<GiftsService>();

    internal override bool RequireAdmin()
    {
      return true;
    }

    internal override bool WouldDispatch(string tag)
    {
      return tag.Equals("amoney");
    }

    internal override IPromise<Nothing> Dispatch(ShellDispatcher dispatcher, PlayerHandle sender, ChatManager server, string tag, IEnumerable<string> arguments)
    {
      try
      {
        var playerName = arguments.ElementAt(0).Trim();
        var amount = Convert.ToInt32(arguments.ElementAt(1).Trim());

        PlayerHandle player;
        lock (server)
        {
          player = server.World.FindPlayer(arguments.First().Trim());
        }

        if (this.config.Currency.Enabled == false)
        {
          return dispatcher.Error(sender, server, "Currency is not enabled");
        }

        if (player == null)
        {
          return dispatcher.Error(sender, server, "Failed to find player");
        }

        this.giftService.ChangeCurrency(player.User, amount);
        return dispatcher.Output(sender, server, "Changed, player now has {0}.", player.User.Currency);
      }
      catch (Exception e)
      {
        if (e is FormatException || e is ArgumentOutOfRangeException)
        {
          return dispatcher.Error(sender, server, "Invalid arguments");
        }
        else
        {
          throw e;
        }
      }

    }
  }
}
