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
  internal class RoomOpen: ARequest
  {
    private string identifier;
    private string name;
    private string type;
    private UserRoom.AccessType accessType;

    private UserRoomService userRoomService = DI.Get<UserRoomService>();
    private Configuration config = DI.Get<Configuration>();

    internal static RoomOpen Parse(Message ev)
    {
      if (ev.data is List<object> == false)
      {
        return null;
      }

      var arguments = ev.data as List<object>;
      if (arguments.Count() < 4)
      {
        return null;
      }

      var lobbyIdentifier = CharacterService.StripCharacterName(arguments[0].ToString().Trim());
      var name = arguments[3].ToString().Trim();

      if (lobbyIdentifier.Count() == 0 || name.Count() == 0)
      {
        return null;
      }

      UserRoom.AccessType accessType;
      switch (System.Convert.ToInt32(arguments[2].ToString()))
      {
        case 0:
          accessType = UserRoom.AccessType.ForAll;
          break;
        case 1:
          accessType = UserRoom.AccessType.ForFriends;
          break;
        case 2:
          accessType = UserRoom.AccessType.ForGroup;
          break;
        default:
          return null;
      }

      var roomType = arguments[1].ToString().Trim();
      if (!Lobby.IsIdentifierFromDefaultPool(roomType) && !roomType.Equals("house"))
      {
        return null;
      }

      return new RoomOpen
      {
        identifier = lobbyIdentifier,
        name = name,
        accessType = accessType,
        type = roomType,
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

      return true;
    }

    internal override IPromise<Nothing> Process(PlayerHandle sender, ChatManager server)
    {
      var room = new UserRoom
      {
        Identifier = this.identifier,
        OwnerId = sender.Id,
        OwnerName = sender.Name,
        Name = this.userRoomService.CleanupName(this.name),
        Type = this.type,
        Access = this.accessType,
        SlowmodeInterval = this.config.LocalChatIntermessageInterval,
      };

      return this.userRoomService.OpenRoom(room);
    }
  }
}
