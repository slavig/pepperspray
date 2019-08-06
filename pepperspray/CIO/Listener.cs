using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

using RSG;
using Serilog;
using pepperspray.Utils;
using pepperspray.SharedServices;

namespace pepperspray.CIO
{
  class Listener
  {
    private Socket listener;
    private Configuration config = DI.Get<Configuration>();

    public Listener()
    {
    }

    public Listener Bind()
    {
      var addr = this.config.CoreServerAddress;
      var port = this.config.CoreServerPort;

      Log.Information("Binding core server to {addr}:{port}", addr, port);
      this.listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
      this.listener.Bind(new IPEndPoint(addr, port));
      this.listener.Listen(100);

      return this;
    }

    public IMultiPromise<CIOSocket> Incoming()
    {
      var promise = new MultiPromise<CIOSocket>();
      CIOReactor.Spawn("incomingConnections", true, () =>
      {
        try
        {
          Log.Information("Listening for connections");
          var sync = new ManualResetEvent(false);
          while (true)
          {
            sync.Reset();

            Log.Verbose("Accepting connection...");
            this.listener.BeginAccept(new AsyncCallback(result =>
            {
              Log.Verbose("... ending accept connection");
              var socket = this.listener.EndAccept(result);
              var wrappedSocket = new CIOSocket(socket);

              Log.Debug("Accepted connection {hash} from {ip}", wrappedSocket.GetHashCode(), wrappedSocket.Endpoint);
              promise.SingleResolve(wrappedSocket);
              sync.Set();
            }), null);

            Log.Verbose("Waiting on sync");
            sync.WaitOne();
          }
        }
        catch (Exception e)
        {
          Log.Error("Rejecting Incoming() promise, listener caught exception: {exception}", e);
          promise.Reject(e);
        }
      });

      return promise;
    }
  }
}
