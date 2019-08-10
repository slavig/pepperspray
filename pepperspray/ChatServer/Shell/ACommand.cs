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
    internal abstract bool WouldDispatch(string tag);
    internal abstract IPromise<Nothing> Dispatch(ShellDispatcher dispatcher, PlayerHandle sender, ChatManager server, string tag, IEnumerable<string> arguments);

    internal virtual bool RequireAdmin()
    {
      return false;
    }
  }
}
