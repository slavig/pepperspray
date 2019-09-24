using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Serilog;
using RSG;
using pepperspray.CIO;
using pepperspray.ChatServer.Game;
using pepperspray.ChatServer.Protocol;
using pepperspray.Utils;
using pepperspray.SharedServices;
using pepperspray.Resources;

namespace pepperspray.ChatServer.Shell
{
  internal class Help: AShellCommand
  {
    private ShellDispatcher dispatcher = DI.Get<ShellDispatcher>();

    internal override bool WouldDispatch(string tag, IEnumerable<string> arguments)
    {
      return tag.Equals("/help");
    }

    internal override IPromise<Nothing> Dispatch(PlayerHandle sender, CommandDomain domain, string tag, IEnumerable<string> arguments)
    {
      var promises = new List<IPromise<Nothing>>();
      foreach (var line in Strings.SHELL_HELP_TEXT.Split('\n'))
      {
        promises.Add(this.dispatcher.Output(sender, line));
      }

      return new CombinedPromise<Nothing>(promises);
    }
  }
}
