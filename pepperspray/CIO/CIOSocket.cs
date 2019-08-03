using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using RSG;
using Serilog;

namespace pepperspray.CIO
{
  public class CIOSocket
  {
    public IPEndPoint Endpoint
    {
      get
      {
        try
        {
          return this.socket.RemoteEndPoint as IPEndPoint;
        } catch (ObjectDisposedException)
        {
          return null;
        }
      }
    }

    public int ConnectionHash
    {
      get
      {
        return this.socket.GetHashCode();
      }
    }

    private Socket socket;
    public CIOSocket(Socket socket)
    {
      this.socket = socket;
    }

    public IMultiPromise<byte[]> InputStream()
    {
      var promise = new MultiPromise<byte[]>();
      CIOReactor.Spawn("inputStreamOf" + this.ConnectionHash, true, () =>
      {
        var sync = new ManualResetEvent(false);
        var run = true;

        Log.Debug("Starting input stream for {hash}", this.ConnectionHash);
        while (run)
        {
          sync.Reset();
          this.readPacket().Then(value =>
          {
            if (value.Count() == 0)
            {
              run = false;
              Log.Debug("Empty response from {hash}, terminating.", this.ConnectionHash);
              promise.Reject(new Exception("Closed"));
            } else { 
              promise.SingleResolve(value);
            }

            sync.Set();
          }).Catch(ex =>
          {
            Log.Debug("readPacket() from {hash} rejected.", this.ConnectionHash);
            run = false;
            promise.Reject(ex);
            sync.Set();
          });

          sync.WaitOne();
        }

        Log.Debug("Input stream for {hash} ended", this.ConnectionHash);
      }).Catch(ex => 
      {
        Log.Debug("Input stream {name} for {hash} crashed, rejecting its promise", this.ConnectionHash);
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
          try
          {
            this.socket.EndSend(res);
            promise.Resolve(new Nothing());
          }
          catch (Exception e)
          {
            promise.Reject(e);
          }
        }), null);
      } catch (Exception e) {
        promise.Reject(e);
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
      var promise = new Promise<byte[]>();
      try
      {
        var messageBuffer = new byte[this.socket.ReceiveBufferSize];
        this.socket.BeginReceive(messageBuffer, 0, messageBuffer.Count(), 0, new AsyncCallback(res =>
        {
          try
          {
            var byteCount = this.socket.EndReceive(res);
            promise.Resolve(messageBuffer.Take(byteCount).ToArray());
          }
          catch (Exception e)
          {
            Log.Error("EndReceive failed: {exception}", e);
            promise.Reject(e);
          }
        }), null);
      }
      catch (Exception e)
      {
        Log.Error("BeginReceive failed: {exception}", e);
        promise.Reject(e);
      }

      return promise;
    }
  }
}
