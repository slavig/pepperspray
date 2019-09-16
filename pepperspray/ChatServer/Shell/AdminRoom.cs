using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Serilog;
using RSG;
using pepperspray.CIO;
using pepperspray.ChatServer.Game;
using pepperspray.ChatServer.Services;
using pepperspray.ChatServer.Protocol;
using pepperspray.Utils;
using pepperspray.SharedServices;
using pepperspray.Resources;

namespace pepperspray.ChatServer.Shell
{
  internal class AdminRoom: AShellCommand
  {
    private UserRoomService userRoomService = DI.Get<UserRoomService>();
    internal override bool RequireAdmin()
    {
      return true;
    }

    internal override bool WouldDispatch(string tag)
    {
      return tag.Equals("aroom");
    }

    internal override IPromise<Nothing> Dispatch(ShellDispatcher dispatcher, PlayerHandle sender, ChatManager server, string tag, IEnumerable<string> arguments)
    {
      if (arguments.Count() < 1 || (arguments.ElementAt(0) == "priority" && arguments.Count() < 2))
      {
        return dispatcher.InvalidUsage(sender, server);
      }

      var command = arguments.ElementAt(0);
      if (command.Equals("priority"))
      {
        var id = arguments.ElementAt(1).Trim();
        var room = server.World.FindUserRoom(id);
        if (room == null)
        {
          return dispatcher.Error(sender, server, Strings.ROOM_HAS_NOT_BEEN_FOUND, id);
        }

        room.IsPrioritized = !room.IsPrioritized;
        return dispatcher.Output(sender, server, room.IsPrioritized ? Strings.ROOM_NOW_PRIORITIZED : Strings.ROOM_NOW_NOT_PRIORITIZED);
      }
      else if (command.Equals("close"))
      {
        var id = arguments.ElementAt(1).Trim();
        var room = server.World.FindUserRoom(id);
        if (room == null)
        {
          return dispatcher.Error(sender, server, Strings.ROOM_HAS_NOT_BEEN_FOUND, id);
        }

        return this.userRoomService.CloseRoom(room).Then(a => dispatcher.Output(sender, server, Strings.ROOM_CLOSED));
      }
      else
      {
        return dispatcher.Error(sender, server, Strings.UNKNOWN_COMMAND, command);
      }
    }
  }
}
