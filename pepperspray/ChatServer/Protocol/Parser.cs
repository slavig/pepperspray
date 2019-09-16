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


namespace pepperspray.ChatServer.Protocol
{
  internal class Parser
  {
    internal class ParseException : Exception { }

    internal static byte[] SerializeMessage(Message e)
    {
      var networkMessage = Parser.networkMessageFrom(e);
      var tcpMessage = Parser.tcpMessageFrom(networkMessage);

      byte[] result = new byte[tcpMessage.SizeToSend];
      Array.Copy(tcpMessage.data, result, result.Count());
      return result;
    }

    internal static Message ParseMessage(byte[] bytes, int seekPos, out int seekTo)
    {
      NetMessage tcpMessage;
      try
      {
        tcpMessage = Parser.tcpMessageFrom(bytes, seekPos, out seekTo);
      }
      catch (Exception)
      {
        throw new ParseException();
      }

      if (tcpMessage == null)
      {
        return null;
      }

      if (tcpMessage.MessageType == NetMessageType.Ping)
      {
        return Message.Ping;
      }

      NetworkMessage networkMessage;
      try
      {
        networkMessage = Parser.networkMessageFrom(tcpMessage);
      }
      catch (Exception)
      {
        throw new ParseException();
      }

      var reader = new BinaryReader(new MemoryStream(networkMessage.data, 0, networkMessage.Size));
      try
      {
        var parsedObject = reader.ReadObject();
        if (parsedObject is NodeServerEvent)
        {
          var ev = parsedObject as NodeServerEvent;
          return new Message(ev.name, ev.data);
        }
      }
      catch (Exception) {
        throw new ParseException();
      }

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

    private static NetworkMessage networkMessageFrom(Message e)
    {
      var nodeEvent = new NodeServerEvent();
      nodeEvent.name = e.name;
      nodeEvent.data = e.data;

      var memoryStream = new MemoryStream();
      var writer = new BinaryWriter(memoryStream);
      writer.WriteObject(nodeEvent);

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
