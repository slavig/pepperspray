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
          .Map(ev => this.coreServer.ProcessMessage(player, ev))
          .Then(_ =>
          {
            Log.Information("Input stream of {player} finished", player.Digest);
          })
          .Catch(ex =>
          {
            if (player.Stream.IsConnectionAlive)
            {
              Log.Warning("Input stream of {player} rejected and is being terminated due to unhandled exception: {exception}", player.Digest, ex);
            }
            else
            {
              Log.Debug("Input stream of {player} ended on rejected state ({ex}), already terminated.", player.Digest, ex.Message);
            }
          })
          .Then(() =>
          {
            if (player.Stream.IsConnectionAlive)
            {
              Log.Debug("Terminating connection of {player}", player.Digest);

              this.coreServer.PlayerLoggedOff(player);
              player.Stream.Terminate();
            }
          })
        ).Then(a => Nothing.Resolved());
    }
  }
}
