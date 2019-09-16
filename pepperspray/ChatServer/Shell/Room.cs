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
using pepperspray.ChatServer.Services;
using pepperspray.SharedServices;
using pepperspray.Utils;
using pepperspray.Resources;

namespace pepperspray.ChatServer.Shell
{
  internal class Room: AShellCommand
  {
    private UserRoomService userRoomService = DI.Get<UserRoomService>();
    private Configuration config = DI.Get<Configuration>();
    
    internal override bool WouldDispatch(string tag)
    {
      return tag.Equals("room");
    }

    internal override IPromise<Nothing> Dispatch(ShellDispatcher dispatcher, PlayerHandle sender, ChatManager server, string tag, IEnumerable<string> arguments)
    {
      if (arguments.Count() < 1 || arguments.ElementAt(0).Equals("sex") && arguments.Count() < 2)
      {
        return dispatcher.InvalidUsage(sender, server);
      }

      var userRoom = server.World.FindUserRoom(sender);
      if (userRoom == null)
      {
        return dispatcher.Error(sender, server, Strings.YOU_DONT_CURRENTLY_OWN_A_ROOM);
      }

      var command = arguments.ElementAt(0);
      if (command.Equals("persist"))
      {
        if (!this.config.DanglingRoom.Enabled)
        {
          return dispatcher.Error(sender, server, Strings.THIS_FEATURE_IS_NOT_ENABLED);
        }

        userRoom.IsSemiPersistent = true;

        var message = String.Format(Strings.ROOM_IS_NOW_PERSISTENT, this.config.DanglingRoom.Timeout.TotalMinutes);
        return dispatcher.Output(sender, server, message);
      } 
      else if (command.Equals("close"))
      {
        return this.userRoomService.CloseRoom(userRoom)
          .Then((a) => dispatcher.Output(sender, server, Strings.ROOM_CLOSED));
      }
      else if (command.Equals("sex"))
      {
        userRoom.IsSexAllowed = !arguments.ElementAt(1).Equals("forbid");
        return dispatcher.Output(sender, server, userRoom.IsSexAllowed ? Strings.SEX_IS_NOW_ALLOWED_IN_ROOM : Strings.SEX_IS_FORBIDDEN_IN_ROOM);
      }
      else
      {
        return dispatcher.Error(sender, server, Strings.UNKNOWN_COMMAND, command);
      }
    }
  }
}
