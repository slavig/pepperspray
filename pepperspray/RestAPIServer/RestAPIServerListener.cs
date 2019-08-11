using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

using Serilog;
using RSG;
using WatsonWebserver;
using HttpMultipartParser;
using pepperspray.CIO;
using pepperspray.RestAPIServer.Services;
using pepperspray.SharedServices;
using pepperspray.Utils;

namespace pepperspray.RestAPIServer
{
  internal class RestAPIServerListener
  {
    private Configuration config = DI.Get<Configuration>();

    internal static string newsPath = Path.Combine("peppersprayData", "news.txt");
    internal static string worldDirectoryPath = Path.Combine("peppersprayData", "worlds");

    internal static string staticsUrl = "/peppersprayData/static/";

    private Server server;

    internal IPromise<Nothing> Listen()
    {
      var ip = this.config.RestAPIServerAddress.ToString();
      var port = this.config.RestAPIServerPort;
      Log.Information("Binding external server to {ip}:{port}", ip, port);

      this.server = new Server(ip, port, false, DefaultRoute);
      this.server.ContentRoutes.Add(RestAPIServerListener.staticsUrl, true);
      this.server.StaticRoutes.Add(HttpMethod.GET, "/getmoney", this.GetMoney);
      this.server.StaticRoutes.Add(HttpMethod.POST, "/news.php", this.News);
      this.server.StaticRoutes.Add(HttpMethod.POST, "/radio.php", this.Radio);
      this.server.StaticRoutes.Add(HttpMethod.POST, "/offlineMsgCheck", this.OfflineMsgCheck);

      this.server.StaticRoutes.Add(HttpMethod.OPTIONS, "/login", this.CrossDomainAccessConfig);
      this.server.StaticRoutes.Add(HttpMethod.OPTIONS, "/signup", this.CrossDomainAccessConfig);
      this.server.StaticRoutes.Add(HttpMethod.OPTIONS, "/delete", this.CrossDomainAccessConfig);

      new Controllers.WorldController(this.server, new Storage.WorldStorage(this.config.WorldCacheCapacity));
      new Controllers.CharacterController(this.server);
      new Controllers.FriendsController(this.server);
      new Controllers.LoginController(this.server);

      return new Promise<Nothing>();
    }

    internal HttpResponse DefaultRoute(HttpRequest req)
    {
      return new HttpResponse(req, 404, null);
    }

    internal HttpResponse News(HttpRequest req)
    {
      return req.TextResponse(File.ReadAllText(RestAPIServerListener.newsPath));
    }

    internal HttpResponse Radio(HttpRequest req)
    {
      try
      {
        string url = "no";
        var action = req.GetFormParameter("action");
        switch (action)
        {
          case "geturl":
            var id = req.GetFormParameter("level");
            this.config.Radiostations.TryGetValue(id, out url);

            return req.TextResponse(url);

          default:
            return req.FailureResponse();
        }
      }
      catch (ArgumentException)
      {
        return req.FailureResponse();
      }
    }

    internal HttpResponse GetMoney(HttpRequest req)
    {
      return new HttpResponse(req, 200, null, "text/plain", "25170");
    }

    internal HttpResponse OfflineMsgCheck(HttpRequest req)
    {
      return new HttpResponse(req, 200, null, "text/plain", "[]");
    }

    internal HttpResponse CrossDomainAccessConfig(HttpRequest req)
    {
      return new HttpResponse(req, 204, new Dictionary<string, string>
      {
        { "Access-Control-Allow-Methods", "POST" },
        { "Access-Control-Allow-Origin", String.Format("http://{0}:{1}", this.config.CrossOriginAddress, this.config.CrossOriginPort) },
        { "Access-Control-Allow-Headers", "*" },
      });
    }
  }
}
