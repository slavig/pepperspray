using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RSG;
using Serilog;
using pepperspray.CIO;
using pepperspray.ChatServer.Game;
using pepperspray.ChatServer.Protocol;
using pepperspray.ChatServer.Services.Events;
using pepperspray.LoginServer;
using pepperspray.SharedServices;

namespace pepperspray.ChatServer.Services
{
  internal class ServerPromptService: IDIService, PlayerLoggedInEvent.IListener, PlayerJoinedLobbyEvent.IListener
  {
    private Configuration config;
    private ChatManager manager;
    private LoginServerListener loginServer;

    public void Inject()
    {
      this.manager = DI.Get<ChatManager>();
      this.config = DI.Get<Configuration>();
      this.loginServer = DI.Get<LoginServerListener>();
    }

    public void PlayerJoinedLobby(PlayerJoinedLobbyEvent ev)
    {
      if (ev.Lobby.IsUserRoom && ev.Lobby.UserRoom.Prompt != null)
      {
        try
        {
          this.loginServer.Emit(ev.Handle.Token, "alert", String.Format("{0}:\n\n{1}", ev.Lobby.UserRoom.Name, ev.Lobby.UserRoom.Prompt));
        }
        catch (LoginServerListener.NotFoundException)
        {
          Log.Warning("Failed to send prompt message to player {player} - socketio connection not found!", ev.Handle.Digest);
        }
      }
    }

    public void PlayerLoggedIn(PlayerLoggedInEvent ev)
    {
      var period = DateTime.Now - ev.Handle.Character.LastLogin;
      if (this.config.ServerPromptText != null && period.TotalHours > this.config.ServerPromptPeriod)
      {
        try
        {
          this.loginServer.Emit(ev.Handle.Token, "alert", this.config.ServerPromptText);
        }
        catch (LoginServerListener.NotFoundException)
        {
          Log.Warning("Failed to send prompt message to player {player} - socketio connection not found!", ev.Handle.Digest);
        }
      }
    }
  }
}
