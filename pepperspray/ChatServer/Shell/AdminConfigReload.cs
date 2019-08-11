using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RSG;
using pepperspray.CIO;
using pepperspray.ChatServer.Game;
using pepperspray.SharedServices;

namespace pepperspray.ChatServer.Shell
{
  internal class AdminConfigReload: AShellCommand
  {
    private Configuration config = DI.Get<Configuration>();

    internal override bool RequireAdmin()
    {
      return true;
    }

    internal override bool WouldDispatch(string tag)
    {
      return tag.Equals("aconfigreload");
    }

    internal override IPromise<Nothing> Dispatch(ShellDispatcher dispatcher, PlayerHandle sender, ChatManager server, string tag, IEnumerable<string> arguments)
    {
      this.config.LoadConfiguration();
      return dispatcher.Output(sender, server, "Configuration file reloaded.");
    }
  }
}
