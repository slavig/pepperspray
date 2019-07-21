using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

using RSG;
using pepperspray.CIO;
using ThreeDXChat.Networking;
using ThreeDXChat.Networking.NodeNet;
using ThreeDXChat.Networking.BinarySerialization;
using ThreeDXChat.Networking.Tcp;

namespace pepperspray.CoreServer.Protocol
{
  internal class ClientEventStream
  {
    private CIOSocket socket;
    private byte[] slidingBuffer;
    private static int SlidingBufferLimit = 32000;

    internal ClientEventStream(CIOSocket socket)
    {
      this.socket = socket;
      this.slidingBuffer = new byte[0];
    }

    internal IPromise<Nothing> Terminate()
    {
      this.socket.Shutdown();
      return Nothing.Resolved();
    }

    internal IPromise<Nothing> Write(NodeServerEvent outEvent)
    {
      return this.socket.Write(Parser.SerializeEvent(outEvent));
    }

    internal IMultiPromise<NodeServerEvent> EventStream()
    {
      var promise = new MultiPromise<NodeServerEvent>();
      this.socket.InputStream().SingleThen(bytes =>
      {
        var originalCount = this.slidingBuffer.Count();
        Array.Resize(ref this.slidingBuffer, this.slidingBuffer.Count() + bytes.Count());
        Array.Copy(bytes, 0, this.slidingBuffer, originalCount, bytes.Count());

        while (this.slidingBuffer.Count() > 0)
        {
          int seekTo = 0;
          int newCount = this.slidingBuffer.Count();
          var ev = Parser.ParseEvent(this.slidingBuffer, seekTo, out seekTo);

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

          if (newCount > ClientEventStream.SlidingBufferLimit)
          {
            throw new Exception("Sliding buffer limit exceeded.");
          }

          if (seekTo == 0)
          {
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
