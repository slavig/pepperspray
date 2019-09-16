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
    private AShellCommand[] registeredCommands = new AShellCommand[]
    {
      new AdminKick(),
      new AdminPlayers(),
      new AdminBroadcast(),
      new AdminConfigReload(),
      new AdminRoom(),
      new AdminMoney(),
      new AdminPlayer(),
      new AdminPrivateMessage(),
      new MyOnline(),
      new Pay(),
      new Money(),
      new Players(),
      new Expel(),
      new Room(),
      new Help(),
    };

    public void Inject()
    {

    }

    internal bool ShouldDispatch(string message)
    {
      return message.StartsWith("/") && !message.StartsWith("/me") && !message.StartsWith("/roll");
    }

    internal IPromise<Nothing> Dispatch(PlayerHandle sender, ChatManager server, string message)
    {
      Log.Debug("Shell dispatches command from {name}: {cmd}", sender.Digest, message);
      string[] components = message.Substring(1).Split(' ');
      string tag = components.First();
      foreach (var command in this.registeredCommands)
      {
        if (command.WouldDispatch(tag))
        {
          if (!command.RequireAdmin() || command.RequireAdmin() && sender.AdminOptions.IsEnabled)
          {
            return command.Dispatch(this, sender, server, tag, components.Skip(1));
          }
          else
          {
            return this.Error(sender, server, Strings.ADMIN_NOT_AUTHENTICATED);
          }
        }
      }

      Log.Debug("Unkown shell command from {player} - {command}", sender.Digest, message);
      return this.Error(sender, server, Strings.UNKNOWN_COMMAND_SEE_HELP);
    }

    internal IPromise<Nothing> Error(PlayerHandle sender, ChatManager server, string format, params object[] arguments)
    {
      return this.Output(sender, server, format, arguments);
    }

    internal IPromise<Nothing> InvalidUsage(PlayerHandle sender, ChatManager server)
    {
      return this.Error(sender, server, Strings.INVALID_USAGE);
    }

    internal IPromise<Nothing> Output(PlayerHandle sender, ChatManager server, string format, params object[] arguments)
    {
      return sender.Stream.Write(Responses.ServerMessage(server, String.Format(format, arguments)));
    }
  }
}
