using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RSG;
using pepperspray.CIO;
using pepperspray.ChatServer.Game;
using pepperspray.ChatServer.Services;
using pepperspray.Utils;
using pepperspray.SharedServices;

namespace pepperspray.ChatServer.Protocol.Requests
{
  internal class RoomClose: ARequest
  {
    private string identifier;
    private UserRoom userRoom;

    private UserRoomService userRoomService = DI.Auto<UserRoomService>();

    internal static RoomClose Parse(Message ev)
    {
      return new RoomClose
      {
        identifier = ev.data.ToString()
      };
    }

    internal override bool Validate(PlayerHandle sender, ChatManager server)
    {
      if (!base.Validate(sender, server))
      {
        return false;
      }

      if (!this.identifier.StartsWith(sender.Name))
      {
        return false;
      }

      lock (server)
      {
        this.userRoom = server.World.FindUserRoom(this.identifier);
      }

      return this.userRoom != null;
    }

    internal override IPromise<Nothing> Process(PlayerHandle sender, ChatManager server)
    {
      return this.userRoomService.CloseRoom(sender, server, this.userRoom);
    }
  }
}
