using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RSG;
using pepperspray.CIO;
using pepperspray.ChatServer.Game;

namespace pepperspray.ChatServer.Shell
{
  internal abstract class AShellCommand
  {
    internal abstract bool WouldDispatch(string tag, IEnumerable<string> arguments);
    internal abstract IPromise<Nothing> Dispatch(PlayerHandle sender, CommandDomain domain, string tag, IEnumerable<string> arguments);

    internal virtual bool HasPermissionToExecute(PlayerHandle sender)
    {
      return true;
    }
  }

  internal static class CommandUtils
  {
    internal static string GetText(IEnumerable<string> components)
    {
      var s = String.Join(" ", components).Trim();
      return s.Length == 0 ? null : s;
    }

    internal static PlayerHandle GetPlayer(string input, CommandDomain domain, ChatManager manager)
    {
      if (input.Equals(".") && domain.IsPrivate)
      {
        return domain.Recipients.FirstOrDefault();
      }
      else
      {
        lock (manager)
        {
          return manager.World.FindPlayer(input.Trim());
        }
      }
    }
  }

  internal class CommandDomain
  {
    internal PlayerHandle Sender;
    internal IEnumerable<PlayerHandle> Recipients;
    internal string Identifier;

    internal bool IsPrivate => this.Identifier.StartsWith("~private/");
    internal bool IsWorld => this.Identifier.StartsWith("~worldchat/");
    internal bool IsLocal => this.Identifier.StartsWith("~chat/");

    internal CommandDomain(string identifier, PlayerHandle sender, IEnumerable<PlayerHandle> recipients)
    {
      this.Recipients = recipients;
      this.Sender = sender;
      this.Identifier = identifier;
    }
  }
}
