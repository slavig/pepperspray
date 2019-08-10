using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pepperspray.ChatServer.Protocol
{
  internal class Message
  {
    internal enum MessageType
    {
      Event,
      Ping
    }

    internal MessageType Type;
    internal string name;
    internal object data;

    internal static Message Ping = new Message(MessageType.Ping);

    internal Message(MessageType type)
    {
      this.Type = type;
    }

    internal Message(string name, object data)
    {
      this.Type = MessageType.Event;
      this.name = name;
      this.data = data;
    }

    internal string DebugDescription() {
      if (this.Type == MessageType.Ping)
      {
        return "PING";
      }

      string description = null;
      if (this.data is List<object>)
      {
        description = (this.data as List<object>).Aggregate("", (i, a) => i + " " + a);
      } else if (this.data is List<string>)
      {
        description = (this.data as List<string>).Aggregate("", (i, a) => i + " " + a);
      } else if (this.data is Dictionary<string, string>)
      {
        description = (this.data as Dictionary<string, string>).Aggregate("", (i, a) => i + " " + a.Key + "=" + a.Value);
      } else if (this.data is Dictionary<string, object>)
      {
        description = (this.data as Dictionary<string, object>).Aggregate("", (i, a) => i + " " + a.Key + "=" + a.Value);
      } else if (this.data is Dictionary<String, object>)
      {
        description = (this.data as Dictionary<String, object>).Aggregate("", (i, a) => i + " " + a.Key + "=" + a.Value);
      } else if (this.data is Dictionary<String, String>)
      {
        description = (this.data as Dictionary<String, String>).Aggregate("", (i, a) => i + " " + a.Key + "=" + a.Value);
      } else if (this.data != null)
      {
        description = this.data.ToString();
      }

      return String.Format("{0} ({1})", this.name, description);
    }
  }
}
