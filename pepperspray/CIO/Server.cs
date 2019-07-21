using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

using RSG;
using pepperspray.CIO;

namespace pepperspray.CIO
{
  class Server
  {
    private Socket listener;

    public Server(String ip, int port)
    {
      var addr = IPAddress.Parse(ip);
      this.listener = new Socket(addr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
      this.listener.Bind(new IPEndPoint(addr, 8124));
      this.listener.Listen(100);
    }

    public IMultiPromise<CIOSocket> Incoming()
    {
      var promise = new MultiPromise<CIOSocket>();
      CIOReactor.Spawn("incomingConnections", () =>
      {
        try
        {
          var sync = new ManualResetEvent(false);
          while (true)
          {
            sync.Reset();

            listener.BeginAccept(new AsyncCallback(result =>
            {
              var handler = (Socket)result.AsyncState;
              var socket = handler.EndAccept(result);
              promise.SingleResolve(new CIOSocket(socket));
              sync.Set();
            }), listener);

            sync.WaitOne();
          }
        }
        catch (Exception e)
        {
          promise.Reject(e);
        }
      });

      return promise;
    }
  }
}
