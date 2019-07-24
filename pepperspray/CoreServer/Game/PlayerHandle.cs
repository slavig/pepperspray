using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RSG;
using pepperspray.CIO;
using pepperspray.CoreServer.Protocol;
using pepperspray.Utils;
using ThreeDXChat.Networking.NodeNet;

namespace pepperspray.CoreServer.Game
{
  internal class PlayerHandle
  {
    internal bool IsLoggedIn;
    internal string Name;
    internal string Hash;
    internal string Sex;
    internal string Id;
    internal Lobby CurrentLobby;

    private ClientEventStream stream;

    internal PlayerHandle(ClientEventStream writer)
    {
      this.stream = writer;

      this.IsLoggedIn = false;
    }

    internal IPromise<Nothing> Send(NodeServerEvent e)
    {
      Console.WriteLine("=> {0}: {1}({2})", this.Name, e.name, e.DebugDescription());
      return this.stream.Write(e);
    }
    
    internal IPromise<Nothing> Disconnect()
    {
      return this.stream.Terminate();
    }

    internal IMultiPromise<NodeServerEvent> EventStream()
    {
      return this.stream.EventStream();
    }
  }
}
