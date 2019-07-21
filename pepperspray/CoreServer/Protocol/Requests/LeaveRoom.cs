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
  internal class LeaveRoom: ARequest
  {
    internal static LeaveRoom Parse(NodeServerEvent ev)
    {
      return new LeaveRoom();
    }

    internal override IPromise<Nothing> Process(PlayerHandle sender, CoreServer server)
    {
      {
        var lobby = sender.CurrentLobby;
        PlayerHandle[] lobbyPlayers = new PlayerHandle[] { };
        if (lobby != null) 
        {
          lock (server)
          {
            lobby.RemovePlayer(sender);
            sender.CurrentLobby = null;
            lobbyPlayers = lobby.Players().ToArray();

            if (lobby.Players().Count() == 0)
            {
              server.World.RemoveLobby(lobby);
            }
          }
        }

        return sender.Send(Responses.JoinedLobby())
          .Then(a => new CombinedPromise<Nothing>(lobbyPlayers.Select(b => b.Send(Responses.PlayerLeave(sender)))))
        as IPromise<Nothing>;
      }
    }
  }
}
