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
  internal class PlayerHandle: IEquatable<PlayerHandle>
  {
    internal bool IsLoggedIn = false;

    internal uint Id;
    internal string Name;
    internal string Token;
    internal string Sex;

    internal User User;
    internal Character Character;

    internal DateTime LoggedAt = DateTime.Now;

    internal Group CurrentGroup;
    internal Lobby CurrentLobby;


    internal string CurrentLobbyName
    {
      get
      {
        return this.CurrentLobby != null ? this.CurrentLobby.Name ?? "unknown" : "none";
      }
    }

    internal string CurrentLobbyIdentifier
    {
      get
      {
        return this.CurrentLobby != null ? this.CurrentLobby.Identifier : "none";
      }
    }

    internal EventStream Stream;

    internal PlayerHandle(EventStream stream)
    {
      this.Stream = stream;
    }

    public bool Equals(PlayerHandle other)
    {
      return this.Id == other.Id;
    }

    internal IPromise<Nothing> Terminate(ErrorException exception)
    {
      this.IsLoggedIn = false;
      this.ErrorAlert(exception);

      return WaitPromise.FromTimeSpan(TimeSpan.FromSeconds(10)).Then(a => this.Stream.Terminate());
    }

    internal IPromise<Nothing> ErrorAlert(ErrorException exception)
    {
      var loginServer = DI.Get<LoginServerListener>();
      if (loginServer.HasClient(this.Token))
      {
        try
        {
          var message = String.Format("Connection will be terminated in 10 seconds.\n\nReason: {0}\n\nInternal code: {1}.", exception.PlayerMessage, exception.Message);
          return loginServer.Emit(this.Token, "alert", message);
        }
        catch (LoginServerListener.NotFoundException) { }
      }

      if (this.Stream != null)
      {
        var message = String.Format("DISCONNECTED FROM SERVER, disconnecting in 10s.: {0}", exception.PlayerMessage);
        return this.Stream.Write(Responses.MakeshiftAlert(message));
      }

      Log.Error("Failed to alert player {player} ({hash}/{endpoint}) about exception error {exception}",
        this.Name,
        this.Stream != null ? this.Stream.ConnectionHash : 0,
        this.Stream != null ? this.Stream.ConnectionEndpoint : null,
        exception);

      return Nothing.Resolved();
    }
  }
}
