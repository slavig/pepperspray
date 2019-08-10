using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Security.Cryptography;
using System.Xml;

namespace peppersprayPlugin
{
  public class Configuration
  {
    public class Address
    {
      public string Ip;
      public Int32 Port;
    }

    public Address ChatAddress;
    public string RestAPIAddress;
    public string LoginAddress;

    public static Configuration Load()
    {
      var confidDoc = Utils.OpenXml(Configuration.configPath());
      var coreAddressNode = confidDoc.SelectSingleNode("servers/chat");
      var extAddressNode = confidDoc.SelectSingleNode("servers/rest-api");
      var loginAddressNode = confidDoc.SelectSingleNode("servers/login");

      var coreAddressComponents = coreAddressNode.InnerText.Split(new char[] { ':' });
      var coreAddress = new Address
      {
        Ip = coreAddressComponents[0],
        Port = System.Convert.ToInt32(coreAddressComponents[1])
      };

      return new Configuration
      {
        ChatAddress = coreAddress,
        RestAPIAddress = extAddressNode.InnerText,
        LoginAddress = loginAddressNode.InnerText
      };
    }

    private static string configPath()
    {
      return ".\\peppersprayPlugin\\config.xml";
    }
  }
}
