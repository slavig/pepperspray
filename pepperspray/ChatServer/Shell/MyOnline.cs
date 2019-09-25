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
using pepperspray.Resources;
using pepperspray.SharedServices;

namespace pepperspray.ChatServer.Shell
{
  internal class MyOnline: AShellCommand
  {
    private ShellDispatcher dispatcher = DI.Get<ShellDispatcher>();

    internal override bool WouldDispatch(string tag, IEnumerable<string> arguments)
    {
      return tag.Equals("/myonline");
    }

    internal override IPromise<Nothing> Dispatch(PlayerHandle sender, CommandDomain domain, string tag, IEnumerable<string> arguments)
    {
      var hoursOnline = TimeSpan.FromSeconds(sender.User.TotalSecondsOnline).TotalHours;
      var message = String.Format(Strings.YOU_HAVE_BEEN_ONLINE_FOR_HOURS, hoursOnline < 1 ? "< 1" : Convert.ToUInt32(hoursOnline).ToString());
      return this.dispatcher.Output(domain, message);
    }
  }
}
