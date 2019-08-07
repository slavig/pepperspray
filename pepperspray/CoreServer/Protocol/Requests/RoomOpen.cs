using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RSG;
using pepperspray.CIO;
using pepperspray.CoreServer.Game;
using pepperspray.CoreServer.Services;
using pepperspray.Utils;
using pepperspray.SharedServices;

namespace pepperspray.CoreServer.Protocol.Requests
{
  internal class RoomOpen: ARequest
  {
    private string lobbyIdentifier;
    private string name;
    private int numberOfPlayers;
    private UserRoom.AccessType accessType;

    private UserRoomService userRoomService = DI.Auto<UserRoomService>();

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

      var lobbyIdentifier = arguments[0].ToString().Trim();
      var name = arguments[3].ToString().Trim();

      if (lobbyIdentifier.Count() == 0 || name.Count() == 0)
      {
        return null;
      }

      UserRoom.AccessType accessType;
      switch (System.Convert.ToInt32(arguments[2].ToString()))
      {
        case 0:
        case 1:
          accessType = UserRoom.AccessType.ForAll;
          break;
        case 2:
          accessType = UserRoom.AccessType.ForGroup;
          break;
        default:
          return null;
      }

      return new RoomOpen
      {
        lobbyIdentifier = lobbyIdentifier,
        name = name,
        numberOfPlayers = 0,
        accessType = accessType,
      };
    }

    internal override bool Validate(PlayerHandle sender, CoreServer server)
    {
      if (!base.Validate(sender, server))
      {
        return false;
      }

      if (!this.lobbyIdentifier.StartsWith(sender.Name))
      {
        return false;
      }

      return true;
    }

    internal override IPromise<Nothing> Process(PlayerHandle sender, CoreServer server)
    {
      var room = new UserRoom
      {
        Identifier = this.lobbyIdentifier,
        User = sender,
        Name = this.name,
        Access = this.accessType,
        NumberOfPlayers = this.numberOfPlayers
      };

      return this.userRoomService.OpenRoom(sender, server, room);
    }
  }
}
