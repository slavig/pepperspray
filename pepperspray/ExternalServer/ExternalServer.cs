﻿using HttpMultipartParser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Serilog;
using RSG;
using WatsonWebserver;
using pepperspray.CIO;

namespace pepperspray.ExternalServer
{
  internal class ExternalServer
  {
    internal static string newsPath = ".\\peppersprayData\\news.txt";
    internal static string worldDirectoryPath = ".\\peppersprayData\\worlds\\";
    internal static string characterPresetsDirectoryPath = ".\\peppersprayData\\presets\\";

    internal static string staticsPath = "/peppersprayData/static/";

    private Server server;

    internal IPromise<Nothing> Listen(string ip, int port)
    {
      Log.Information("Binding external server to {ip}:{port}", ip, port);

      this.server = new Server(ip, port, false, DefaultRoute);
      this.server.ContentRoutes.Add(ExternalServer.staticsPath, true);
      this.server.StaticRoutes.Add(HttpMethod.POST, "/getmoney", this.GetMoney);
      this.server.StaticRoutes.Add(HttpMethod.POST, "/news.php", this.News);

      new Controllers.WorldController(this.server);
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
      return new HttpResponse(req, 200, null, "text/plain", "2517");
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
