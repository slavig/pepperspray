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
  internal class RoomList: ARequest
  {
    internal static RoomList Parse(Message ev)
    {
      return new RoomList();
    }

    internal override IPromise<Nothing> Process(PlayerHandle sender, CoreServer server)
    {
      Message response = null;
      lock (server)
      {
        response = Responses.UserRoomList(server.World.Lobbies.Values);
      }

      return sender.Stream.Write(response);
    }
  }
}
