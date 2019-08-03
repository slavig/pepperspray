using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using pepperspray.CoreServer.Game;
using ThreeDXChat.Networking.NodeNet;

namespace pepperspray.CoreServer.Protocol
{
  internal class Responses
  {
    internal static Message Connected()
    {
      return new Message("srv", "connected");
    }

    internal static Message UserRoomList(IEnumerable<UserRoom> list)
    {
      var builder = new StringBuilder("userroomlist=");
      foreach (UserRoom room in list)
      {
        builder.AppendFormat("{0}|{1}|house|0|{2}|{3}|False|{4}|+", room.User.Name, room.Identifier, room.NumberOfPlayers, room.Name, room.User.Id);
      }

      return new Message("srv", builder.ToString());
    }

    internal static Message UserRoomClosed(UserRoom room)
    {
      return new Message("srv", "userroomclosed=" + room.Identifier);
    }

    internal static Message JoinedRoom(Lobby lobby)
    {
      return new Message("srv", "joinedroom=" + lobby.Identifier + "=");
    }

    internal static Message JoinedLobby()
    {
      return new Message("srv", "joinedlobby");
    }

    internal static Message NewPlayer(PlayerHandle player)
    {
      return new Message("srv", "newplayer=" + player.Id + "=" + player.Name + "=" + player.Sex);
    }

    internal static Message PlayerLeave(PlayerHandle player)
    {
      return new Message("srv", "playerleave=" + player.Name);
    }

    internal static Message Message(PlayerHandle sender, string contents) {
      return new Message("msg", new Dictionary<string, object>
        {
          { "name", sender.Name },
          { "id", sender.Hash },
          { "data", contents }
        }
      );
    }

    internal static Message PrivateChatMessage(PlayerHandle sender, string contents)
    {
      return Responses.Message(sender, "~private/" + contents);
    }

    internal static Message ServerMessage(CoreServer server, string contents) {
      return new Message("msg", new Dictionary<string, object>
        {
          { "name", server.ServerName },
          { "id", "0" },
          { "data", "~private/" + contents }
        }
      );
    }

    internal static Message FriendAlert(string name)
    {
      return new Message("srv2", new Dictionary<string, object>
      {
        { "event", "friend" },
        { "name", name },
        { "sex", "m" },
        { "id", "0" },
        { "token", "0" },
      });
    }
  }
}
