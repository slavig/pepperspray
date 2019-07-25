using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using RSG;
using Serilog;
using Serilog.Events;
using pepperspray.CIO;
using pepperspray.CoreServer;
using pepperspray.ExternalServer;

namespace pepperspray
{
  public class pepperspray
  {
    public pepperspray()
    {
    }

    public static int Main(String[] args)
    {
      Utils.Logging.ConfigureLogger(LogEventLevel.Debug);

      var address = args[0];
      var port = System.Convert.ToInt32(args[1]);
      Log.Information("pepperspray v0.1");

      var server = new CoreServer.CoreServer();
      var task = new CIO.Listener()
        .Bind(address, port)
        .Incoming()
        .Map(connection => server.ConnectPlayer(connection))
        .Map(player => player.Stream.Stream()
          .Map(ev => server.ProcessCommand(player, ev))
          .Catch(ex => { server.PlayerLoggedOff(player); player.Stream.Terminate(); }));

      Console.Read();
      return 0;
    }
  }
}
