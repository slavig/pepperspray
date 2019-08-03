using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Xml;

namespace pepperspray.Utils
{
  internal class Configuration
  {
    internal IPAddress CoreServerAddress;
    internal int CoreServerPort;

    internal IPAddress MiscServerAddress;
    internal int MiscServerPort;

    internal Dictionary<string, string> AdminTokens = new Dictionary<string, string>();

    internal uint WorldCacheCapacity;

    internal Configuration(string path)
    {
      var doc = new XmlDocument();
      doc.Load(path);

      var coreAddressNode = doc.SelectSingleNode("configuration/address/core");
      this.CoreServerAddress = this.parseAddress(coreAddressNode.Attributes["ip"].InnerText);
      this.CoreServerPort = Convert.ToInt32(coreAddressNode.Attributes["port"].InnerText);

      var miscAddressNode = doc.SelectSingleNode("configuration/address/misc");
      this.MiscServerAddress = this.parseAddress(miscAddressNode.Attributes["ip"].InnerText);
      this.MiscServerPort = Convert.ToInt32(miscAddressNode.Attributes["port"].InnerText);

      var worldCacheNode = doc.SelectSingleNode("configuration/world-cache");
      this.WorldCacheCapacity = Convert.ToUInt32(worldCacheNode.Attributes["capacity"].InnerText);

      var adminTokensNodes = doc.SelectNodes("configuration/admin-tokens/token");
      foreach (XmlNode tokenNode in adminTokensNodes)
      {
        var name = tokenNode.Attributes["id"].InnerText;
        var hash = tokenNode.Attributes["hash"].InnerText;

        this.AdminTokens[hash] = name;
      }

    }

    private IPAddress parseAddress(string value)
    {
      if (value.Equals("auto"))
      {
        IPHostEntry ipHostInfo = Dns.Resolve(Dns.GetHostName());
        return ipHostInfo.AddressList[0];
      } else
      {
        return IPAddress.Parse(value);
      }
    }
  }
}
