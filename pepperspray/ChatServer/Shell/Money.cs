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

namespace pepperspray.ChatServer.Shell
{
  internal class Money: AShellCommand
  {
    private GiftsService giftService = DI.Get<GiftsService>();

    internal override bool WouldDispatch(string tag)
    {
      return tag.Equals("money");
    }

    internal override IPromise<Nothing> Dispatch(ShellDispatcher dispatcher, PlayerHandle sender, ChatManager server, string tag, IEnumerable<string> arguments)
    {
      return dispatcher.Output(sender, server, "You currently have {0} coins.", this.giftService.GetCurrency(sender.User));
    }
  }
}
