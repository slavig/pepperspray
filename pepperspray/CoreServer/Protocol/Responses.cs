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
    internal static NodeServerEvent Connected()
    {
      return new NodeServerEvent
      {
        name = "srv",
        data = "connected"
      };
    }

    internal static NodeServerEvent UserRoomList(IEnumerable<UserRoom> list)
    {
      var builder = new StringBuilder("userroomlist=");
      foreach (UserRoom room in list)
      {
        builder.AppendFormat("{0}|{1}|house|0|{2}|{3}|False|{4}|+", room.User.Name, room.Identifier, room.NumberOfPlayers, room.Name, room.User.Id);
      }

      return new NodeServerEvent
      {
        name = "srv",
        data = builder.ToString()
      };
    }

    internal static NodeServerEvent JoinedRoom(Lobby lobby)
    {
      return new NodeServerEvent
      {
        name = "srv",
        data = "joinedroom=" + lobby.Identifier + "="
      };
    }

    internal static NodeServerEvent JoinedLobby()
    {
      return new NodeServerEvent
      {
        name = "srv",
        data = "joinedlobby"
      };
    }

    internal static NodeServerEvent NewPlayer(PlayerHandle player)
    {
      return new NodeServerEvent
      {
        name = "srv",
        //data = "newplayer=" + player.Name + "=" + player.Sex + "="
        data = "newplayer=" + player.Id + "=" + player.Name + "=" + player.Sex
      };
    }

    internal static NodeServerEvent PlayerLeave(PlayerHandle player)
    {
      return new NodeServerEvent
      {
        name = "srv",
        data = "playerleave=" + player.Name
      };
    }

    internal static NodeServerEvent Message(PlayerHandle sender, string contents) {
      return new NodeServerEvent
      {
        name = "msg",
        data = new Dictionary<string, object>
        {
          { "name", sender.Name },
          { "id", sender.Hash },
          { "data", contents }
        }
      };
    }
  }
}
