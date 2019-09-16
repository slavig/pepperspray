using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using pepperspray.ChatServer.Game;
using pepperspray.SharedServices;
using pepperspray.Utils;

namespace pepperspray.ChatServer.Protocol
{
  internal class Responses
  {
    internal static Message Connected()
    {
      return new Message("srv", "connected");
    }

    internal static Message UserRoomList(IEnumerable<UserRoom> roomList)
    {
      var builder = new StringBuilder("userroomlist=");
      foreach (var room in roomList)
      {
        builder.AppendFormat("{0}+", Responses.UserRoomRecord(room));
      }

      return new Message("srv", builder.ToString());
    }

    internal static string UserRoomRecord(UserRoom room)
    {
      int accessType = 0;
      switch (room.Access)
      {
        case UserRoom.AccessType.ForAll:
          accessType = 0;
          break;
        case UserRoom.AccessType.ForFriends:
          accessType = 1;
          break;
        case UserRoom.AccessType.ForGroup:
          accessType = 2;
          break;
      }

      bool isPrioritized = room.IsPrioritized;
      if (room.IsPermanent || room.IsVisibilityRestricted)
      {
        isPrioritized = true;
      }

      return String.Format("{0}|{1}|house|{2}|{3}|{4}|{5}|{6}|",
        room.OwnerName,
        room.Identifier,
        accessType,
        room.NumberOfPlayers >= 0 ? room.NumberOfPlayers : 0,
        room.Name,
        isPrioritized ? "True" : "False",
        room.OwnerId);
    }

    internal static Message UserRoomClosed(UserRoom room)
    {
      return new Message("srv", "userroomclosed=" + room.Identifier);
    }

    internal static Message OrderOk()
    {
      return new Message("srv", "order=ok=movetop_room");
    }

    internal static Message JoinedRoom(Lobby lobby)
    {
      return new Message("srv", "joinedroom=" + lobby.Identifier + "=");
    }

    internal static Message JoinedLobby()
    {
      return new Message("srv", "joinedlobby");
    }

    internal static Message MyGroup(Group group)
    {
      return new Message("srv", "mygroup=" + group.Identifier);
    }

    internal static Message GroupList(IEnumerable<PlayerHandle> players)
    {
      var builder = new StringBuilder("grouplist=");
      foreach (var player in players)
      {
        builder.AppendFormat("{0}+", player.Name);
      }

      return new Message("srv", builder.ToString());
    }

    internal static Message GroupAdd(PlayerHandle player)
    {
      return new Message("srv", String.Format("groupadd={0}={1}", player.Name, player.Sex));
    }

    internal static Message GroupLeave(PlayerHandle player)
    {
      return new Message("srv", String.Format("groupleave={0}=", player.Name));
    }

    internal static Message NewPlayer(PlayerHandle player)
    {
      return new Message("srv", "newplayer=" + player.Id + "=" + player.Name + "=" + player.Sex);
    }

    internal static Message PlayerLeave(PlayerHandle player)
    {
      return new Message("srv", "playerleave=" + player.Name);
    }

    internal static Message Friend(PlayerHandle player, PlayerHandle recepient)
    {
      var payload = String.Format("{0},{1}",
        recepient.Id,
        player.Id
      );

      return new Message("srv2", new Dictionary<string, object>
      {
        { "event", "friend" },
        { "id", player.Id.ToString() },
        { "name", player.Name },
        { "sex", player.Sex },
        { "token", payload },
      });
    }

    internal static Message OnlineUsers(IEnumerable<uint> ids)
    {
      return new Message("srv", "onlineusers=" + String.Join("|", ids) + "|");
    }

    internal static Message Message(PlayerHandle sender, string contents) {
      var name = sender.Name;

      if (sender.Character.ChatNameDecoration != null)
      {
        if (contents.StartsWith("~world") || contents.StartsWith("~chat") || contents.StartsWith("~group"))
        {
          name = String.Format(sender.Character.ChatNameDecoration, name);
        }
      }

      return new Message("msg", new Dictionary<string, object>
        {
          { "name", name },
          { "id", sender.Id.ToString() },
          { "data", contents },
        }
      );
    }

    internal static Message PrivateChatMessage(PlayerHandle sender, string contents)
    {
      return Responses.Message(sender, "~private/" + contents);
    }

    internal static Message ServerPrivateChatMessage(string senderName, uint senderId, string contents)
    {
      return new Message("msg", new Dictionary<string, object>
      {
        { "name", senderName },
        { "id", senderId.ToString() },
        { "data", "~private/" + contents },
      });
    }

    internal static Message ServerMessage(ChatManager server, string contents) {
      return new Message("msg", new Dictionary<string, object>
        {
          { "name", server.Monogram },
          { "id", "0" },
          { "data", "~private/" + contents }
        }
      );
    }

    internal static Message ServerWorldMessage(ChatManager server, string contents) {
      return new Message("msg", new Dictionary<string, object>
        {
          { "name", server.Monogram },
          { "id", "0" },
          { "data", "~worldchat/" + contents }
        }
      );
    }

    internal static Message MakeshiftAlert(string name)
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

    internal static Message CharacterAppearance(PlayerHandle player)
    {
      return Responses.Message(player, "~action2/charData|" + player.Character.Appearance);
    }

    internal static Message RunRadio(ChatManager server, string senderName, string url)
    {
      return new Message("msg", new Dictionary<string, object>
      {
          { "name", senderName },
          { "id", "0" },
          { "data", "~action2/runRadio|" + url }
      });
    }
  }
}
