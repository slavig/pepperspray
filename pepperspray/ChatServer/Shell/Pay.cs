using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Serilog;
using RSG;
using pepperspray.CIO;
using pepperspray.ChatServer.Game;
using pepperspray.SharedServices;
using pepperspray.ChatServer.Protocol;

namespace pepperspray.ChatServer.Shell
{
  internal class Pay: AShellCommand
  {
    private Configuration config = DI.Get<Configuration>();
    private GiftsService giftsService = DI.Get<GiftsService>();

    internal override bool WouldDispatch(string tag)
    {
      return tag.Equals("pay");
    }

    internal override IPromise<Nothing> Dispatch(ShellDispatcher dispatcher, PlayerHandle sender, ChatManager server, string tag, IEnumerable<string> arguments)
    {
      try
      {
        var recepientName = arguments.ElementAt(0).Trim();
        var amount = Convert.ToUInt32(arguments.ElementAt(1).Trim());

        PlayerHandle recepient = server.World.FindPlayer(recepientName);
        if (recepient == null)
        {
          return dispatcher.Error(sender, server, "Failed to find player {0}.", recepientName);
        }

        if (this.config.Currency.Enabled == false)
        {
          return dispatcher.Error(sender, server, "Currency is not enabled on this server!");
        }

        this.giftsService.TransferCurrency(sender.User, recepient.User, amount);

        return dispatcher
          .Output(sender, server, "Transferred {0} coins to {1}, you now have {2}.", amount, recepientName, sender.User.Currency)
          .Then(a => recepient.Stream.Write(Responses.ServerMessage(server, String.Format("Player {0} send you {1} coins, you now have {2}.", sender.Name, amount, recepient.User.Currency))));
      }
      catch (GiftsService.NotEnoughCurrencyException)
      {
        return dispatcher.Error(sender, server, "Not enough currency!");
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
