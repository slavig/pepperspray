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
    private ShellDispatcher dispatcher = DI.Get<ShellDispatcher>();
    private ChatManager manager = DI.Get<ChatManager>();
    
    internal override bool WouldDispatch(string tag, IEnumerable<string> arguments)
    {
      return tag.Equals("/room");
    }

    internal override IPromise<Nothing> Dispatch(PlayerHandle sender, CommandDomain domain, string tag, IEnumerable<string> arguments)
    {
      if (arguments.Count() < 1 || arguments.ElementAt(0).Equals("sex") && arguments.Count() < 2)
      {
        return this.dispatcher.InvalidUsage(sender);
      }

      var userRoom = this.manager.World.FindUserRoom(sender);
      if (userRoom == null)
      {
        return this.dispatcher.Error(sender, Strings.YOU_DONT_CURRENTLY_OWN_A_ROOM);
      }

      var command = arguments.ElementAt(0);
      if (command.Equals("persist"))
      {
        if (!this.config.DanglingRoom.Enabled)
        {
          return this.dispatcher.Error(sender, Strings.THIS_FEATURE_IS_NOT_ENABLED);
        }

        userRoom.IsSemiPersistent = true;

        var message = String.Format(Strings.ROOM_IS_NOW_PERSISTENT, this.config.DanglingRoom.Timeout.TotalMinutes);
        return this.dispatcher.Output(sender, message);
      } 
      else if (command.Equals("close"))
      {
        return this.userRoomService.CloseRoom(userRoom)
          .Then((a) => this.dispatcher.Output(sender, Strings.ROOM_CLOSED));
      }
      else if (command.Equals("sex"))
      {
        userRoom.IsSexAllowed = !arguments.ElementAt(1).Equals("forbid");
        return this.dispatcher.Output(sender, userRoom.IsSexAllowed ? Strings.SEX_IS_NOW_ALLOWED_IN_ROOM : Strings.SEX_IS_NOW_FORBIDDEN_IN_ROOM);
      }
      else
      {
        return this.dispatcher.Error(sender, Strings.UNKNOWN_COMMAND, command);
      }
    }
  }
}
