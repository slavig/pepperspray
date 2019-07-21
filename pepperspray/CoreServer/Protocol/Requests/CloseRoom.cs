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
    private string lobbyIdentifier;

    internal static CloseRoom Parse(NodeServerEvent ev)
    {
      return new CloseRoom
      {
        lobbyIdentifier = ev.data.ToString()
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

      return server.World.FindUserRoom(this.lobbyIdentifier) != null;
    }

    internal override IPromise<Nothing> Process(PlayerHandle sender, CoreServer server)
    {
      lock(server)
      {
        server.World.RemoveUserRoom(this.lobbyIdentifier);
      }

      return Nothing.Resolved();
    }
  }
}
