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
  internal class OpenRoom: ARequest
  {
    private string lobbyIdentifier;
    private string name;
    private uint numberOfPlayers;
    private UserRoom.AccessType accessType;

    internal static OpenRoom Parse(Message ev)
    {
      if (ev.data is List<string> == false)
      {
        return null;
      }

      var arguments = ev.data as List<string>;
      if (arguments.Count() < 4)
      {
        return null;
      }

      var lobbyIdentifier = arguments[0].ToString().Trim();
      var name = arguments[3].ToString().Trim();

      if (lobbyIdentifier.Count() == 0 || name.Count() == 0)
      {
        return null;
      }

      var numberOfPlayers = System.Convert.ToUInt32(arguments[2].ToString());
      return new OpenRoom
      {
        lobbyIdentifier = lobbyIdentifier,
        name = name,
        numberOfPlayers = numberOfPlayers,
        accessType = UserRoom.AccessType.ForAll
      };
    }

    internal override bool Validate(PlayerHandle sender, CoreServer server)
    {
      if (!base.Validate(sender, server))
      {
        return false;
      }

      if (!this.lobbyIdentifier.StartsWith(sender.Name))
      {
        return false;
      }

      return true;
    }

    internal override IPromise<Nothing> Process(PlayerHandle sender, CoreServer server)
    {
      var room = new UserRoom
      {
        Identifier = this.lobbyIdentifier,
        User = sender,
        Name = this.name,
        Access = this.accessType,
        NumberOfPlayers = this.numberOfPlayers
      };

      lock(server)
      {
        server.World.AddUserRoom(room);
      }

      return Nothing.Resolved();
    }
  }
}
