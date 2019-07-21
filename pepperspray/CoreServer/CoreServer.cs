using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;

using RSG;
using pepperspray.CIO;
using pepperspray.CoreServer.Protocol;
using pepperspray.CoreServer.Game;
using pepperspray.Utils;
using ThreeDXChat.Networking.NodeNet;

namespace pepperspray.CoreServer
{

  internal class CoreServer
  {
    internal World World;
    private EventDispatcher dispatcher;

    internal CoreServer()
    {
      this.World = new World();
      this.dispatcher = new EventDispatcher(this);
    }

    internal PlayerHandle ConnectPlayer(CIOSocket socket)
    {
      var handle = new PlayerHandle(new ClientEventStream(socket));
      handle.Send(Responses.Connected());
      Console.WriteLine("IN connection {0}", handle.GetHashCode());
      return handle;
    }

    internal Nothing ProcessCommand(PlayerHandle handle, NodeServerEvent msg)
    {
      Console.WriteLine("<= {0} {1}", msg.name, msg.DebugDescription());

      var promise = this.dispatcher.Dispatch(handle, msg);
      if (promise != null)
      {
        return new Nothing();
      } else
      {
        return new Nothing();
      }
    }

    internal void PlayerLoggedIn(PlayerHandle player)
    {
      this.World.AddPlayer(player);
    }

    internal void PlayerLoggedOff(PlayerHandle player)
    {
      Console.WriteLine("OUT connection {0} ({1})", player.GetHashCode(), player.Name);

      player.CurrentLobby.RemovePlayer(player);
      this.World.RemovePlayer(player);
    }
  }
}
