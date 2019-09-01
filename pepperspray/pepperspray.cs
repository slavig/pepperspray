using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using RSG;
using Serilog;
using Serilog.Events;
using pepperspray.CIO;
using pepperspray.Utils;
using pepperspray.ChatServer;
using pepperspray.RestAPIServer;
using pepperspray.LoginServer;
using pepperspray.SharedServices;

namespace pepperspray
{
  public class pepperspray
  {
    public pepperspray()
    {
    }

    public static int Main(String[] args)
    {
      var debugMode = System.Diagnostics.Debugger.IsAttached;
      if (!debugMode)
      {
        Utils.Logging.ConfigureLogger(LogEventLevel.Information);
        Utils.Logging.ConfigureExceptionHandler();
      }
      else
      {
        Utils.Logging.ConfigureLogger(LogEventLevel.Verbose);
      }

      var config = new Configuration(Path.Combine("peppersprayData", "configuration.xml"));
      DI.Register(config);

      if (debugMode)
      {
        var localhostAddress = IPAddress.Parse("127.0.0.1");
        config.ChatServerAddress = localhostAddress;
        config.RestAPIServerAddress = localhostAddress;
        config.LoginServerAddress = localhostAddress;
        config.CrossOriginAddress = localhostAddress;
        config.PlayerInactivityTimeout = 10 * 60;
      }

      Log.Information("pepperspray v1.2.1");
      var coreServer = DI.Get<ChatServerListener>();
      var externalServer = DI.Get<RestAPIServerListener>();
      var loginServer = DI.Get<LoginServerListener>();
      Promise<Nothing>.Race(coreServer.Listen(), externalServer.Listen(), loginServer.Listen()).Join();

      return 0;
    }
  }
}
