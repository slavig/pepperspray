using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;

using RSG;
using Serilog;
using pepperspray.CIO;
using pepperspray.CoreServer.Protocol;
using pepperspray.CoreServer.Game;
using pepperspray.Utils;
using ThreeDXChat.Networking.NodeNet;

namespace pepperspray.CoreServer
{

  internal class CoreServer
  {
    internal World World;
    private EventDispatcher dispatcher;

    internal CoreServer()
    {
      this.World = new World();
      this.dispatcher = new EventDispatcher(this);
    }

    internal void Sink<T>(IPromise<T> promise)
    {

    }

    internal PlayerHandle ConnectPlayer(CIOSocket socket)
    {
      var handle = new PlayerHandle(new EventStream(socket));
      Log.Information("Connecting player {hash}/{endpoint}", handle.Stream.ConnectionHash, handle.Stream.ConnectionEndpoint);
      this.Sink(handle.Stream.Write(Responses.Connected()));

      return handle;
    }

    internal Nothing ProcessCommand(PlayerHandle handle, NodeServerEvent msg)
    {
      Log.Debug("<= {player} {event_name}: \"{event}\"", handle.Name, msg.name, msg.DebugDescription());

      var promise = this.dispatcher.Dispatch(handle, msg);
      if (promise != null)
      {
        this.Sink(promise);
      }

      return new Nothing();
    }

    internal void PlayerLoggedIn(PlayerHandle player)
    {
      Log.Information("Player {name} logged in, connection {hash}/{endpoint}", player.Name, player.Stream.ConnectionHash, player.Stream.ConnectionEndpoint);

      lock (this)
      {
        this.World.AddPlayer(player);
      }
    }

    internal void PlayerLoggedOff(PlayerHandle player)
    {
      Log.Information("Player {name} logged off (connection {hash}/{endpoint})", player.Name, player.Stream.ConnectionHash, player.Stream.ConnectionEndpoint);

      PlayerHandle[] playersToNotify = new PlayerHandle[] { };
      lock (this)
      {
        if (player.CurrentLobby != null)
        {
          player.CurrentLobby.RemovePlayer(player);
          playersToNotify = player.CurrentLobby.Players.ToArray();
        }

        this.World.RemovePlayer(player);
      }

      Log.Debug("Notifying {number_of_players} that {name} logged off.", playersToNotify.Count(), player.Name);
      this.Sink(new CombinedPromise<Nothing>(playersToNotify.Select(b => b.Stream.Write(Responses.PlayerLeave(player)))));
    }
  }
}
