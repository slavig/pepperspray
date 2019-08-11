using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

using RSG;
using Serilog;
using pepperspray.CIO;
using pepperspray.ChatServer.Protocol;
using pepperspray.Utils;
using pepperspray.SharedServices;
using pepperspray.LoginServer;

namespace pepperspray.ChatServer.Game
{
  internal class PlayerHandle
  {
    internal bool IsLoggedIn = false;

    internal uint Id;
    internal string Name;
    internal string Token;
    internal string Sex;

    internal User User;
    internal uint CharacterId;
    internal Client Client;

    internal Group CurrentGroup;
    internal Lobby CurrentLobby;

    internal EventStream Stream;

    internal PlayerHandle(EventStream stream)
    {
      this.Stream = stream;
    }

    internal IPromise<Nothing> Terminate(ErrorException exception)
    {
      return this.ErrorAlert(exception)
        .Then(a => new WaitPromise(TimeSpan.FromSeconds(10)))
        .Then(a => a.Then(b => this.Stream.Terminate()));
    }

    internal IPromise<Nothing> ErrorAlert(ErrorException exception)
    {
      if (this.Client != null)
      {
        try
        {
          var message = String.Format("Connection will be terminated in 10 seconds.\n\nReason: {0}\n\nInternal code: {1}.", exception.PlayerMessage, exception.Message);
          return this.Client.Emit("alert", message);
        }
        catch (LoginServerListener.NotFoundException) { }
      }

      if (this.Stream != null)
      {
        var message = String.Format("DISCONNECTED FROM SERVER, disconnecting in 10s.: {0}", exception.PlayerMessage);
        return this.Stream.Write(Responses.MakeshiftAlert(message));
      }

      Log.Warning("Failed to alert player {player} ({hash}/{endpoint}) about exception error {exception}",
        this.Name,
        this.Stream != null ? this.Stream.ConnectionHash : 0,
        this.Stream != null ? this.Stream.ConnectionEndpoint : null,
        exception);

      return Nothing.Resolved();
    }
  }
}
