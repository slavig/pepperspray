﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Serilog;
using RSG;
using pepperspray.CIO;
using pepperspray.CoreServer.Game;
using pepperspray.CoreServer.Protocol;
using pepperspray.Utils;

namespace pepperspray.CoreServer.Shell
{
  internal class Help: AShellCommand
  {
    internal override bool WouldDispatch(string tag)
    {
      return tag.Equals("help");
    }

    internal override IPromise<Nothing> Dispatch(ShellDispatcher dispatcher, PlayerHandle sender, CoreServer server, string tag, IEnumerable<string> arguments)
    {
      return dispatcher.Output(sender, server, "Following commands are available: ")
        .Then(a => dispatcher.Output(sender, server, "/players - show how many players are at locations"))
        .Then(a => dispatcher.Output(sender, server, "/pm PLAYER - open private message tab if player is online."));
    }
  }
}