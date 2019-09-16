using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RSG;
using Serilog;
using pepperspray.CIO;
using pepperspray.ChatServer.Game;
using pepperspray.ChatServer.Protocol;
using pepperspray.Utils;
using pepperspray.SharedServices;

namespace pepperspray.ChatServer
{
  internal class EventDispatcher: IDIService
  {
    private ChatManager server;

    public void Inject()
    {
      this.server = DI.Get<ChatManager>();
    }

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

      Log.Verbose("<= {player}@{lobby} {event_description}",
          client.Digest,
          client.CurrentLobbyIdentifier,
          request.DebugDescription());

      return request.Process(client, this.server);
    }
  }
}
