using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RSG;
using pepperspray.CIO;
using pepperspray.ChatServer.Game;
using pepperspray.ChatServer.Protocol;

namespace pepperspray.ChatServer.Shell
{
  class PrivateMessage: AShellCommand
  {
    internal override bool WouldDispatch(string tag)
    {
      return tag.Equals("pm");
    }

    internal override IPromise<Nothing> Dispatch(ShellDispatcher dispatcher, PlayerHandle sender, ChatManager server, string tag, IEnumerable<string> arguments)
    {
      if (arguments.Count() != 1)
      {
        return dispatcher.Error(sender, server, "Invalid arguments");
      }

      lock (server)
      {
        var player = server.World.FindPlayer(arguments.First());
        if (player != null)
        {
          return sender.Stream.Write(Responses.PrivateChatMessage(player, "Player is online, you can message him."));
        } 
        else
        {
          return dispatcher.Error(sender, server, "Player is offline.");
        }
      }
    }
  }
}
