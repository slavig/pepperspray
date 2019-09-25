using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using pepperspray.ChatServer.Game;
using pepperspray.SharedServices;

namespace pepperspray.ChatServer.Services.Events
{
  internal abstract class ServerEvent
  {
    internal static void Dispatch(ServerEvent ev, IEnumerable<IDIService> services)
    {
      foreach (var service in services)
      {
        if (ev is PlayerLoggedInEvent && service is PlayerLoggedInEvent.IListener)
        {
          (service as PlayerLoggedInEvent.IListener).PlayerLoggedIn(ev as PlayerLoggedInEvent);
        }
        else if (ev is PlayerLoggedOffEvent && service is PlayerLoggedOffEvent.IListener)
        {
          (service as PlayerLoggedOffEvent.IListener).PlayerLoggedOff(ev as PlayerLoggedOffEvent);
        }
        else if (ev is PlayerJoinedLobbyEvent && service is PlayerJoinedLobbyEvent.IListener)
        {
          (service as PlayerJoinedLobbyEvent.IListener).PlayerJoinedLobby(ev as PlayerJoinedLobbyEvent);
        }
        else if (ev is PlayerLeftLobbyEvent && service is PlayerLeftLobbyEvent.IListener)
        {
          (service as PlayerLeftLobbyEvent.IListener).PlayerLeftLobby(ev as PlayerLeftLobbyEvent);
        }
      }
    }
  }

  internal class PlayerLoggedInEvent: ServerEvent
  {
    internal PlayerHandle Handle;

    internal interface IListener
    {
      void PlayerLoggedIn(PlayerLoggedInEvent ev);
    }
  }

  internal class PlayerLoggedOffEvent: ServerEvent
  {
    internal PlayerHandle Handle;

    internal interface IListener
    {
      void PlayerLoggedOff(PlayerLoggedOffEvent ev);
    }
  }

  internal class PlayerJoinedLobbyEvent: ServerEvent
  {
    internal PlayerHandle Handle;
    internal Lobby Lobby;

    internal interface IListener
    {
      void PlayerJoinedLobby(PlayerJoinedLobbyEvent ev);
    }
  }

  internal class PlayerLeftLobbyEvent: ServerEvent
  {
    internal PlayerHandle Handle;
    internal Lobby Lobby;

    internal interface IListener
    {
      void PlayerLeftLobby(PlayerLeftLobbyEvent ev);
    }
  }
}
