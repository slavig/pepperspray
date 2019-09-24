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
    private ShellDispatcher dispatcher = DI.Get<ShellDispatcher>();
    private ChatManager manager = DI.Get<ChatManager>();

    internal override bool WouldDispatch(string tag, IEnumerable<string> arguments)
    {
      return tag.Equals("/pay");
    }

    internal override IPromise<Nothing> Dispatch(PlayerHandle sender, CommandDomain domain, string tag, IEnumerable<string> arguments)
    {
      try
      {
        var recepientName = arguments.ElementAt(0).Trim();
        var amount = Convert.ToUInt32(arguments.ElementAt(1).Trim());

        PlayerHandle recepient = this.manager.World.FindPlayer(recepientName);
        if (recepient == null || recepient.User.Id == sender.User.Id)
        {
          return this.dispatcher.Error(sender, Strings.PLAYER_NOT_FOUND, recepientName);
        }

        if (this.config.Currency.Enabled == false)
        {
          return this.dispatcher.Error(sender, Strings.CURRENCY_IS_NOT_ENABLED);
        }

        this.giftsService.TransferCurrency(sender.User, recepient.User, amount);

        var senderMessage = String.Format(Strings.TRANSFERRED_COINS_TO, amount, recepientName, this.giftsService.GetCurrency(sender.User));
        var recepientMessage = String.Format(Strings.PLAYER_SENT_YOU_COINS, sender.Name, amount, this.giftsService.GetCurrency(recepient.User));

        return this.dispatcher
          .Output(sender, senderMessage)
          .Then(a => recepient.Stream.Write(Responses.ServerDirectMessage(this.manager, recepientMessage)));
      }
      catch (GiftsService.NotEnoughCurrencyException)
      {
        return this.dispatcher.Error(sender, Strings.NOT_ENOUGH_COINS);
      }
      catch (Exception e)
      {
        if (e is FormatException || e is ArgumentOutOfRangeException)
        {
          return this.dispatcher.Error(sender, Strings.INVALID_AMOUNT);
        }
        else
        {
          throw e;
        }
      }
    }
  }
}
