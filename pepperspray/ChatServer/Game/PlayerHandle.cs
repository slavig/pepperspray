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
using pepperspray.Resources;

namespace pepperspray.ChatServer.Game
{
  [Flags]
  internal enum AdminFlags
  {
    AdminBroadcast = 1 << 0,
    ConfigReload = 1 << 1,
    PlayerKick = 1 << 2,
    Currency = 1 << 3,
    PlayerManagement = 1 << 4,
    OnlinePlayerLookup = 1 << 5,
    RoomManagement = 1 << 6,
    DisabledAuthenticator = 1 << 7,
    AdminPlayerManagement = 1 << 8,
  }

  internal class PlayerHandle: IEquatable<PlayerHandle>
  {
    internal class AdminOptionsConfiguration
    {
      private AdminFlags flags = 0;
      internal AdminOptionsConfiguration(int flags)
      {
        this.flags = (AdminFlags)flags;
      }
      
      internal AdminOptionsConfiguration()
      {
      }

      internal bool HasFlag(AdminFlags flag)
      {
        return this.flags.HasFlag(flag);
      }
    }

    internal bool IsLoggedIn = false;

    internal uint Id;
    internal string Name;
    internal string Token;
    internal string Sex;

    internal User User;
    internal AdminOptionsConfiguration AdminOptions = new AdminOptionsConfiguration();
    internal Character Character;

    internal DateTime LoggedAt = DateTime.Now;

    internal Group CurrentGroup;
    internal Lobby CurrentLobby;

    internal EventStream Stream;

    internal string Digest
    {
      get
      {
        return String.Format("{0}#{1}/{2}", this.Name, this.Stream.ConnectionHash, this.Stream.ConnectionEndpoint);
      }
    }

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
        return this.CurrentLobby != null ? this.CurrentLobby.IsPrivateRoom ? "private_room" : this.CurrentLobby.Identifier : "none";
      }
    }

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
      if (this.Token != null && loginServer.HasClient(this.Token))
      {
        try
        {
          var message = String.Format(Strings.DISCONNECTING_ON_TIMEOUT_LONG, exception.PlayerMessage, exception.Message);
          return loginServer.Emit(this.Token, "alert", message);
        }
        catch (LoginServerListener.NotFoundException) { }
      }

      if (this.Stream != null)
      {
        var message = String.Format(Strings.DISCONNECTING_ON_TIMEOUT_SHORT, exception.PlayerMessage);
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
