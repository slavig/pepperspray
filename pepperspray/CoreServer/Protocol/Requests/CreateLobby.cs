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
  internal class CreateLobby: ARequest
  {
    private string lobbyIdentifier;
    private string recepientName;
    private PlayerHandle recepient;

    internal static CreateLobby Parse(NodeServerEvent ev)
    {
      if (ev.data is List<string> == false)
      {
        return null;
      }
      var arguments = ev.data as List<string>;
      if (arguments.Count() < 2)
      {
        return null;
      }

      var recepient = arguments[0].ToString();
      var message = arguments[1].ToString();

      if (!message.StartsWith("~ask/runchat"))
      {
        return null;
      }

      return new CreateLobby
      {
        lobbyIdentifier = recepient + "_room",
        recepientName = recepient
      };
    }

    internal override bool Validate(PlayerHandle sender, CoreServer server)
    {
      if (!base.Validate(sender, server))
      {
        return false;
      }

      this.recepient = server.World.FindPlayer(this.recepientName);
      if (this.recepient == null)
      {
        return false;
      }

      // return server.World.FindLobby(this.lobbyIdentifier) == null;
      return true;
    }

    internal override IPromise<Nothing> Process(PlayerHandle sender, CoreServer server)
    {
      server.World.CreateLobby(this.lobbyIdentifier);
      return Nothing.Resolved();
    }
  }
}
