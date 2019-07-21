using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

using Unosquare.Labs.EmbedIO;
using Unosquare.Labs.EmbedIO.Constants;
using Unosquare.Labs.EmbedIO.Modules;
using Unosquare.Labs.EmbedIO.Core;
using HttpMultipartParser;
using System.Security.Cryptography;

namespace pepperspray.ExtServer
{
  class ExtServer
  {
    public void Listen()
    {
      var url = "http://127.0.0.1:8125/";
      using (var server = new WebServer(url))
      {
        server.RegisterModule(new WebApiModule());

        server.RegisterModule(new StaticFilesModule(ExtServer.staticsPath));
        server.Module<StaticFilesModule>().UseRamCache = true;

        server.Module<WebApiModule>().RegisterController<WorldController>();
        server.Module<WebApiModule>().RegisterController<InfoController>();
        
        server.WithLocalSession();
        server.RunAsync();
        Console.Read();
      }
    }

    public static string newsPath = ".\\peppersprayData\\news.txt";
    public static string worldDirectoryPath = ".\\peppersprayData\\worlds\\";
    public static string staticsPath = ".\\peppersprayData\\static\\";
  }

  class WorldController: WebApiController
  {
    public WorldController(IHttpContext context) : base(context)
    {

    }

    [WebApiHandler(HttpVerbs.Post, "/uploadworld2.php")]
    public async Task<bool> UploadWorld()
    {
      var parser = new MultipartFormDataParser(HttpContext.Request.InputStream);
      var uid = parser.HasParameter("uid") ? parser.GetParameterValue("uid") : null;
      if (uid == null)
      {
        return false;
      }

      if (parser.Files.Count() < 1)
      {
        return false;
      }

      var file = parser.Files[0];
      Console.WriteLine(uid);
      Console.WriteLine(file);
      return true;
    }

    [WebApiHandler(HttpVerbs.Post, "/getworld2.php")]
    public async Task<bool> DownloadWorld()
    {
      var form = await HttpContext.RequestFormDataDictionaryAsync();
      string uid = null;

      if (HttpContext.InQueryString("uid"))
      {
        uid = HttpContext.QueryString("uid");
      }
      else if (form.ContainsKey("uid"))
      {
        uid = form["uid"].ToString();
      }
      else
      {
        return false;
      }

      await HttpContext.FileResponseAsync(new FileInfo(this.worldPath(uid)));
      return true;
    }

    private string worldPath(string uid)
    {
      var identifier = this.md5Hash(uid);
      return ExtServer.worldDirectoryPath + "\\" + identifier + ".world";
    }

    private string md5Hash(string input)
    {
      MD5 md5 = System.Security.Cryptography.MD5.Create();
      byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
      byte[] hash = md5.ComputeHash(inputBytes);
      StringBuilder sb = new StringBuilder();
      for (int i = 0; i < hash.Length; i++)
      {
        sb.Append(hash[i].ToString("X2"));
      }

      return sb.ToString();
    }
  }

  class InfoController: WebApiController
  {
    public InfoController(IHttpContext context) : base(context)
    {
    }

    [WebApiHandler(HttpVerbs.Post, "/news.php")]
    public async Task<bool> News()
    {
      var str = File.ReadAllText(ExtServer.newsPath);
      await HttpContext.Response.StringResponseAsync(str);
      return true;
    }
  }
}
