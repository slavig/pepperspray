﻿using System;
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
  internal class JoinRoom: ARequest
  {
    private string lobbyIdentifier;
    private Lobby lobby;

    internal static JoinRoom Parse(NodeServerEvent ev)
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
      var otherPlayers = new List<PlayerHandle>(this.lobby.Players());

      lock(server) {
        this.lobby.AddPlayer(sender);
        sender.CurrentLobby = this.lobby;
      }

      var existing = otherPlayers.Select(a => a.Send(Responses.NewPlayer(sender)));
      var existing2 = otherPlayers.Select(a => sender.Send(Responses.NewPlayer(a)));

      return sender.Send(Responses.JoinedRoom(this.lobby))
        .Then(a => new CombinedPromise<Nothing>(existing))
        .Then(a => new CombinedPromise<Nothing>(existing2))
      as IPromise<Nothing>;
    }
  }
}
