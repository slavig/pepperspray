using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RSG;
using pepperspray.CIO;
using pepperspray.CoreServer.Game;
using ThreeDXChat.Networking.NodeNet;

namespace pepperspray.CoreServer.Protocol.Requests
{
  internal class Login : ARequest
  {
    private string name, hash, sex;

    internal static Login Parse(NodeServerEvent ev)
    {
      if (ev.data is Dictionary<string, object> == false)
      {
        return null;
      }

      var arguments = ev.data as Dictionary<string, object>;
      string name = arguments["name"].ToString();
      string hash = arguments["id"].ToString();
      string sex = arguments["sex"].ToString();

      if (name != null && hash != null && sex != null)
      {
        return new Login
        {
          name = name,
          hash = hash,
          sex = sex
        };
      }
      else
      {
        return null;
      }
    }

    internal override bool Validate(PlayerHandle sender, CoreServer server)
    {
      if (server.World.FindPlayer(this.name) == null)
      {
        return true;
      } else
      {
        sender.Disconnect();
        return false;
      }
    }

    internal override IPromise<Nothing> Process(PlayerHandle sender, CoreServer server)
    {
      sender.Name = this.name;
      sender.Hash = this.hash;
      sender.Id = this.hash;
      sender.Sex = this.sex;
      sender.IsLoggedIn = true;

      lock (server)
      {
        server.PlayerLoggedIn(sender);
      }
      return Nothing.Resolved();
    }
  }
}
