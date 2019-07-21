using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ThreeDXChat.Networking.NodeNet;

namespace pepperspray.Utils
{
  internal class Debug
  {
  }

  public static class Extensions
  {
    public static string DebugDescription(this NodeServerEvent msg) {
      string description = null;
      if (msg.data is List<object>)
      {
        description = (msg.data as List<object>).Aggregate("", (i, a) => i + " " + a);
      } else if (msg.data is List<string>)
      {
        description = (msg.data as List<string>).Aggregate("", (i, a) => i + " " + a);
      } else if (msg.data is Dictionary<string, string>)
      {
        description = (msg.data as Dictionary<string, string>).Aggregate("", (i, a) => i + " " + a.Key + "=" + a.Value);
      } else if (msg.data is Dictionary<string, object>)
      {
        description = (msg.data as Dictionary<string, object>).Aggregate("", (i, a) => i + " " + a.Key + "=" + a.Value);
      } else if (msg.data is Dictionary<String, object>)
      {
        description = (msg.data as Dictionary<String, object>).Aggregate("", (i, a) => i + " " + a.Key + "=" + a.Value);
      } else if (msg.data is Dictionary<String, String>)
      {
        description = (msg.data as Dictionary<String, String>).Aggregate("", (i, a) => i + " " + a.Key + "=" + a.Value);
      } else
      {
        description = msg.data.ToString();
      }

      return description;
    }
  }
}
