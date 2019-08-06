using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RSG;
using Serilog;
using pepperspray.CIO;
using pepperspray.CoreServer.Game;
using pepperspray.CoreServer.Protocol;
using pepperspray.Utils;
using pepperspray.SharedServices;

namespace pepperspray.CoreServer
{
  internal class EventDispatcher
  {
    private CoreServer server = DI.Get<CoreServer>();

    internal IPromise<Nothing> Dispatch(PlayerHandle client, Message eventMsg)
    {
      ARequest request = ARequest.Parse(client, this.server, eventMsg);

      if (request == null)
      {
        Log.Warning("Failed to parse request from {name}/{hash}/{address}: {msg_name} \"{msg_data}\"",
          client.Name,
          client.Stream.ConnectionHash,
          client.Stream.ConnectionEndpoint,
          eventMsg.name,
          eventMsg.DebugDescription());

        return Nothing.Resolved();
      }

      if (!request.Validate(client, this.server))
      {
        Log.Warning("Failed to validate request from {name}/{hash}/{address}: {msg_name} \"{msg_data}\"",
          client.Name,
          client.Stream.ConnectionHash,
          client.Stream.ConnectionEndpoint,
          eventMsg.name,
          eventMsg.DebugDescription());

        return Nothing.Resolved();
      }

      return request.Process(client, this.server);
    }
  }
}
