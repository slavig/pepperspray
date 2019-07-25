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

namespace pepperspray.CIO
{
  class Listener
  {
    private Socket listener;

    public Listener()
    {
    }

    public Listener Bind(String ip, int port)
    {
      var addr = IPAddress.Parse(ip);

      IPHostEntry ipHostInfo = Dns.Resolve(Dns.GetHostName());
      IPAddress ipAddress = ipHostInfo.AddressList[0];
      IPEndPoint localEndPoint = new IPEndPoint(ipAddress, port);

      Log.Information("Binding to {addr}:{port} (DNS tells us {dns_ip})", ip, port, ipAddress);
      this.listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
      this.listener.Bind(new IPEndPoint(addr, port));
      this.listener.Listen(100);

      return this;
    }

    public IMultiPromise<CIOSocket> Incoming()
    {
      var promise = new MultiPromise<CIOSocket>();
      CIOReactor.Spawn("incomingConnections", () =>
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
          Log.Error("Listener caught exception: {exception}", e);
          promise.Reject(e);
        }
      });

      return promise;
    }
  }
}
