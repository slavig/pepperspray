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

namespace pepperspray.ChatServer.Shell
{
  internal class Help: AShellCommand
  {
    internal override bool WouldDispatch(string tag)
    {
      return tag.Equals("help");
    }

    internal override IPromise<Nothing> Dispatch(ShellDispatcher dispatcher, PlayerHandle sender, ChatManager server, string tag, IEnumerable<string> arguments)
    {
      return dispatcher.Output(sender, server, "Following commands are available: ")
        .Then(a => dispatcher.Output(sender, server, "/players - show how many players are at locations."))
        .Then(a => dispatcher.Output(sender, server, "/pm PLAYER - open private message tab if player is online."))
        .Then(a => dispatcher.Output(sender, server, "/money - get amount of coints you have right now."))
        .Then(a => dispatcher.Output(sender, server, "/pay PLAYER AMOUNT - transfer currency to other player."))
        .Then(a => dispatcher.Output(sender, server, "/expel PLAYER MINUTES - expel player from your location for MINUTES. \"0\" can be given as duration to simply kick him."))
        .Then(a => dispatcher.Output(sender, server, "/room persist - will turn your room into semi-persistent one, meaning that it will be available for some time after you disconnect."))
        .Then(a => dispatcher.Output(sender, server, "/room close - will close your currently shared room."))
        .Then(a => dispatcher.Output(sender, server, "/room sex VALUE - \"allow\" to allow sex in your room, \"forbid\" to forbid it."))
      ;
    }
  }
}
