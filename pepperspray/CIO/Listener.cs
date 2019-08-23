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
  class CIOListener
  {
    private Socket listener;
    private Configuration config = DI.Get<Configuration>();

    private string name;

    public CIOListener(string name)
    {
      this.name = name;
    }

    public CIOListener Bind(IPAddress addr, int port)
    {
      Log.Information("Binding {server} to {addr}:{port}", name, addr, port);
      this.listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
      this.listener.Bind(new IPEndPoint(addr, port));
      this.listener.Listen(100);

      return this;
    }

    public IMultiPromise<CIOSocket> Incoming()
    {
      var promise = new MultiPromise<CIOSocket>();
      CIOReactor.Spawn(this.name + "_incomingConnections", true, () =>
      {
        Log.Information("{server} listening for connections", this.name);
        var sync = new ManualResetEvent(false);
        while (true)
        {
          try
          {
            sync.Reset();

            this.listener.BeginAccept(new AsyncCallback(result =>
            {
              try
              {
                var socket = this.listener.EndAccept(result);
                var wrappedSocket = new CIOSocket(this.name, socket);

                Log.Debug("{server} accepted connection {hash} from {ip}", this.name, wrappedSocket.GetHashCode(), wrappedSocket.Endpoint);
                promise.SingleResolve(wrappedSocket);
              }
              catch (Exception e)
              {
                Log.Error("{server} end accept caught: {exception}", this.name, e);
              }

              sync.Set();
            }), null);

            sync.WaitOne();
          }
          catch (Exception e)
          {
            Log.Error("{server} begin accept caught exception: {exception}", this.name, e);
          }
        }
      });

      return promise;
    }
  }
}
