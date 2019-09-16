using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

using RSG;
using Serilog;
using pepperspray.CIO;
using pepperspray.ChatServer.Game;
using pepperspray.ChatServer.Protocol;
using pepperspray.SharedServices;

namespace pepperspray.ChatServer.Services
{
  internal class RoomRadioService: IDIService
  {
    public void Inject()
    {
    }

    internal bool ShouldDispatch(string text)
    {
      return text.StartsWith("~action2/getRadioID|") || text.StartsWith("~action2/runRadio|");
    }

    internal IPromise<Nothing> Dispatch(PlayerHandle sender, ChatManager manager, IEnumerable<PlayerHandle> recepients, string text)
    {
      var prefix = "~action2/";
      var query = text.Substring(prefix.Length).Split('|');

      Lobby lobby = sender.CurrentLobby;
      if (lobby == null)
      {
        Log.Debug("Player {player} requested radio get/set command when he wasn't in the lobby!", sender.Digest);
        return Nothing.Resolved();
      }

      var lobbyOwnerName = lobby.IsUserRoom ? lobby.UserRoom.OwnerName : lobby.Identifier.Substring(0, lobby.Identifier.Length - "_lobby".Length + 1);
      if (query.First() == "getRadioID")
      {
        var value = lobby.RadioURL ?? "no";

        Log.Debug("Player {sender} requested radio stream of lobby {identifier}, returning {value}", sender.Digest, lobby.Identifier, value);
        return sender.Stream.Write(Responses.RunRadio(manager, lobbyOwnerName, value));
      }
      else if (query.First() == "runRadio")
      {
        var url = query.ElementAt(1).Trim();
        if (lobbyOwnerName == sender.Name || sender.AdminOptions.IsEnabled)
        {
          Log.Debug("Player {sender} setting radio URL for lobby {identifier} to {url}", sender.Digest, lobby.Identifier, url);
          lobby.RadioURL = url;
          if (lobby.IsUserRoom)
          {
            lobby.UserRoom.RadioURL = url;
          }

          if (recepients.Count() > 0)
          {
            return new CombinedPromise<Nothing>(recepients.Select(p => p.Stream.Write(Responses.RunRadio(manager, lobbyOwnerName, url))));
          } else
          {
            return Nothing.Resolved();
          }
        }
        else
        {
          Log.Warning("Player {sender} attempted to run radio stream in lobby {identifier}, which is not his room!", sender.Digest, lobby.Identifier);
          return Nothing.Resolved();
        }
      }
      else
      {
        Debug.Assert(false, "invalid request");
        return Nothing.Resolved();
      }
    }
  }
}
