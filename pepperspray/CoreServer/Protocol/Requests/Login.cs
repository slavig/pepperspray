using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RSG;
using Serilog;
using pepperspray.CIO;
using pepperspray.CoreServer.Game;
using pepperspray.Utils;
using pepperspray.SharedServices;

namespace pepperspray.CoreServer.Protocol.Requests
{
  internal class Login : ARequest
  {
    private Configuration config = DI.Get<Configuration>();
    private NameValidator nameValidator = DI.Auto<NameValidator>();

    private string name, hash, sex;

    internal static Login Parse(Message ev)
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
      if (!this.nameValidator.Validate(this.name))
      {
        Log.Information("Terminating connection of {name}/{id} from {hash}/{address} - name is not valid",
          this.name,
          this.hash,
          sender.Stream.ConnectionHash,
          sender.Stream.ConnectionEndpoint);

        sender.Stream.Write(Responses.FriendAlert("ERROR: Invalid characters in name. Please use only letters and numbers.")).Then(a => sender.Stream.Terminate());
        return false;
      }

      var existingPlayer = server.World.FindPlayer(this.name);
      if (existingPlayer == null)
      {
        return true;
      }
      else if (server.CheckPlayerTimeout(existingPlayer))
      {
        return true;
      }
      else
      {
        Log.Information("Terminating connection of {name}/{id} from {hash}/{address} - invalid login due to player already online",
          this.name,
          this.hash,
          sender.Stream.ConnectionHash,
          sender.Stream.ConnectionEndpoint);

        var message = String.Format("ERROR: Player already logged in. Please change name or wait {0} seconds if you think this is an error.", this.config.PlayerInactivityTimeout);

        sender.Stream.Write(Responses.FriendAlert(message))
          .Then(a => sender.Stream.Terminate());
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
