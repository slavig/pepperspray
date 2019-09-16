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
using pepperspray.Resources;

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
        if (recepient == null || recepient.User.Id == sender.User.Id)
        {
          return dispatcher.Error(sender, server, Strings.PLAYER_NOT_FOUND, recepientName);
        }

        if (this.config.Currency.Enabled == false)
        {
          return dispatcher.Error(sender, server, Strings.CURRENCY_IS_NOT_ENABLED);
        }

        this.giftsService.TransferCurrency(sender.User, recepient.User, amount);

        var senderMessage = String.Format(Strings.TRANSFERRED_COINS_TO, amount, recepientName, this.giftsService.GetCurrency(sender.User));
        var recepientMessage = String.Format(Strings.PLAYER_SENT_YOU_COINS, sender.Name, amount, this.giftsService.GetCurrency(recepient.User));

        return dispatcher
          .Output(sender, server, senderMessage)
          .Then(a => recepient.Stream.Write(Responses.ServerMessage(server, recepientMessage)));
      }
      catch (GiftsService.NotEnoughCurrencyException)
      {
        return dispatcher.Error(sender, server, Strings.NOT_ENOUGH_COINS);
      }
      catch (Exception e)
      {
        if (e is FormatException || e is ArgumentOutOfRangeException)
        {
          return dispatcher.Error(sender, server, Strings.INVALID_AMOUNT);
        }
        else
        {
          throw e;
        }
      }
    }
  }
}
