using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Serilog;
using RSG;
using pepperspray.CIO;
using pepperspray.ChatServer.Game;
using pepperspray.ChatServer.Services;
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
    private LobbyService lobbyService = DI.Get<LobbyService>();

    internal override bool WouldDispatch(string tag, IEnumerable<string> arguments)
    {
      return tag.Equals("/pay") || tag.Equals("/give");
    }

    internal override IPromise<Nothing> Dispatch(PlayerHandle sender, CommandDomain domain, string tag, IEnumerable<string> arguments)
    {
      try
      {
        if (this.config.Currency.Enabled == false)
        {
          return this.dispatcher.Error(domain, Strings.CURRENCY_IS_NOT_ENABLED);
        }

        var recipientName = ".";
        bool recepientless = false;
        uint amount;
        
        try
        {
          // single argument - amount
          amount = Convert.ToUInt32(arguments.ElementAt(0).Trim());
          recepientless = true;
        }
        catch (FormatException)
        {
          // two arguments
          recipientName = arguments.ElementAt(0);
          amount = Convert.ToUInt32(arguments.ElementAt(1).Trim());
        }

        PlayerHandle recipient = CommandUtils.GetPlayer(recipientName, domain, this.manager);
        if (recipient == null || recipient.User.Id == sender.User.Id)
        {
          return this.dispatcher.Error(domain, Strings.PLAYER_NOT_FOUND, recipientName);
        }

        this.giftsService.TransferCurrency(sender.User, recipient.User, amount);

        var reason = CommandUtils.GetText(arguments.Skip(recepientless ? 1 : 2));
        var reasonText = reason != null ? ", \"" + reason + "\"" : "";

        var senderMessage = String.Format(Strings.TRANSFERRED_COINS_TO, amount, recipient.Name, this.giftsService.GetCurrency(sender.User));
        var recipientMessage = String.Format(Strings.PLAYER_SENT_YOU_COINS, sender.Name, amount, reasonText, this.giftsService.GetCurrency(recipient.User));
        var localMessage = String.Format(Strings.GAVE_COINS_TO, sender.Name, amount, recipient.Name);

        var promises = new List<IPromise<Nothing>>
        {
          this.dispatcher.Output(domain, senderMessage),
          recipient.Stream.Write(Responses.ServerDirectMessage(this.manager, recipientMessage))
        };

        if (tag.Equals("/give"))
        {
          if (sender.CurrentLobby != null && this.lobbyService.CheckSlowmodeTimer(sender.CurrentLobby, sender))
          {
            var localMsg = Responses.ServerLocalMessage(this.manager, localMessage);

            promises.Add(sender.Stream.Write(localMsg));
            promises.AddRange(domain.Recipients.Select(r => r.Stream.Write(localMsg)));
          }
        }

        return new CombinedPromise<Nothing>(promises);
      }
      catch (GiftsService.NotEnoughCurrencyException)
      {
        return this.dispatcher.Error(domain, Strings.NOT_ENOUGH_COINS);
      }
      catch (Exception e)
      {
        if (e is FormatException || e is ArgumentOutOfRangeException)
        {
          return this.dispatcher.Error(domain, Strings.INVALID_AMOUNT);
        }
        else
        {
          throw e;
        }
      }
    }
  }
}
