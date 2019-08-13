using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Xml;

namespace pepperspray.SharedServices
{
  internal class Configuration
  {
    internal class MailConfiguration
    {
      internal string ServerAddress;
      internal int ServerPort;
      internal string Address;
      internal string Username;
      internal string Password;
    }

    internal IPAddress ChatServerAddress;
    internal int ChatServerPort;

    internal IPAddress RestAPIServerAddress;
    internal int RestAPIServerPort;

    internal IPAddress LoginServerAddress;
    internal int LoginServerPort;

    internal IPAddress CrossOriginAddress;
    internal int CrossOriginPort;

    internal Dictionary<string, string> Radiostations = new Dictionary<string, string>();
    internal string TokenSalt;
    internal uint WorldCacheCapacity;
    internal int PlayerInactivityTimeout;

    internal MailConfiguration Mail;

    private string path;

    internal Configuration(string path)
    { 
      this.path = path;
      this.LoadConfiguration();
    }

    internal void LoadConfiguration()
    {
      var doc = new XmlDocument();
      doc.Load(this.path);

      {
        var chatAddressNode = doc.SelectSingleNode("configuration/chat-server/addr");
        this.ChatServerAddress = this.parseAddress(chatAddressNode.Attributes["ip"].InnerText);
        this.ChatServerPort = Convert.ToInt32(chatAddressNode.Attributes["port"].InnerText);

        var playerInactivityTimeoutNode = doc.SelectSingleNode("configuration/chat-server/player-inactivity-timeout");
        this.PlayerInactivityTimeout = Convert.ToInt32(playerInactivityTimeoutNode.Attributes["seconds"].InnerText);
      }

      {
        var restApiAddressNode = doc.SelectSingleNode("configuration/rest-api-server/addr");
        this.RestAPIServerAddress = this.parseAddress(restApiAddressNode.Attributes["ip"].InnerText);
        this.RestAPIServerPort = Convert.ToInt32(restApiAddressNode.Attributes["port"].InnerText);

        var crossOriginNode = doc.SelectSingleNode("configuration/rest-api-server/cross-origin");
        this.CrossOriginAddress = this.parseAddress(crossOriginNode.Attributes["allow-ip"].InnerText);

        if (crossOriginNode.Attributes["port"] != null)
        {
          this.CrossOriginPort = Convert.ToInt32(crossOriginNode.Attributes["port"].InnerText);
        }
        else
        {
          this.CrossOriginPort = 0;
        }

        var worldCacheNode = doc.SelectSingleNode("configuration/rest-api-server/world-cache");
        this.WorldCacheCapacity = Convert.ToUInt32(worldCacheNode.Attributes["capacity"].InnerText);

        var radiostationsNodes = doc.SelectSingleNode("configuration/rest-api-server/radiostations");
        foreach (var nodeElement in radiostationsNodes.ChildNodes)
        {
          var node = nodeElement as XmlNode;
          var lobbyId = node.Attributes["id"].InnerText;
          var url = node.Attributes["url"].InnerText;

          this.Radiostations[lobbyId] = url;
        }

        {
          var mailNode = doc.SelectSingleNode("configuration/rest-api-server/mail");

          var smptServerNode = mailNode.SelectSingleNode("smtp-server");
          var credentialsNode = mailNode.SelectSingleNode("credentials");
          var senderNode = mailNode.SelectSingleNode("sender");

          this.Mail = new Configuration.MailConfiguration
          {
            ServerAddress = smptServerNode.Attributes["addr"].InnerText,
            ServerPort = Convert.ToInt32(smptServerNode.Attributes["port"].InnerText),
            Username = credentialsNode.Attributes["username"].InnerText,
            Password = credentialsNode.Attributes["password"].InnerText,
            Address = senderNode.Attributes["addr"].InnerText,
          };
        }
      }

      {
        var loginAddressNode = doc.SelectSingleNode("configuration/login-server/addr");
        this.LoginServerAddress = this.parseAddress(loginAddressNode.Attributes["ip"].InnerText);
        this.LoginServerPort = Convert.ToInt32(loginAddressNode.Attributes["port"].InnerText);
      }

      var tokenSaltNode = doc.SelectSingleNode("configuration/token");
      this.TokenSalt = tokenSaltNode.Attributes["salt"].InnerText;
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
