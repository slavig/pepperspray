using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RSG;
using pepperspray.CIO;
using pepperspray.CoreServer.Game;
using pepperspray.CoreServer.Protocol;

namespace pepperspray.CoreServer.Shell
{
  internal class Players: AShellCommand
  {
    private static Dictionary<string, string> locationsMapping = new Dictionary<string, string>
    {
      { "SALOON", "Saloon" },
      { "beach", "Beach" },
      { "island", "Island" },
      { "CLUB_NEW", "Fresco" },
      { "YACHT", "Yacht" },
      { "club", "Nightclub" },
      { "BDSM_club", "Sin Club" },
    };
    
    internal override bool WouldDispatch(string tag)
    {
      return tag.Equals("players");
    }

    internal override IPromise<Nothing> Dispatch(ShellDispatcher dispatcher, PlayerHandle sender, CoreServer server, string tag, IEnumerable<string> arguments)
    {
      var builder = new StringBuilder("Online players: ");

      lock (server)
      {
        foreach (var item in server.World.Lobbies)
        {
          string name;
          if (Players.locationsMapping.TryGetValue(item.Key, out name))
          {
            builder.AppendFormat(" {0} ({1}),", name, item.Value.Players.Count());
          }
        }
      }

      var response = builder.ToString();
      return dispatcher.Output(sender, server, response.Substring(0, response.Length - 1));
    }
  }
}
