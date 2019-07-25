﻿using System;
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
using pepperspray.CoreServer.Protocol;
using ThreeDXChat.Networking;
using ThreeDXChat.Networking.NodeNet;
using ThreeDXChat.Networking.BinarySerialization;
using ThreeDXChat.Networking.Tcp;

namespace pepperspray.CoreServer
{
  internal class EventStream
  {
    internal int ConnectionHash { get { return this.socket.ConnectionHash; } }
    internal string ConnectionEndpoint
    {
      get
      {
        var endpoint = this.socket.Endpoint;
        return String.Format("{0}:{1}", endpoint.Address, endpoint.Port);
      }
    }

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
      Log.Debug("Terminating connection");
      this.socket.Shutdown();
      return Nothing.Resolved();
    }

    internal IPromise<Nothing> Write(NodeServerEvent outEvent)
    {
      return this.socket.Write(Parser.SerializeEvent(outEvent));
    }

    internal IMultiPromise<NodeServerEvent> Stream()
    {
      var promise = new MultiPromise<NodeServerEvent>();
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

          NodeServerEvent ev = Parser.ParseEvent(this.slidingBuffer, seekTo, out seekTo);

          if (ev != null)
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
            Log.Error("EventStream {hash} exceeded buffer limit: {total_bytes} total bytes", newCount);
            promise.Reject(new Exception("Sliding buffer limit exceeded."));
            break;
          }

          if (seekTo == 0)
          {
            Log.Warning("{hash} failed to parse event, total {total_bytes} in bufffer", this.slidingBuffer.Count());
            break;
          }
        }
      })
      .Catch(ex => {
        promise.Reject(ex);
      });

      return promise;
    }
  }
}