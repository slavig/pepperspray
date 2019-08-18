using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RSG;
using pepperspray.CIO;
using pepperspray.ChatServer.Game;
using pepperspray.ChatServer.Services;
using pepperspray.SharedServices;

namespace pepperspray.ChatServer.Protocol.Requests
{
  internal class RoomList: ARequest
  {
    private UserRoomService userRoomService = DI.Get<UserRoomService>();

    internal static RoomList Parse(Message ev)
    {
      return new RoomList();
    }

    internal override IPromise<Nothing> Process(PlayerHandle sender, ChatManager server)
    {
      return this.userRoomService.ListRooms(sender);
    }
  }
}
