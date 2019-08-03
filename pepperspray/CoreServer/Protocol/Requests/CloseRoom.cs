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
  internal class CloseRoom: ARequest
  {
    private string identifier;

    internal static CloseRoom Parse(Message ev)
    {
      return new CloseRoom
      {
        identifier = ev.data.ToString()
      };
    }

    internal override bool Validate(PlayerHandle sender, CoreServer server)
    {
      if (!base.Validate(sender, server))
      {
        return false;
      }

      if (!this.identifier.StartsWith(sender.Name))
      {
        return false;
      }

      return server.World.FindUserRoom(this.identifier) != null;
    }

    internal override IPromise<Nothing> Process(PlayerHandle sender, CoreServer server)
    {
      PlayerHandle[] lobbyPlayers = new PlayerHandle[] { };
      UserRoom userRoom = null;

      lock (server)
      {
        var lobby = server.World.FindLobby(this.identifier);
        if (lobby != null)
        {
          lobbyPlayers = lobby.Players.ToArray();
        }

        userRoom = server.World.FindUserRoom(this.identifier);
        if (userRoom == null)
        {
          return Nothing.Resolved();
        }

        server.World.RemoveUserRoom(this.identifier);
      }

      return new CombinedPromise<Nothing>(lobbyPlayers.Select(a => a.Stream.Write(Responses.UserRoomClosed(userRoom))));
    }
  }
}
