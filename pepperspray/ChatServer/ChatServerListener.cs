using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading.Tasks;

using RSG;
using Serilog;
using Serilog.Events;
using pepperspray.CIO;
using pepperspray.Utils;
using pepperspray.ChatServer;
using pepperspray.RestAPIServer;
using pepperspray.SharedServices;

namespace pepperspray.ChatServer
{
  internal class ChatServerListener: IDIService
  {
    private Configuration config;
    private ChatManager coreServer;

    public void Inject()
    {
      this.config = DI.Get<Configuration>();
      this.coreServer = DI.Get<ChatManager>();
    }

    internal IPromise<Nothing> Listen()
    {
      return new CIO.CIOListener("CoreServer")
        .Bind(this.config.ChatServerAddress, this.config.ChatServerPort)
        .Incoming()
        .Map(connection => this.coreServer.ConnectPlayer(connection))
        .Map(player => player.Stream.Stream()
          .Map(ev => this.coreServer.ProcessCommand(player, ev))
          .Catch(ex =>
          {
            Log.Warning("Terminating connection of {name}/{token} due to unhandled exception: {exception}", player.Name, player.Token, ex);
            this.coreServer.PlayerLoggedOff(player);
            player.Stream.Terminate();
          }))
          .Then(a => Nothing.Resolved());
    }
  }
}
