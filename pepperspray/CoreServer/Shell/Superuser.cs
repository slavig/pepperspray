using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Serilog;
using RSG;
using pepperspray.CIO;
using pepperspray.CoreServer.Game;
using pepperspray.CoreServer.Protocol;
using pepperspray.Utils;
using pepperspray.SharedServices;

namespace pepperspray.CoreServer.Shell
{
  internal class Superuser: AShellCommand
  {
    private Configuration config = DI.Get<Configuration>();

    internal override bool WouldDispatch(string tag)
    {
      return tag.Equals("su") || tag.Equals("password");
    }

    internal override IPromise<Nothing> Dispatch(ShellDispatcher dispatcher, PlayerHandle sender, CoreServer server, string tag, IEnumerable<string> arguments)
    {
      switch (tag)
      {
        case "su":
          return dispatcher.Output(sender, server, "Please enter password with /password PASSWORD");

        case "password":
          if (arguments.Count() != 1)
          {
            return dispatcher.Error(sender, server, "Invalid arguments");
          }

          var passwordHash = Hashing.Md5(arguments.First());
          string tokenId = null;
          if (this.config.AdminTokens.TryGetValue(passwordHash, out tokenId))
          {
            sender.IsAdmin = true;
            Log.Information("Player {name}/{hash}/{ip} authenticated as admin (token id {id})",
              sender.Name,
              sender.Stream.ConnectionHash,
              sender.Stream.ConnectionEndpoint,
              tokenId);
            return dispatcher.Output(sender, server, "Authenticated");
          }
          else
          {
            Log.Information("Player {name}/{hash}/{ip} failed to authenticate as admin",
              sender.Name,
              sender.Stream.ConnectionHash,
              sender.Stream.ConnectionEndpoint);

            return dispatcher.Error(sender, server, "Failed to authenticate. Incident will be reported.");
          }

        default:
          return Nothing.Resolved();
      }
    }
  }
}
