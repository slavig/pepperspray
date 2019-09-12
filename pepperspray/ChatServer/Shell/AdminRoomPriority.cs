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

namespace pepperspray.ChatServer.Shell
{
  internal class AdminRoomPriority: AShellCommand
  {
    internal override bool RequireAdmin()
    {
      return true;
    }

    internal override bool WouldDispatch(string tag)
    {
      return tag.Equals("aroompriority");
    }

    internal override IPromise<Nothing> Dispatch(ShellDispatcher dispatcher, PlayerHandle sender, ChatManager server, string tag, IEnumerable<string> arguments)
    {
      if (arguments.Count() < 1)
      {
        return dispatcher.Error(sender, server, "Invalid arguments");
      }

      var id = arguments.ElementAt(0) + "_room";
      var room = server.World.FindUserRoom(id);
      if (room == null)
      {
        return dispatcher.Error(sender, server, "Room {0} has not been found.", id);
      }

      room.IsPrioritized = !room.IsPrioritized;
      return dispatcher.Output(sender, server, room.IsPrioritized ? "Room now prioritized." : "Room now not prioritized");
    }
  }
}
