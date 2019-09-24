using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using pepperspray.ChatServer.Game;
using pepperspray.CIO;
using RSG;

using Serilog;
using RSG;
using pepperspray.CIO;
using pepperspray.ChatServer.Game;
using pepperspray.ChatServer.Protocol;
using pepperspray.Utils;
using pepperspray.SharedServices;
using pepperspray.Resources;
using pepperspray.ChatServer.Protocol.Requests;

namespace pepperspray.ChatServer.Shell
{
  internal class Me: AShellCommand
  {
    private ChatManager manager = DI.Get<ChatManager>();

    internal override bool WouldDispatch(string tag, IEnumerable<string> arguments)
    {
      return tag.Equals("/me") || arguments.Aggregate(false, (result, element) => result || element.Contains("/me"));
    }

    internal override IPromise<Nothing> Dispatch(PlayerHandle sender, CommandDomain domain, string tag, IEnumerable<string> arguments)
    {
      if (domain.IsWorld && !this.manager.CheckWorldMessagePermission(sender))
      {
        return Nothing.Resolved();
      }

      var contents = tag + " " + String.Join(" ", arguments);
      var messageToSend = "";

      var components = contents.Split(new char[] { '/', ';' });
      foreach (var substring in components)
      {
        var trimmedSubstring = substring.Trim();
        if (trimmedSubstring.Any())
        {
          if (trimmedSubstring.StartsWith("me"))
          {
            messageToSend += trimmedSubstring.Substring(2);
          }
          else
          {
            messageToSend += String.Format(" \"{0}\"", trimmedSubstring);
          }
        }
      }

      messageToSend = messageToSend.Trim();

      var text = String.Format("{0}/me {1}", domain.Identifier, messageToSend);

      var commands = domain.Recepients.Select(r => r.Stream.Write(Responses.Message(sender, text))).ToList();
      if (components.Where(c => c.Trim().Any()).Count() > 1)
      {
        if (domain.IsPrivate)
        {
          if (domain.Recepients.Count() > 0)
          {
            var recepient = domain.Recepients.First();
            var selfText = String.Format("{0}/me {1} {2} {3}", domain.Identifier, this.manager.Monogram, sender.Name, messageToSend);
            commands.Add(sender.Stream.Write(Responses.Message(recepient, selfText)));
          }
        }
        else
        {
          var selfText = String.Format("{0}/me {1} {2}", domain.Identifier, sender.Name, messageToSend);
          commands.Add(sender.Stream.Write(Responses.ServerMessage(this.manager, selfText)));
        }
      }

      return new CombinedPromise<Nothing>(commands);
    }
  }
}
