using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RSG;
using pepperspray.CIO;
using pepperspray.ChatServer.Game;
using ThreeDXChat.Networking.NodeNet;

namespace pepperspray.ChatServer.Protocol
{
  internal abstract class ARequest
  {
    internal abstract IPromise<Nothing> Process(PlayerHandle sender, ChatManager server);

    internal virtual bool Validate(PlayerHandle sender, ChatManager server)
    {
      return sender.IsLoggedIn;
    }

    internal static ARequest Parse(PlayerHandle player, ChatManager server, Message ev)
    {
      switch (ev.name)
      {
        case "login":
          return Requests.Login.Parse(ev);
        case "joinroom":
          return Requests.LobbyJoin.Parse(ev);
        case "leaveroom":
          return Requests.LobbyLeave.Parse(ev);
        case "openroom":
          return Requests.RoomOpen.Parse(ev);
        case "closeroom":
          return Requests.RoomClose.Parse(ev);
        case "joingroup":
          return Requests.GroupJoin.Parse(ev);
        case "leavegroup":
          return Requests.GroupLeave.Parse(ev);
        case "getUserRoomList":
          return Requests.RoomList.Parse(ev);
        case "sendgroup":
          return Requests.SendGroup.Parse(ev);
        case "sendroom":
          return Requests.SendLocal.Parse(ev);
        case "sendworld":
          return Requests.SendWorld.Parse(ev);
        case "private":
          return Requests.SendPM.Parse(ev);
        case "friend":
          return Requests.FriendRequest.Parse(ev);
        case "getonline":
          return Requests.GetOnline.Parse(ev);
        case "order":
          return Requests.Order.Parse(ev);
        default:
          return null;
      }
    }
  }
}
