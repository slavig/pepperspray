using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RSG;
using pepperspray.CIO;
using pepperspray.CoreServer.Game;
using pepperspray.CoreServer.Protocol;
using pepperspray.Utils;
using pepperspray.SharedServices;

namespace pepperspray.CoreServer.Services
{
  internal class UserRoomService
  {
    private CoreServer server = DI.Get<CoreServer>();

    internal IPromise<Nothing> OpenRoom(PlayerHandle sender, CoreServer server, UserRoom room) 
    {
      lock(server)
      {
        server.World.AddUserRoom(room);
      }

      return Nothing.Resolved();
    }

    internal IPromise<Nothing> CloseRoom(PlayerHandle sender, CoreServer server, UserRoom room)
    {
      PlayerHandle[] lobbyPlayers = new PlayerHandle[] { };

      lock (server)
      {
        var lobby = server.World.FindLobby(room.Identifier);
        if (lobby != null)
        {
          lobbyPlayers = lobby.Players.ToArray();
        }

        server.World.RemoveUserRoom(room.Identifier);
      }

      return new CombinedPromise<Nothing>(lobbyPlayers.Select(a => a.Stream.Write(Responses.UserRoomClosed(room))));
    }

    internal IPromise<Nothing> PlayerLoggedOff(PlayerHandle sender)
    {
      if (!sender.IsLoggedIn)
      {
        return Nothing.Resolved();
      }

      lock(this.server)
      {
        var room = this.server.World.FindUserRoom(sender);
        if (room != null)
        {
          return this.CloseRoom(sender, this.server, room);
        }
        else
        {
          return Nothing.Resolved();
        }
      }
    }
  }
}
