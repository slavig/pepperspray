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
  internal class RestAPIServerListener: IDIService
  {
    internal static string worldDirectoryPath = Path.Combine("peppersprayData", "worlds");

    private LoginService loginService;
    private Configuration config;
    private Server server;

    public void Inject()
    {
      this.config = DI.Get<Configuration>();
      this.loginService = DI.Get<LoginService>();
    }

    internal IPromise<Nothing> Listen()
    {
      var ip = this.config.RestAPIServerAddress.ToString();
      var port = this.config.RestAPIServerPort;
      Log.Information("Binding external server to {ip}:{port}", ip, port);

      this.server = new Server(ip, port, false, DefaultRoute);

      this.server.StaticRoutes.Add(HttpMethod.GET, "/getmoney", this.GetMoney);
      this.server.StaticRoutes.Add(HttpMethod.POST, "/news.php", this.News);
      this.server.StaticRoutes.Add(HttpMethod.POST, "/radio.php", this.Radio);

      new Controllers.WorldController(this.server, new Storage.WorldStorage(this.config.Worlds.RamCacheCapacity));
      new Controllers.CharacterController(this.server);
      new Controllers.FriendsController(this.server);
      new Controllers.LoginController(this.server);
      new Controllers.PhotoController(this.server);
      new Controllers.GiftController(this.server);
      new Controllers.OfflineMessageController(this.server);

      return new Promise<Nothing>();
    }

    internal HttpResponse DefaultRoute(HttpRequest req)
    {
      return new HttpResponse(req, 404, null);
    }

    internal HttpResponse News(HttpRequest req)
    {
      var builder = new StringBuilder();
      foreach (var announcement in this.config.Announcements)
      {
        builder.AppendFormat(
          "{0}|{1}|{2}|{3}|",
          announcement.Title,
          announcement.Text.Replace("\n", "").Replace("<br />", "<br>").Replace("<br/>", "<br>"),
          announcement.ImageURL,
          announcement.LinkURL
          );
      }

      return req.TextResponse(builder.ToString());
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
      try
      {
        var user = this.loginService.AuthorizeUser(req.GetBearerToken());
        return req.TextResponse((user.Currency + this.config.Currency.Padding).ToString());
      }
      catch (Exception e)
      {
        Log.Warning("Client {endpoint} failed to get money: {exception}", req.GetEndpoint(), e);
        if (e is LoginService.InvalidTokenException)
        {
          return req.FailureResponse();
        } 
        else
        {
          throw e;
        }
      }
    }

    internal HttpResponse OfflineMsgCheck(HttpRequest req)
    {
      return new HttpResponse(req, 200, null, "text/plain", "[]");
    }
  }
}
