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
      Log.Information("Setting {url} as cross-origin allowed", this.crossOriginAllowedAddress());

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
        { "Access-Control-Allow-Origin", this.crossOriginAllowedAddress() },
        { "Access-Control-Allow-Headers", "Accept, Accept-CH, Accept-Charset, Accept-Datetime, Accept-Encoding, Accept-Ext, Accept-Features, Accept-Language, Accept-Params, Accept-Ranges, Access-Control-Allow-Credentials, Access-Control-Allow-Headers, Access-Control-Allow-Methods, Access-Control-Allow-Origin, Access-Control-Expose-Headers, Access-Control-Max-Age, Access-Control-Request-Headers, Access-Control-Request-Method, Age, Allow, Alternates, Authentication-Info, Authorization, C-Ext, C-Man, C-Opt, C-PEP, C-PEP-Info, CONNECT, Cache-Control, Compliance, Connection, Content-Base, Content-Disposition, Content-Encoding, Content-ID, Content-Language, Content-Length, Content-Location, Content-MD5, Content-Range, Content-Script-Type, Content-Security-Policy, Content-Style-Type, Content-Transfer-Encoding, Content-Type, Content-Version, Cookie, Cost, DAV, DELETE, DNT, DPR, Date, Default-Style, Delta-Base, Depth, Derived-From, Destination, Differential-ID, Digest, ETag, Expect, Expires, Ext, From, GET, GetProfile, HEAD, HTTP-date, Host, IM, If, If-Match, If-Modified-Since, If-None-Match, If-Range, If-Unmodified-Since, Keep-Alive, Label, Last-Event-ID, Last-Modified, Link, Location, Lock-Token, MIME-Version, Man, Max-Forwards, Media-Range, Message-ID, Meter, Negotiate, Non-Compliance, OPTION, OPTIONS, OWS, Opt, Optional, Ordering-Type, Origin, Overwrite, P3P, PEP, PICS-Label, POST, PUT, Pep-Info, Permanent, Position, Pragma, ProfileObject, Protocol, Protocol-Query, Protocol-Request, Proxy-Authenticate, Proxy-Authentication-Info, Proxy-Authorization, Proxy-Features, Proxy-Instruction, Public, RWS, Range, Referer, Refresh, Resolution-Hint, Resolver-Location, Retry-After, Safe, Sec-Websocket-Extensions, Sec-Websocket-Key, Sec-Websocket-Origin, Sec-Websocket-Protocol, Sec-Websocket-Version, Security-Scheme, Server, Set-Cookie, Set-Cookie2, SetProfile, SoapAction, Status, Status-URI, Strict-Transport-Security, SubOK, Subst, Surrogate-Capability, Surrogate-Control, TCN, TE, TRACE, Timeout, Title, Trailer, Transfer-Encoding, UA-Color, UA-Media, UA-Pixels, UA-Resolution, UA-Windowpixels, URI, Upgrade, User-Agent, Variant-Vary, Vary, Version, Via, Viewport-Width, WWW-Authenticate, Want-Digest, Warning, Width, X-Content-Duration, X-Content-Security-Policy, X-Content-Type-Options, X-CustomHeader, X-DNSPrefetch-Control, X-Forwarded-For, X-Forwarded-Port, X-Forwarded-Proto, X-Frame-Options, X-Modified, X-OTHER, X-PING, X-PINGOTHER, X-Powered-By, X-Requested-With" },
      });
    }

    private string crossOriginAllowedAddress()
    {
      return "http://" + this.config.CrossOriginAddress + (this.config.CrossOriginPort != 0 ? ":" + this.config.CrossOriginPort.ToString() : "");
    }
  }
}
