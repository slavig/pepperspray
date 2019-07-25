using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RSG;
using Serilog;
using pepperspray.CIO;
using pepperspray.CoreServer.Game;
using ThreeDXChat.Networking.NodeNet;

namespace pepperspray.CoreServer.Protocol.Requests
{
  internal abstract class Send: ARequest
  {
    protected string contents;

    internal abstract IEnumerable<PlayerHandle> Recepients(PlayerHandle sender, CoreServer server);

    internal override IPromise<Nothing> Process(PlayerHandle sender, CoreServer server)
    {
      var commands = this.Recepients(sender, server)
        .Select(r => r.Stream.Write(Responses.Message(sender, this.contents)));

      return new CombinedPromise<Nothing>(commands);
    }
  }

  internal class SendPM: Send
  {
    private string recepientName;
    private PlayerHandle recepient;

    internal static SendPM Parse(NodeServerEvent ev)
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

      return new SendPM
      {
        recepientName = arguments[0].ToString(),
        contents = arguments[1].ToString()
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

      return true;
    }

    internal override IEnumerable<PlayerHandle> Recepients(PlayerHandle sender, CoreServer server)
    {
      return new PlayerHandle[] { this.recepient };
    }
  }

  internal class SendLocal: Send
  {
    internal static SendLocal Parse(NodeServerEvent ev)
    {
      return new SendLocal
      {
        contents = ev.data.ToString()
      };
    }

    internal override IEnumerable<PlayerHandle> Recepients(PlayerHandle sender, CoreServer server)
    {
      return sender.CurrentLobby.Players.Except(new PlayerHandle[] { sender });
    }
  }

  internal class SendWorld: Send
  {
    internal static SendWorld Parse(NodeServerEvent ev)
    {
      return new SendWorld
      {
        contents = ev.data.ToString()
      };
    }

    internal override IEnumerable<PlayerHandle> Recepients(PlayerHandle sender, CoreServer server)
    {
      List<PlayerHandle> recepients = null;
      lock(server)
      {
        recepients = server.World.Players.Except(new PlayerHandle[] { sender }).ToList();
      }

      return recepients;
    }
  }
}
