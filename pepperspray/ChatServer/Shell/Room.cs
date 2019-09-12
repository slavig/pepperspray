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
        return dispatcher.Error(sender, server, "Invalid usage.");
      }

      var userRoom = server.World.FindUserRoom(sender);
      if (userRoom == null)
      {
        return dispatcher.Error(sender, server, "Room not found.");
      }

      var command = arguments.ElementAt(0);
      if (command.Equals("persist"))
      {
        if (!this.config.DanglingRoom.Enabled)
        {
          return dispatcher.Error(sender, server, "This feature is not enabled on the server.");
        }

        userRoom.IsSemiPersistent = true;

        var message = String.Format("Room is now persistent. It will stay opened for {0} minutes after you log-off. "
          + "Remember that you can only close it by \"/room close\".", this.config.DanglingRoom.Timeout.TotalMinutes);
        return dispatcher.Output(sender, server, message);
      } 
      else if (command.Equals("close"))
      {
        return this.userRoomService.CloseRoom(userRoom)
          .Then((a) => dispatcher.Output(sender, server, "Room now closed."));
      }
      else if (command.Equals("sex"))
      {
        userRoom.IsSexAllowed = !arguments.ElementAt(1).Equals("forbid");
        return dispatcher.Output(sender, server, "Sex is now {0} in this room.", userRoom.IsSexAllowed ? "allowed" : "forbidden");
      }
      else
      {
        return dispatcher.Error(sender, server, "Uknown command.");
      }
    }
  }
}
