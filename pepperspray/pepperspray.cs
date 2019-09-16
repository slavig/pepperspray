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
using System.Globalization;

namespace pepperspray
{
  public class pepperspray
  {
    public pepperspray()
    {
    }

    public static int Main(String[] args)
    {
#if DEBUG
        Utils.Logging.ConfigureLogger(LogEventLevel.Verbose);
#else
        Utils.Logging.ConfigureLogger(LogEventLevel.Information);
        Utils.Logging.ConfigureExceptionHandler();
#endif

      var config = new Configuration(Path.Combine("peppersprayData", "configuration.xml"));
      DI.Register(config);

#if DEBUG
      var localhostAddress = IPAddress.Parse("127.0.0.1");
      config.ChatServerAddress = localhostAddress;
      config.RestAPIServerAddress = localhostAddress;
      config.LoginServerAddress = localhostAddress;
      config.CrossOriginAddress = localhostAddress;
      config.PlayerInactivityTimeout = 10 * 60;
#endif

      if (config.OverrideLocale != null)
      {
        Log.Information("Locale is overriden by the config and set to {local}", config.OverrideLocale);
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.CreateSpecificCulture(config.OverrideLocale);
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.CreateSpecificCulture(config.OverrideLocale);
      }

      Log.Information("pepperspray v1.3.4");
      var coreServer = DI.Get<ChatServerListener>();
      var externalServer = DI.Get<RestAPIServerListener>();
      var loginServer = DI.Get<LoginServerListener>();
      Promise<Nothing>.Race(coreServer.Listen(), externalServer.Listen(), loginServer.Listen()).Join();

      return 0;
    }
  }
}
