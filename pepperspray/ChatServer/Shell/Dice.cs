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
using pepperspray.Utils;
using pepperspray.Resources;

namespace pepperspray.ChatServer.Shell
{
  internal class Dice: AShellCommand
  {
    private Random random = new Random();
    private ShellDispatcher dispatcher = DI.Get<ShellDispatcher>();
    private ChatManager manager = DI.Get<ChatManager>();

    internal override bool WouldDispatch(string tag, IEnumerable<string> arguments)
    {
      return tag.Equals("/dice");
    }

    internal override IPromise<Nothing> Dispatch(PlayerHandle sender, CommandDomain domain, string tag, IEnumerable<string> arguments)
    {
      int cap = 6;
      if (arguments.Count() > 0)
      {
        try
        {
          cap = Convert.ToInt32(arguments.First());
        }
        catch (FormatException) { }
      }

      if (domain.IsWorld && !this.manager.CheckWorldMessagePermission(sender))
      {
        return Nothing.Resolved();
      }

      var value = this.random.Next(cap + 1);

      var messages = new List<Message>
      {
        Responses.ServerMessage(this.manager, domain.Identifier + String.Format(Strings.PLAYER_ROLLED_OUT_OF, sender.Name, value, cap))
      };

      var promises = new List<IPromise<Nothing>>();
      foreach (var msg in messages)
      {
        if (domain.IsPrivate)
        {
          if (domain.Recepients.Count() > 0)
          {
            var recepient = domain.Recepients.First();
            promises.Add(sender.Stream.Write(Responses.PrivateChatMessage(recepient, String.Format(Strings.PLAYER_ROLLED_OUT_OF, sender.Name, value, cap))));
            promises.Add(recepient.Stream.Write(Responses.PrivateChatMessage(sender, String.Format(Strings.PLAYER_ROLLED_OUT_OF_VERIFY, sender.Name, value, cap))));
          }
        }
        else
        {
          promises.Add(sender.Stream.Write(msg));
        }

        promises.AddRange(domain.Recepients.Select(r => r.Stream.Write(msg)));
      }

      return new CombinedPromise<Nothing>(promises);
    }
  }
}
