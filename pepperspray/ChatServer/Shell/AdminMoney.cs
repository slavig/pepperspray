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
using pepperspray.SharedServices;
using pepperspray.Resources;

namespace pepperspray.ChatServer.Shell
{
  internal class AdminMoney : AShellCommand
  {
    private Configuration config = DI.Get<Configuration>();
    private GiftsService giftService = DI.Get<GiftsService>();
    private ShellDispatcher dispatcher = DI.Get<ShellDispatcher>();
    private ChatManager manager = DI.Get<ChatManager>();

    internal override bool HasPermissionToExecute(PlayerHandle sender)
    {
      return sender.AdminOptions.HasFlag(AdminFlags.Currency);
    }

    internal override bool WouldDispatch(string tag, IEnumerable<string> arguments)
    {
      return tag.Equals("/amoney");
    }

    internal override IPromise<Nothing> Dispatch(PlayerHandle sender, CommandDomain domain, string tag, IEnumerable<string> arguments)
    {
      try
      {
        var playerName = arguments.ElementAt(0).Trim();
        var amount = Convert.ToInt32(arguments.ElementAt(1).Trim());

        PlayerHandle[] players;

        lock (this.manager)
        {
          if (playerName.Equals("\\world"))
          {
            players = this.manager.World.Players.ToArray();
          }
          else if (playerName.Equals("\\lobby"))
          {
            if (sender.CurrentLobby == null)
            {
              return this.dispatcher.Error(sender, Strings.YOU_ARE_NOT_IN_LOBBY);
            }

            players = sender.CurrentLobby.Players.ToArray();
          }
          else
          {
            var player = this.manager.World.FindPlayer(arguments.First().Trim());
            if (player == null)
            {
              return this.dispatcher.Error(sender, Strings.PLAYER_NOT_FOUND, arguments.First());
            }

            players = new PlayerHandle[] { player };
          }
        }

        if (this.config.Currency.Enabled == false)
        {
          return this.dispatcher.Error(sender, Strings.CURRENCY_IS_NOT_ENABLED);
        }

        foreach (var player in players)
        {
          try
          {
            this.giftService.ChangeCurrency(player.User, amount);
          }
          catch (GiftsService.NotEnoughCurrencyException e)
          {
            Log.Warning("Failed to gift money as admin: {exception}", e);
          }
        }

        var message = String.Format(Strings.YOU_HAVE_BEEN_GIFTED_COINS_FROM_ADMIN, amount);
        return new CombinedPromise<Nothing>(players.Select(p => p.Stream.Write(Responses.ServerDirectMessage(this.manager, message))))
          .Then(a => this.dispatcher.Output(sender, Strings.DONE));
      }
      catch (Exception e)
      {
        if (e is FormatException || e is ArgumentOutOfRangeException)
        {
          return this.dispatcher.InvalidUsage(sender);
        }
        else
        {
          throw e;
        }
      }

    }
  }
}
