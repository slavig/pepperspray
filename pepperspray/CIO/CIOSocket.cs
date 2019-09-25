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
    public class EmptyPacketException : Exception { }

    public bool IsAlive => this.socket.Connected;
    public int ConnectionHash => this.socket.GetHashCode();

    public IPEndPoint Endpoint
    {
      get
      {
        try
        {
          return this.socket.RemoteEndPoint as IPEndPoint;
        }
        catch (SocketException) { }
        catch (ObjectDisposedException) { }

        return null;
      }
    }

    private string name;
    private Socket socket;

    public CIOSocket(string name, Socket socket)
    {
      this.name = name;
      this.socket = socket;
    }

    public IMultiPromise<byte[]> InputStream()
    {
      var watch = new Stopwatch(); ;
      watch.Start();

      var promise = new MultiPromise<byte[]>();
      CIOReactor.Spawn(this.name + "_inputStreamOf" + this.ConnectionHash, true, () =>
      {
        var sync = new ManualResetEvent(false);
        var run = true;

        Log.Debug("{server} starting input stream for {hash}", this.name, this.ConnectionHash);
        while (run)
        {
          sync.Reset();
          this.readPacket().Then(value =>
          {
            if (value.Count() == 0)
            {
              run = false;
              Log.Debug("{server} received empty response from {hash}, terminating.", this.name, this.ConnectionHash);
              promise.Reject(new EmptyPacketException());
            } else { 
              promise.SingleResolve(value);
            }

            sync.Set();
          }).Catch(ex =>
          {
            Log.Debug("{server} readPacket() from {hash} rejected.", this.name, this.ConnectionHash);
            run = false;
            promise.Reject(ex);
            sync.Set();
          });

          sync.WaitOne();
        }

        Log.Debug("{server} input stream for {hash} ended", this.name, this.ConnectionHash);
      }).Catch(ex => 
      {
        Log.Error("{server} input stream for {hash} crashed, rejecting its promise", this.name, this.ConnectionHash);
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
            Log.Debug("{server} EndReceive failed: {exception}", this.name, e);
            promise.Reject(e);
          }
        }), null);
      }
      catch (Exception e)
      {
        Log.Debug("{server} BeginReceive failed: {exception}", this.name, e);
        promise.Reject(e);
      }

      return promise;
    }
  }
}
