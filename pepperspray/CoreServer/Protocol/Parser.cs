using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

using pepperspray.CIO;
using ThreeDXChat.Networking;
using ThreeDXChat.Networking.NodeNet;
using ThreeDXChat.Networking.BinarySerialization;
using ThreeDXChat.Networking.Tcp;


namespace pepperspray.CoreServer.Protocol
{
  internal class Parser
  {
    internal static byte[] SerializeEvent(NodeServerEvent e)
    {
      var networkMessage = Parser.networkMessageFrom(e);
      var tcpMessage = Parser.tcpMessageFrom(networkMessage);

      byte[] result = new byte[tcpMessage.SizeToSend];
      Array.Copy(tcpMessage.data, result, result.Count());
      return result;
    }

    internal static NodeServerEvent ParseEvent(byte[] bytes, int seekPos, out int seekTo)
    {
      var tcpMessage = Parser.tcpMessageFrom(bytes, seekPos, out seekTo);
      if (tcpMessage == null)
      {
        return null;
      }

      var networkMessage = Parser.networkMessageFrom(tcpMessage);
      var reader = new BinaryReader(new MemoryStream(networkMessage.data, 0, networkMessage.Size));
      try
      {
        var parsedObject = reader.ReadObject();
        if (parsedObject is NodeServerEvent)
        {
          return parsedObject as NodeServerEvent;
        }
      }
      catch (EndOfStreamException) { }
      catch (ArgumentOutOfRangeException) { }
      catch (IndexOutOfRangeException) { }

      return null;
    }

    private static NetworkMessage networkMessageFrom(NetMessage tcpMessage)
    {
      var msg = NetworkMessage.Pop(tcpMessage.Size);
      msg.data = tcpMessage.ReadBytes(tcpMessage.Size);
      msg.Size = msg.data.Count();
      return msg;
    }

    private static NetMessage tcpMessageFrom(byte[] bytes, int seekPos, out int seekTo)
    {
      var msg = new NetMessage(bytes.Count());
      msg.data = bytes;
      if (msg.SizeToSend <= bytes.Count())
      {
        seekTo = msg.SizeToSend;
        return msg;
      } else
      {
        seekTo = seekPos;
        return null;
      }
    }

    private static NetworkMessage networkMessageFrom(NodeServerEvent e)
    {
      var memoryStream = new MemoryStream();
      var writer = new BinaryWriter(memoryStream);
      writer.WriteObject(e);

      byte[] bytes = new byte[memoryStream.Length];
      Array.Copy(memoryStream.GetBuffer(), bytes, memoryStream.Length);
      memoryStream.Close();

      var msg = NetworkMessage.Pop(32);
      msg.Write(bytes);
      return msg;
    }

    private static NetMessage tcpMessageFrom(NetworkMessage message)
    {
      var netMessage = new NetMessage();
      NetworkMessageType messageType = message.MessageType;
      if (messageType == NetworkMessageType.Data)
      {
        netMessage.MessageType = NetMessageType.Data;
      }
      else if (messageType == NetworkMessageType.ConnectionApproval) { }
      else
      {
        netMessage.MessageType = NetMessageType.Auth;
      }

      netMessage.Write(message.data, 0, message.Size);
      message.Recycle();
      netMessage.WritePositionToSize(true);

      return netMessage;
    }
  }
}
