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
using pepperspray.Utils;
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
      if (!System.Diagnostics.Debugger.IsAttached)
      {
        Utils.Logging.ConfigureLogger(LogEventLevel.Information);
        Utils.Logging.ConfigureExceptionHandler();
      }
      else
      {
        Utils.Logging.ConfigureLogger(LogEventLevel.Verbose);
      }

      DI.Setup();
      var config = DI.Get<Configuration>();
      if (System.Diagnostics.Debugger.IsAttached)
      {
        var localhostAddress = IPAddress.Parse("127.0.0.1");
        config.CoreServerAddress = localhostAddress;
        config.MiscServerAddress = localhostAddress;
      }

      Log.Information("pepperspray v0.4");
      var coreServer = new CoreServer.CoreServer();
      var externalServer = new ExternalServer.ExternalServer();

      var coreTask = new CIO.Listener()
        .Bind()
        .Incoming()
        .Map(connection => coreServer.ConnectPlayer(connection))
        .Map(player => player.Stream.Stream()
          .Map(ev => coreServer.ProcessCommand(player, ev))
          .Catch(ex => { coreServer.PlayerLoggedOff(player); player.Stream.Terminate(); }));

      var externalTask = externalServer.Listen();

      PromiseHelpers.All(coreTask, externalTask).Join();
      return 0;
    }
  }
}
