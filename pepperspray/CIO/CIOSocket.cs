using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using RSG;

namespace pepperspray.CIO
{
  public class CIOSocket
  {
    private Socket socket;
    public CIOSocket(Socket socket)
    {
      this.socket = socket;
    }

    public IMultiPromise<byte[]> InputStream()
    {
      var promise = new MultiPromise<byte[]>();

      CIOReactor.Spawn("inputStreamOf" + this.socket.GetHashCode(), () =>
      {
        var sync = new ManualResetEvent(false);
        var run = true;
        while (run)
        {
          sync.Reset();
          this.readPacket().Then(value =>
          {
            if (value.Count() == 0)
            {
              run = false;
              promise.Reject(new Exception("Closed"));
              this.Shutdown();
            } else {
              promise.SingleResolve(value);
            }
            sync.Set();
          }, ex =>
          {
            promise.Reject(ex);
            run = false;
            sync.Set();
          });

          sync.WaitOne();
        }
      });

      return promise;
    }

    public IPromise<Nothing> Write(byte[] bytes)
    {
      var promise = new Promise<Nothing>();
      try
      {
        this.socket.BeginSend(bytes, 0, bytes.Length, 0, new AsyncCallback(res =>
        {
          this.socket.EndSend(res);
          promise.Resolve(new Nothing());
        }), null);
      } catch (Exception e) {
        this.Shutdown();
      }

      return promise;
    }

    public void Shutdown()
    {
      this.socket.Shutdown(SocketShutdown.Both);
      this.socket.Close();
    }

    private IPromise<byte[]> readPacket()
    {
      var promise = new MultiPromise<byte[]>();
      var messageBuffer = new byte[8092];
      try
      {
        this.socket.BeginReceive(messageBuffer, 0, messageBuffer.Count(), 0, new AsyncCallback(res =>
        {
          try
          {
            var byteCount = this.socket.EndReceive(res);
            promise.Resolve(messageBuffer.Take(byteCount).ToArray());
          } catch (Exception e)
          {
            promise.Reject(e);
          }
        }), null);
      }
      catch (Exception e)
      {
        promise.Reject(e);
      }

      return promise;
    }
  }
}
