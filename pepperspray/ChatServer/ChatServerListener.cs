using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
  internal class ChatServerListener
  {
    private Configuration config = DI.Get<Configuration>();
    private ChatManager coreServer = DI.Auto<ChatServer.ChatManager>();

    internal IPromise<Nothing> Listen()
    {
      return new CIO.CIOListener("CoreServer")
        .Bind(this.config.ChatServerAddress, this.config.ChatServerPort)
        .Incoming()
        .Map(connection => this.coreServer.ConnectPlayer(connection))
        .Map(player => player.Stream.Stream()
          .Map(ev => this.coreServer.ProcessCommand(player, ev))
          .Catch(ex => { this.coreServer.PlayerLoggedOff(player); player.Stream.Terminate(); }))
          .Then(a => Nothing.Resolved());
    }
  }
}
