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

  internal class CommandDomain
  {
    internal IEnumerable<PlayerHandle> Recepients;
    internal string Identifier;
    internal bool IsPrivate
    {
      get
      {
        return this.Identifier.StartsWith("~private/");
      }
    }

    internal bool IsWorld
    {
      get
      {
        return this.Identifier.StartsWith("~worldchat/");
      }
    }

    internal CommandDomain(string identifier, IEnumerable<PlayerHandle> recepients)
    {
      this.Recepients = recepients;
      this.Identifier = identifier;
    }
  }
}
