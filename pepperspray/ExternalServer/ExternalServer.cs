using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

using HttpMultipartParser;
using Serilog;
using RSG;
using WatsonWebserver;
using pepperspray.CIO;
using pepperspray.Utils;
using pepperspray.SharedServices;

namespace pepperspray.ExternalServer
{
  internal class ExternalServer
  {
    private Configuration config = DI.Get<Configuration>();

    internal static string newsPath = Path.Combine("peppersprayData", "news.txt");
    internal static string worldDirectoryPath = Path.Combine("peppersprayData", "worlds");
    internal static string characterPresetsDirectoryPath = Path.Combine("peppersprayData", "presets");

    internal static string staticsUrl = "/peppersprayData/static/";

    private Server server;

    internal IPromise<Nothing> Listen()
    {
      var ip = this.config.MiscServerAddress.ToString();
      var port = this.config.MiscServerPort;
      Log.Information("Binding external server to {ip}:{port}", ip, port);

      this.server = new Server(ip, port, false, DefaultRoute);
      this.server.ContentRoutes.Add(ExternalServer.staticsUrl, true);
      this.server.StaticRoutes.Add(HttpMethod.GET, "/getmoney", this.GetMoney);
      this.server.StaticRoutes.Add(HttpMethod.POST, "/news.php", this.News);
      this.server.StaticRoutes.Add(HttpMethod.POST, "/offlineMsgCheck", this.OfflineMsgCheck);

      new Controllers.WorldController(this.server, new Storage.WorldStorage(this.config.WorldCacheCapacity));
      new Controllers.CharacterController(this.server);

      return new Promise<Nothing>();
    }

    internal HttpResponse DefaultRoute(HttpRequest req)
    {
      return new HttpResponse(req, 404, null);
    }

    internal HttpResponse News(HttpRequest req)
    {
      return new HttpResponse(req, 200, null, "text/plain", File.ReadAllText(ExternalServer.newsPath));
    }

    internal HttpResponse GetMoney(HttpRequest req)
    {
      return new HttpResponse(req, 200, null, "text/plain", "25170");
    }

    internal HttpResponse OfflineMsgCheck(HttpRequest req)
    {
      return new HttpResponse(req, 200, null, "text/plain", "[]");
    }

    internal static HttpResponse FailureResponse(HttpRequest req)
    {
      return new HttpResponse(req, 403, null);
    }

    internal static string GetFormParameter(HttpRequest req, string key)
    {
      var str = Encoding.UTF8.GetString(req.Data);
      foreach (string keypair in str.Split('&'))
      {
        string[] kv = keypair.Split('=');
        if (kv.Count() < 2)
        {
          continue;
        }

        if (kv[0].Equals(key))
        {
          return kv[1];
        }
      }

      return null;
    }
  }
}
