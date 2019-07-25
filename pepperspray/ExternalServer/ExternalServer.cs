using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

using Unosquare.Labs.EmbedIO;
using Unosquare.Labs.EmbedIO.Modules;
using Unosquare.Swan;

namespace pepperspray.ExternalServer
{
  class ExternalServer
  {
    public void Listen(string url)
    {
      Terminal.Settings.GlobalLoggingMessageType = LogMessageType.Debug | LogMessageType.Error | LogMessageType.Info | LogMessageType.Trace | LogMessageType.Warning;
      Terminal.OnLogMessageReceived += delegate (object sender, LogMessageReceivedEventArgs args)
      {
        Console.WriteLine(args.Message);
      };

      using (var server = new WebServer(url))
      {
        server.RegisterModule(new WebApiModule());

        server.RegisterModule(new StaticFilesModule(ExternalServer.staticsPath));
        server.Module<StaticFilesModule>().UseRamCache = true;

        server.Module<WebApiModule>().RegisterController<WorldController>();
        server.Module<WebApiModule>().RegisterController<MiscController>();
        server.Module<WebApiModule>().RegisterController<CharactersController>();
        
        server.WithLocalSession();
        server.RunAsync();
        Console.Read();
      }
    }

    public static string newsPath = ".\\peppersprayData\\news.txt";
    public static string worldDirectoryPath = ".\\peppersprayData\\worlds\\";
    public static string characterPresetsDirectoryPath = ".\\peppersprayData\\presets\\";
    public static string staticsPath = ".\\peppersprayData\\static\\";
  }
}
