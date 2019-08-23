using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RSG;
using pepperspray.CIO;
using pepperspray.ChatServer.Game;
using pepperspray.ChatServer.Protocol;
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
              return dispatcher.Error(sender, server, "You are not in lobby.");
            }

            players = sender.CurrentLobby.Players.ToArray();
          }
          else
          {
            var player = server.World.FindPlayer(arguments.First().Trim());
            if (player == null)
            {
              return dispatcher.Error(sender, server, "Failed to find player.");
            }

            players = new PlayerHandle[] { player };
          }
        }

        if (this.config.Currency.Enabled == false)
        {
          return dispatcher.Error(sender, server, "Currency is not enabled");
        }

        foreach (var player in players)
        {
          this.giftService.ChangeCurrency(player.User, amount);
        }

        var message = String.Format("You have been gifted {0} coins from admin.", amount);
        return new CombinedPromise<Nothing>(players.Select(p => p.Stream.Write(Responses.ServerMessage(server, message))))
          .Then(a => dispatcher.Output(sender, server, "Done"));
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
