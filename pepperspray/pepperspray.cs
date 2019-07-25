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
      if (args.Count() < 2)
      {
        Log.Fatal("Arguments required: IP PORT");
      }

      var address = args[0];
      var port = System.Convert.ToInt32(args[1]);
      Log.Information("pepperspray v0.1");
      var coreServer = new CoreServer.CoreServer();
      var externalServer = new ExternalServer.ExternalServer();

      var coreTask = new CIO.Listener()
        .Bind(address, port)
        .Incoming()
        .Map(connection => coreServer.ConnectPlayer(connection))
        .Map(player => player.Stream.Stream()
          .Map(ev => coreServer.ProcessCommand(player, ev))
          .Catch(ex => { coreServer.PlayerLoggedOff(player); player.Stream.Terminate(); }));

      var externalTask = externalServer.Listen(address, port + 1);

      PromiseHelpers.All(coreTask, externalTask).Join();
      return 0;
    }
  }
}
