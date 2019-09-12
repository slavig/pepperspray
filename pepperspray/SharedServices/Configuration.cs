using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Xml;

namespace pepperspray.SharedServices
{
  internal class Configuration: IDIService
  {
    internal class MailConfiguration
    {
      internal string ServerAddress;
      internal int ServerPort;
      internal string Address;
      internal string Username;
      internal string Password;
    }

    public class Announcement
    {
      public string Title;
      public string Text;
      public string ImageURL, LinkURL;
    }

    public class RedirectionConfiguration
    {
      public bool Enabled;
      public string Host;
    }

    public class WorldsConfiguration
    {
      public uint RamCacheCapacity;
    }

    public class CurrencyConfiguration
    {
      public bool Enabled;
      public uint Padding;
      public uint BonusPerHourOnline;
    }

    public class RecaptchaConfiguration
    {
      public bool Enabled;
      public string VisibleSecret;
      public string InvisibleSecret;
    }

    public class PermanentRoom
    {
      public string Name;
      public string Identifier;
      public string Owner;
      public string[] Moderators;
    }

    public class DanglingRoomConfiguration
    {
      public bool Enabled;
      public TimeSpan Timeout;
    }

    public class ExpelConfiguration
    {
      public bool Enabled;
      public TimeSpan MaxDuration;
    }

    internal IPAddress ChatServerAddress;
    internal int ChatServerPort;

    internal IPAddress RestAPIServerAddress;
    internal int RestAPIServerPort;

    internal IPAddress LoginServerAddress;
    internal int LoginServerPort;

    internal IPAddress CrossOriginAddress;
    internal int CrossOriginPort;

    internal ExpelConfiguration Expel;
    internal DanglingRoomConfiguration DanglingRoom;
    internal Dictionary<string, string> Radiostations;
    internal MailConfiguration Mail;
    internal List<Announcement> Announcements;
    internal RedirectionConfiguration StaticsRedirection;
    internal WorldsConfiguration Worlds;
    internal CurrencyConfiguration Currency;
    internal RecaptchaConfiguration Recaptcha;
    internal List<string> BannedAddresses;
    internal List<PermanentRoom> PermanentRooms;

    internal string TokenSalt;
    internal int PlayerInactivityTimeout;
    internal uint PlayerPhotoSlots;
    internal uint PhotoSizeLimit;
    internal uint LoginAttemptThrottle;

    internal string WebfrontProtocolVersion = "web";
    internal uint MinimumProtocolVersion = 5;

    private string path;

    public void Inject()
    {
    }

    public Configuration()
    {
      Debug.Assert(false, "Unsupported constructor.");
    }

    public Configuration(string path)
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
        var permanentRoomsNode = doc.SelectSingleNode("configuration/chat-server/permanent-rooms");
        this.PermanentRooms = new List<PermanentRoom>();

        if (permanentRoomsNode != null)
        {
          foreach (var nodeElement in permanentRoomsNode.ChildNodes)
          {
            var node = nodeElement as XmlNode;
            var identifier = node.Attributes["identifier"].InnerText;
            var name = node.Attributes["name"].InnerText;
            var ownerName = node.Attributes["owner"].InnerText;
            var modNames = node.Attributes["mods"].InnerText;

            this.PermanentRooms.Add(new PermanentRoom
            {
              Identifier = identifier,
              Name = name,
              Owner = ownerName,
              Moderators = modNames.Split(',').Select((a) => a.Trim()).ToArray(),
            });
          }
        } 
      }

      {
        var expelNode = doc.SelectSingleNode("configuration/chat-server/expel");
        var maxDurationMinutes = Convert.ToUInt32(expelNode.Attributes["maximum-duration"].InnerText);
        this.Expel = new ExpelConfiguration
        {
          Enabled = expelNode.Attributes["enabled"].InnerText.Equals("true"),
          MaxDuration = TimeSpan.FromMinutes(maxDurationMinutes),
        };
      }

      {
        var danglingLocationNode = doc.SelectSingleNode("configuration/chat-server/dangling-user-rooms");
        var timeoutMinutes = Convert.ToUInt32(danglingLocationNode.Attributes["timeout"].InnerText);
        this.DanglingRoom = new DanglingRoomConfiguration
        {
          Enabled = danglingLocationNode.Attributes["enabled"].InnerText.Equals("true"),
          Timeout = TimeSpan.FromMinutes(timeoutMinutes),
        };
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

        var loginAttemptThrottleNode = doc.SelectSingleNode("configuration/rest-api-server/login-attempt-throttle");
        this.LoginAttemptThrottle = Convert.ToUInt32(loginAttemptThrottleNode.Attributes["seconds"].InnerText);

        {
          var currencyNode = doc.SelectSingleNode("configuration/rest-api-server/currency");
          this.Currency = new CurrencyConfiguration
          {
            Enabled = currencyNode.Attributes["enabled"].InnerText.Equals("true"),
            Padding = Convert.ToUInt32(currencyNode.Attributes["padding"].InnerText),
            BonusPerHourOnline = Convert.ToUInt32(currencyNode.Attributes["bonusPerHourOnline"].InnerText)
          };
        }

        {
          var worldCacheNode = doc.SelectSingleNode("configuration/rest-api-server/worlds");
          this.Worlds = new WorldsConfiguration
          {
            RamCacheCapacity = Convert.ToUInt32(worldCacheNode.Attributes["ram-cache-capacity"].InnerText),
          };
        }

        {
          var photosNode = doc.SelectSingleNode("configuration/rest-api-server/photos");
          this.PlayerPhotoSlots = Convert.ToUInt32(photosNode.Attributes["slot-count"].InnerText);
          this.PhotoSizeLimit = Convert.ToUInt32(photosNode.Attributes["size-limit"].InnerText);
        }

        var radiostationsNodes = doc.SelectSingleNode("configuration/rest-api-server/radiostations");
        if (radiostationsNodes != null)
        {
          this.Radiostations = new Dictionary<string, string>();
          foreach (var nodeElement in radiostationsNodes.ChildNodes)
          {
            var node = nodeElement as XmlNode;
            var lobbyId = node.Attributes["id"].InnerText;
            var url = node.Attributes["url"].InnerText;

            this.Radiostations[lobbyId] = url;
          }
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

        {
          var newsNode = doc.SelectSingleNode("configuration/rest-api-server/news");
          if (newsNode != null)
          {
            this.Announcements = new List<Announcement>();

            foreach (var nodeElement in newsNode.ChildNodes)
            {
              var node = nodeElement as XmlNode;
              this.Announcements.Add(new Announcement
              {
                Title = node.Attributes["title"].InnerText,
                Text = node.InnerXml.Trim(),
                ImageURL = node.Attributes["image"] != null ? node.Attributes["image"].InnerText : "",
                LinkURL = node.Attributes["url"] != null ? node.Attributes["url"].InnerText : ""
              });
            }
          }
        }

        {
          var redirectionNode = doc.SelectSingleNode("configuration/rest-api-server/statics-redirection");
          var ip = this.parseAddress(redirectionNode.Attributes["ip"].InnerText);
          var port = Convert.ToUInt32(redirectionNode.Attributes["port"].InnerText);

          this.StaticsRedirection = new RedirectionConfiguration
          {
            Enabled = redirectionNode.Attributes["enabled"].InnerText.Equals("true"),
            Host = String.Format("http://{0}:{1}", ip, port)
          };
        }

        {
          var recaptchaNode = doc.SelectSingleNode("configuration/rest-api-server/recaptcha");
          this.Recaptcha = new RecaptchaConfiguration
          {
            Enabled = recaptchaNode.Attributes["enabled"].InnerText.Equals("true"),
            VisibleSecret = recaptchaNode.Attributes["visible-secret"].InnerText,
            InvisibleSecret = recaptchaNode.Attributes["invisible-secret"].InnerText
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

      var bannedAddresses = doc.SelectSingleNode("configuration/banned-addresses");
      if (bannedAddresses != null)
      {
        this.BannedAddresses = new List<string>();

        foreach (var nodeElement in bannedAddresses.ChildNodes)
        {
          var node = nodeElement as XmlNode;
          this.BannedAddresses.Add(node.Attributes["ip"].InnerText);
        }
      }
    }

    private IPAddress parseAddress(string value)
    {
      if (value.Equals("auto"))
      {
        IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
        return ipHostInfo.AddressList[0];
      } else
      {
        return IPAddress.Parse(value);
      }
    }
  }
}
