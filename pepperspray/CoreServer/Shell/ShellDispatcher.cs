using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RSG;

using pepperspray.CIO;
using pepperspray.CoreServer.Game;
using pepperspray.CoreServer.Protocol;

namespace pepperspray.CoreServer.Shell
{
  internal class ShellDispatcher
  {
    private AShellCommand[] registeredCommands = new AShellCommand[]
    {
      new Superuser(),
      new Kick(),
      new Players(),
      new AdminPlayers(),
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

    internal IPromise<Nothing> Dispatch(PlayerHandle sender, CoreServer server, string message)
    {
      string[] components = message.Substring(1).Split(' ');
      string tag = components.First();
      foreach (var command in this.registeredCommands)
      {
        if (command.WouldDispatch(tag))
        {
          if (!command.RequireAdmin() || command.RequireAdmin() && sender.IsAdmin)
          {
            return command.Dispatch(this, sender, server, tag, components.Skip(1));
          }
          else
          {
            return this.Error(sender, server, "Not authenticated. Incident will be reported.");
          }
        }
      }

      return Nothing.Resolved();
    }

    internal IPromise<Nothing> Error(PlayerHandle sender, CoreServer server, string format, params object[] arguments)
    {
      return this.Output(sender, server, format, arguments);
    }

    internal IPromise<Nothing> Output(PlayerHandle sender, CoreServer server, string format, params object[] arguments)
    {
      return sender.Stream.Write(Responses.ServerMessage(server, String.Format(format, arguments)));
    }
  }
}
