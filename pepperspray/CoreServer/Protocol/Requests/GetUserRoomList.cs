using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RSG;
using pepperspray.CIO;
using pepperspray.CoreServer.Game;
using ThreeDXChat.Networking.NodeNet;

namespace pepperspray.CoreServer.Protocol.Requests
{
  internal class GetUserRoomList: ARequest
  {
    internal static GetUserRoomList Parse(NodeServerEvent ev)
    {
      return new GetUserRoomList();
    }

    internal override IPromise<Nothing> Process(PlayerHandle sender, CoreServer server)
    {
      List<UserRoom> list = null;
      lock (server)
      {
        list = server.World.PublicRooms.ToList();
      }

      return sender.Send(Responses.UserRoomList(list));
    }
  }
}
