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
    internal Message originalMessage;

    internal virtual string DebugDescription()
    {
      return originalMessage.DebugDescription();
    }

    internal abstract IPromise<Nothing> Process(PlayerHandle sender, ChatManager server);

    internal virtual bool Validate(PlayerHandle sender, ChatManager server)
    {
      return sender.IsLoggedIn;
    }

    internal static ARequest Parse(PlayerHandle player, ChatManager server, Message ev)
    {
      ARequest request = null;
      switch (ev.name)
      {
        case "login":
          request = Requests.Login.Parse(ev);
          break;
        case "joinroom":
          request = Requests.LobbyJoin.Parse(ev);
          break;
        case "leaveroom":
          request = Requests.LobbyLeave.Parse(ev);
          break;
        case "openroom":
          request = Requests.RoomOpen.Parse(ev);
          break;
        case "closeroom":
          request = Requests.RoomClose.Parse(ev);
          break;
        case "joingroup":
          request = Requests.GroupJoin.Parse(ev);
          break;
        case "leavegroup":
          request = Requests.GroupLeave.Parse(ev);
          break;
        case "getUserRoomList":
          request = Requests.RoomList.Parse(ev);
          break;
        case "sendgroup":
          request = Requests.SendGroup.Parse(ev);
          break;
        case "sendroom":
          request = Requests.SendLocal.Parse(ev);
          break;
        case "sendworld":
          request = Requests.SendWorld.Parse(ev);
          break;
        case "private":
          request = Requests.SendPM.Parse(ev);
          break;
        case "friend":
          request = Requests.FriendRequest.Parse(ev);
          break;
        case "getonline":
          request = Requests.GetOnline.Parse(ev);
          break;
        case "order":
          request = Requests.Order.Parse(ev);
          break;
        default:
          break;
      }

      if (request != null)
      {
        request.originalMessage = ev;
      }

      return request;
    }
  }
}
