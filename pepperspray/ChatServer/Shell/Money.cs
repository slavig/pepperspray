using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RSG;
using pepperspray.CIO;
using pepperspray.ChatServer.Game;
using pepperspray.ChatServer.Protocol;
using pepperspray.SharedServices;
using pepperspray.Resources;

namespace pepperspray.ChatServer.Shell
{
  internal class Money: AShellCommand
  {
    private GiftsService giftService = DI.Get<GiftsService>();
    private ShellDispatcher dispatcher = DI.Get<ShellDispatcher>();

    internal override bool WouldDispatch(string tag, IEnumerable<string> arguments)
    {
      return tag.Equals("/money");
    }

    internal override IPromise<Nothing> Dispatch(PlayerHandle sender, CommandDomain domain, string tag, IEnumerable<string> arguments)
    {
      return this.dispatcher.Output(domain, Strings.YOU_CURRENTLY_HAVE_COINTS, this.giftService.GetCurrency(sender.User));
    }
  }
}
