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
using pepperspray.ChatServer.Services;
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
    private LobbyService lobbyService = DI.Get<LobbyService>();

    internal override bool WouldDispatch(string tag, IEnumerable<string> arguments)
    {
      return tag.Equals("\\я")
        || tag.Equals("/me")
        || arguments.Aggregate(false, (result, element) => result || (element.Contains("/me") || element.Contains("\\я")));
    }

    internal override IPromise<Nothing> Dispatch(PlayerHandle sender, CommandDomain domain, string tag, IEnumerable<string> arguments)
    {
      if (domain.IsWorld && (!this.manager.CheckWorldMessagePermission(sender) || !this.lobbyService.CheckSlowmodeTimerInWorld(sender)))
      {
        return Nothing.Resolved();
      }

      if (domain.IsLocal && sender.CurrentLobby != null && !this.lobbyService.CheckSlowmodeTimer(sender.CurrentLobby, sender))
      {
        return Nothing.Resolved();
      }

      var contents = tag + " " + String.Join(" ", arguments);
      var messageToSend = "";

      var components = contents.Split(new char[] { '\\', '/', ';' }).Select(s => s.Trim()).Where(s => s.Any());
      var selfSufficient = contents.StartsWith("/me") && components.Count() == 1;

      foreach (var substring in components)
      {
        if (substring.StartsWith("me"))
        {
          messageToSend += substring.Substring(2);
        }
        else if (substring.StartsWith("я"))
        {
          messageToSend += substring.Substring(1);
        }
        else
        {
          messageToSend += String.Format(" \"{0}\"", substring);
        }
      }

      messageToSend = messageToSend.Trim();

      var text = String.Format("{0}/me {1}", domain.Identifier, messageToSend);

      var commands = domain.Recipients.Select(r => r.Stream.Write(Responses.Message(sender, text))).ToList();
      if (!selfSufficient)
      {
        if (domain.IsPrivate)
        {
          if (domain.Recipients.Count() > 0)
          {
            var recipient = domain.Recipients.First();
            var selfText = String.Format("{0}/me {1} {2} {3}", domain.Identifier, this.manager.Monogram, sender.Name, messageToSend);
            commands.Add(sender.Stream.Write(Responses.Message(recipient, selfText)));
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
