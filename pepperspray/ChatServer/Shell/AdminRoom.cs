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
    private ShellDispatcher dispatcher = DI.Get<ShellDispatcher>();
    private ChatManager manager = DI.Get<ChatManager>();

    internal override bool HasPermissionToExecute(PlayerHandle sender)
    {
      return sender.AdminOptions.HasFlag(AdminFlags.RoomManagement);
    }

    internal override bool WouldDispatch(string tag, IEnumerable<string> arguments)
    {
      return tag.Equals("/aroom");
    }

    internal override IPromise<Nothing> Dispatch(PlayerHandle sender, CommandDomain domain, string tag, IEnumerable<string> arguments)
    {
      if (arguments.Count() < 1 || (arguments.ElementAt(0) == "priority" && arguments.Count() < 2))
      {
        return this.dispatcher.InvalidUsage(sender);
      }

      var command = arguments.ElementAt(0);
      if (command.Equals("priority"))
      {
        var id = arguments.ElementAt(1).Trim();
        var room = this.manager.World.FindUserRoom(id);
        if (room == null)
        {
          return this.dispatcher.Error(sender, Strings.ROOM_HAS_NOT_BEEN_FOUND, id);
        }

        room.IsPrioritized = !room.IsPrioritized;
        return this.dispatcher.Output(sender, room.IsPrioritized ? Strings.ROOM_NOW_PRIORITIZED : Strings.ROOM_NOW_NOT_PRIORITIZED);
      }
      else if (command.Equals("close"))
      {
        var id = arguments.ElementAt(1).Trim();
        var room = this.manager.World.FindUserRoom(id);
        if (room == null)
        {
          return this.dispatcher.Error(sender, Strings.ROOM_HAS_NOT_BEEN_FOUND, id);
        }

        return this.userRoomService.CloseRoom(room).Then(a => this.dispatcher.Output(sender, Strings.ROOM_CLOSED));
      }
      else
      {
        return this.dispatcher.Error(sender, Strings.UNKNOWN_COMMAND, command);
      }
    }
  }
}
