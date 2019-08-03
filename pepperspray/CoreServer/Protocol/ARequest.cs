using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RSG;
using pepperspray.CIO;
using pepperspray.CoreServer.Game;
using ThreeDXChat.Networking.NodeNet;

namespace pepperspray.CoreServer.Protocol
{
  internal abstract class ARequest
  {
    internal abstract IPromise<Nothing> Process(PlayerHandle sender, CoreServer server);

    internal virtual bool Validate(PlayerHandle sender, CoreServer server)
    {
      return sender.IsLoggedIn;
    }

    internal static ARequest Parse(PlayerHandle player, CoreServer server, Message ev)
    {
      switch (ev.name)
      {
        case "login":
          return Requests.Login.Parse(ev);
        case "joinroom":
          return Requests.JoinRoom.Parse(ev);
        case "leaveroom":
          return Requests.LeaveRoom.Parse(ev);
        case "openroom":
          return Requests.OpenRoom.Parse(ev);
        case "closeroom":
          return Requests.CloseRoom.Parse(ev);
        case "getUserRoomList":
          return Requests.GetUserRoomList.Parse(ev);
        case "sendroom":
          return Requests.SendLocal.Parse(ev);
        case "sendworld":
          return Requests.SendWorld.Parse(ev);
        case "private":
          return Requests.SendPM.Parse(ev);
        default:
          return null;
      }
    }
  }
}
