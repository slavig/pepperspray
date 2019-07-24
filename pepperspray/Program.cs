using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using RSG;
using pepperspray.CIO;
using pepperspray.CoreServer;
using pepperspray.ExternalServer;

namespace pepperspray
{
  public class Program
  {
    public Program()
    {
    }

    public static int Main(String[] args)
    {
      var server = new CoreServer.CoreServer();
      var listener = new CIO.Server("127.0.0.1", 2517);
      var coreTask = listener
        .Incoming()
        .Map(connection => { lock (server) { return server.ConnectPlayer(connection); } })
        .Map(player => player.EventStream()
          .Map(ev => server.ProcessCommand(player, ev))
          .Catch(ex => { lock (server) { server.PlayerLoggedOff(player); } } )
      );

      var extServer = new ExternalServer.ExternalServer();
      extServer.Listen();

      Console.Read();
      return 0;
    }
  }
}
