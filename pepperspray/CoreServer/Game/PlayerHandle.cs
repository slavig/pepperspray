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
using System.Net;

namespace pepperspray.CoreServer.Game
{
  internal class PlayerHandle
  {
    internal string Name;
    internal string Hash;
    internal string Sex;
    internal string Id;

    internal bool IsLoggedIn = false;
    internal bool IsAdmin = false;

    internal Group CurrentGroup;
    internal Lobby CurrentLobby;

    internal EventStream Stream;

    internal PlayerHandle(EventStream stream)
    {
      this.Stream = stream;
    }
  }
}
