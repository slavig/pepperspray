using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RSG;
using Serilog;
using pepperspray.CIO;
using pepperspray.CoreServer.Game;
using ThreeDXChat.Networking.NodeNet;

namespace pepperspray.CoreServer.Protocol.Requests
{
  internal class JoinRoom: ARequest
  {
    private string lobbyIdentifier;
    private Lobby lobby;

    internal static JoinRoom Parse(Message ev)
    {
      return new JoinRoom
      {
        lobbyIdentifier = ev.data.ToString()
      };
    }

    internal override bool Validate(PlayerHandle sender, CoreServer server)
    {
      if (!base.Validate(sender, server))
      {
        return false;
      }

      this.lobby = server.World.FindOrCreateLobby(this.lobbyIdentifier);

      return true;
    }

    internal override IPromise<Nothing> Process(PlayerHandle sender, CoreServer server)
    {
      PlayerHandle[] otherPlayers = null;
      lock (server)
      {
        otherPlayers = this.lobby.Players.ToArray();
        this.lobby.AddPlayer(sender);
        sender.CurrentLobby = this.lobby;
      }

      var notifyExistingAboutNew = otherPlayers.Select(a => a.Stream.Write(Responses.NewPlayer(sender)));
      var notifyNewAboutExisting = otherPlayers.Select(a => sender.Stream.Write(Responses.NewPlayer(a)));

      Log.Information("Player {name} joined lobby {id}, total {total} players.", sender.Name, this.lobby.Identifier, otherPlayers.Count());

      return sender.Stream.Write(Responses.JoinedRoom(this.lobby))
        .Then(a => new CombinedPromise<Nothing>(notifyExistingAboutNew))
        .Then(a => new CombinedPromise<Nothing>(notifyNewAboutExisting))
      as IPromise<Nothing>;
    }
  }
}
