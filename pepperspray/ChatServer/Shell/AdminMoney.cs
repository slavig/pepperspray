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

        PlayerHandle[] players;

        lock (server)
        {
          if (playerName.Equals("\\world"))
          {
            players = server.World.Players.ToArray();
          }
          else if (playerName.Equals("\\lobby"))
          {
            if (sender.CurrentLobby == null)
            {
              return dispatcher.Error(sender, server, Strings.YOU_ARE_NOT_IN_LOBBY);
            }

            players = sender.CurrentLobby.Players.ToArray();
          }
          else
          {
            var player = server.World.FindPlayer(arguments.First().Trim());
            if (player == null)
            {
              return dispatcher.Error(sender, server, Strings.PLAYER_NOT_FOUND, arguments.First());
            }

            players = new PlayerHandle[] { player };
          }
        }

        if (this.config.Currency.Enabled == false)
        {
          return dispatcher.Error(sender, server, Strings.CURRENCY_IS_NOT_ENABLED);
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
        return new CombinedPromise<Nothing>(players.Select(p => p.Stream.Write(Responses.ServerMessage(server, message))))
          .Then(a => dispatcher.Output(sender, server, Strings.DONE));
      }
      catch (Exception e)
      {
        if (e is FormatException || e is ArgumentOutOfRangeException)
        {
          return dispatcher.InvalidUsage(sender, server);
        }
        else
        {
          throw e;
        }
      }

    }
  }
}
