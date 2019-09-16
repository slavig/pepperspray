﻿using System;
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
      var promises = new List<IPromise<Nothing>>();
      foreach (var line in Strings.SHELL_HELP_TEXT.Split('\n'))
      {
        promises.Add(dispatcher.Output(sender, server, line));
      }

      return new CombinedPromise<Nothing>(promises);
    }
  }
}
