using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RSG;
using pepperspray.CIO;
using pepperspray.CoreServer.Game;
using pepperspray.CoreServer.Protocol;
using ThreeDXChat.Networking.NodeNet;

namespace pepperspray.CoreServer
{
  internal class EventDispatcher
  {
    private CoreServer server;

    internal EventDispatcher(CoreServer server)
    {
      this.server = server;
    }

    internal IPromise<Nothing> Dispatch(PlayerHandle client, NodeServerEvent eventMsg)
    {
      ARequest request = ARequest.Parse(client, this.server, eventMsg);

      if (request == null)
      {
        Console.WriteLine("Invalid request");
        return Nothing.Resolved();
      }

      if (!request.Validate(client, this.server))
      {
        Console.WriteLine("Unvalidated request");
        return Nothing.Resolved();
      }

      return request.Process(client, this.server);
    }
  }
}
