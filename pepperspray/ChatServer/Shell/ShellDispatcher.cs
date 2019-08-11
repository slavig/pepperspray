using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RSG;

using pepperspray.CIO;
using pepperspray.ChatServer.Game;
using pepperspray.ChatServer.Protocol;

namespace pepperspray.ChatServer.Shell
{
  internal class ShellDispatcher
  {
    private AShellCommand[] registeredCommands = new AShellCommand[]
    {
      new AdminKick(),
      new AdminPlayers(),
      new AdminBroadcast(),
      new AdminConfigReload(),
      new AdminRoomPriority(),
      new Players(),
      new PrivateMessage(),
      new Help(),
    };

    internal bool ShouldDispatch(string message)
    {
      if (message.StartsWith("/me"))
      {
        return false;
      } if (message.StartsWith("/"))
      {
        return true;
      }
      else
      {
        return false;
      }
    }

    internal IPromise<Nothing> Dispatch(PlayerHandle sender, ChatManager server, string message)
    {
      string[] components = message.Substring(1).Split(' ');
      string tag = components.First();
      foreach (var command in this.registeredCommands)
      {
        if (command.WouldDispatch(tag))
        {
          if (!command.RequireAdmin() || command.RequireAdmin() && sender.User.IsAdmin)
          {
            return command.Dispatch(this, sender, server, tag, components.Skip(1));
          }
          else
          {
            return this.Error(sender, server, "Not authenticated. Incident will be reported.");
          }
        }
      }

      return this.Error(sender, server, "Unknown command, see /help.");
    }

    internal IPromise<Nothing> Error(PlayerHandle sender, ChatManager server, string format, params object[] arguments)
    {
      return this.Output(sender, server, format, arguments);
    }

    internal IPromise<Nothing> Output(PlayerHandle sender, ChatManager server, string format, params object[] arguments)
    {
      return sender.Stream.Write(Responses.ServerMessage(server, String.Format(format, arguments)));
    }
  }
}
