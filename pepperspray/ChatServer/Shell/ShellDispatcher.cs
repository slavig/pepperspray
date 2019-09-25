using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RSG;

using Serilog;
using pepperspray.CIO;
using pepperspray.ChatServer.Game;
using pepperspray.ChatServer.Protocol;
using pepperspray.SharedServices;
using pepperspray.Resources;

namespace pepperspray.ChatServer.Shell
{
  internal class ShellDispatcher: IDIService
  {
    private AShellCommand[] registeredCommands;

    private ChatManager manager;

    public void Inject()
    {
      this.manager = DI.Get<ChatManager>();
      this.registeredCommands = new AShellCommand[] 
      {
        new AdminKick(),
        new AdminPlayers(),
        new AdminBroadcast(),
        new AdminConfigReload(),
        new AdminRoom(),
        new AdminMoney(),
        new AdminPlayer(),
        new PrivateMessage(),
        new MyOnline(),
        new Pay(),
        new Money(),
        new Players(),
        new Expel(),
        new Dice(),
        new Sex(),
        new Me(),
        new Room(),
        new Help(),
      };
    }

    internal bool ShouldDispatch(string message)
    {
      return message.StartsWith("/") || message.Contains("/me") || message.Contains("\\я");
    }

    internal IPromise<Nothing> Dispatch(PlayerHandle sender, CommandDomain domain, string message)
    {
      Log.Debug("Shell dispatches command from {name}: {cmd}", sender.Digest, message);
      string[] components = message.Split(' ');
      string tag = components.First();
      foreach (var command in this.registeredCommands)
      {
        if (command.WouldDispatch(tag, components.Skip(1)))
        {
          if (command.HasPermissionToExecute(sender))
          {
            try
            {
              return command.Dispatch(sender, domain, tag, components.Skip(1));
            }
            catch (Exception e)
            {
#if DEBUG
              throw e;
#else
              Log.Error("Exception occured during shell command {cmd} dispath from {player}: {exception}", message, sender.Digest, e);
              return this.Error(domain, Strings.INTERNAL_SERVER_ERROR);
#endif
            }
          }
          else
          {
            return this.Error(domain, Strings.ADMIN_NOT_AUTHENTICATED);
          }
        }
      }

      Log.Debug("Unkown shell command from {player} - {command}", sender.Digest, message);
      return this.Error(domain, Strings.UNKNOWN_COMMAND_SEE_HELP);
    }

    internal IPromise<Nothing> Error(CommandDomain domain, string format, params object[] arguments)
    {
      return this.Output(domain, format, arguments);
    }

    internal IPromise<Nothing> InvalidUsage(CommandDomain domain)
    {
      return this.Error(domain, Strings.INVALID_USAGE);
    }

    internal IPromise<Nothing> Output(CommandDomain domain, string format, params object[] arguments)
    {
      var text = String.Format(format, arguments);
      var sender = domain.Sender;

      if (domain.IsPrivate)
      {
        if (domain.Recipients.Count() > 0)
        {
          var recipient = domain.Recipients.First();
          text = String.Format("{0}/me {1} {2}", domain.Identifier, this.manager.Monogram, text);
          return sender.Stream.Write(Responses.Message(recipient, text));
        }
      }

      return sender.Stream.Write(Responses.ServerDirectMessage(this.manager, text));
    }
  }
}
