using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Net;

using RSG;
using Serilog;
using pepperspray.CIO;
using pepperspray.ChatServer.Protocol;
using ThreeDXChat.Networking;
using ThreeDXChat.Networking.NodeNet;
using ThreeDXChat.Networking.BinarySerialization;
using ThreeDXChat.Networking.Tcp;

namespace pepperspray.ChatServer
{
  internal class EventStream
  {
    internal int ConnectionHash { get { return this.socket.ConnectionHash; } }
    internal string ConnectionEndpoint
    {
      get
      {
        var endpoint = this.socket.Endpoint;
        if (endpoint != null)
        {
          return String.Format("{0}:{1}", endpoint.Address, endpoint.Port);
        }
        else
        {
          return "null";
        }
      }
    }
    internal DateTime LastCommunicationDate;

    private CIOSocket socket;
    private byte[] slidingBuffer;
    private static int SlidingBufferLimit = 65534;

    internal EventStream(CIOSocket socket)
    {
      this.socket = socket;
      this.slidingBuffer = new byte[0];
    }

    internal IPromise<Nothing> Terminate()
    {
      try
      {
        Log.Debug("Terminating connection");
        this.socket.Shutdown();
      }
      catch (Exception) {}

      return Nothing.Resolved();
    }

    internal IPromise<Nothing> Write(Message outEvent)
    {
      Log.Verbose("=> {sender} {message}", this.ConnectionHash, outEvent.DebugDescription());

      return this.socket.Write(Parser.SerializeMessage(outEvent));
    }

    internal IMultiPromise<Message> Stream()
    {
      var promise = new MultiPromise<Message>();
      Log.Debug("Starting event stream for {hash}", this.ConnectionHash);
      this.socket.InputStream().SingleThen(bytes =>
      {
        var originalCount = this.slidingBuffer.Count();
        Array.Resize(ref this.slidingBuffer, this.slidingBuffer.Count() + bytes.Count());
        Array.Copy(bytes, 0, this.slidingBuffer, originalCount, bytes.Count());

        while (this.slidingBuffer.Count() > 0)
        {
          int seekTo = 0;
          int newCount = this.slidingBuffer.Count();

          Message ev = Parser.ParseMessage(this.slidingBuffer, seekTo, out seekTo);
          this.LastCommunicationDate = DateTime.Now;

          if (ev != null && ev.Type == Message.MessageType.Event)
          {
            promise.SingleResolve(ev);
          }

          if (seekTo != 0)
          {
            newCount = this.slidingBuffer.Count() - seekTo;
            Array.Copy(this.slidingBuffer, seekTo, this.slidingBuffer, 0, newCount);
            Array.Resize(ref this.slidingBuffer, newCount);
          }

          if (newCount > EventStream.SlidingBufferLimit)
          {
            Log.Error("EventStream {hash} exceeded buffer limit: {total_bytes} total bytes", this.ConnectionHash, newCount);
            promise.Reject(new Exception("Sliding buffer limit exceeded."));
            break;
          }

          if (seekTo == 0)
          {
            Log.Warning("{hash} failed to parse event, total {total_bytes} in bufffer", this.ConnectionHash, this.slidingBuffer.Count());
            break;
          }
        }
      }).Catch(exception =>
      {
        promise.Reject(exception);
      });

      return promise;
    }
  }
}
