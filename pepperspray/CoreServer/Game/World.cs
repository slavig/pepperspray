using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RSG;
using pepperspray.CIO;

namespace pepperspray.CoreServer.Game
{
  internal class World
  {
    private Dictionary<string, Lobby> lobbies = new Dictionary<string, Lobby>();
    private Dictionary<string, UserRoom> userRooms = new Dictionary<string, UserRoom>();
    private Dictionary<string, PlayerHandle> players = new Dictionary<string, PlayerHandle>();

    internal IEnumerable<PlayerHandle> Players { get { return this.players.Values;  } }

    internal IEnumerable<UserRoom> PublicRooms
    {
      get { return this.userRooms.Values; }
    }

    internal Lobby FindLobby(string identifier)
    {
      if (this.lobbies.ContainsKey(identifier))
      {
        return this.lobbies[identifier];
      } else
      {
        return null;
      }
    }

    internal Lobby FindOrCreateLobby(string identifier)
    {
      var lobby = this.FindLobby(identifier);
      if (lobby == null)
      {
        lobby = this.CreateLobby(identifier);
      }

      lobby.UserRoom = this.FindUserRoom(identifier);

      return lobby;
    }

    internal PlayerHandle FindPlayer(string name)
    {
      if (this.players.ContainsKey(name))
      {
        return this.players[name];
      } else
      {
        return null;
      }
    }

    internal Lobby CreateLobby(string identifier)
    {
      var lobby = new Lobby(identifier);
      this.lobbies.Add(identifier, lobby);
      return lobby;
    }

    internal void RemoveLobby(Lobby lobby)
    {
      if (this.lobbies.ContainsKey(lobby.Identifier))
      {
        this.lobbies.Remove(lobby.Identifier);
      }
    }

    internal void AddUserRoom(UserRoom room)
    {
      this.userRooms[room.Identifier] = room;
    }

    internal void RemoveUserRoom(string identifier)
    {
      if (this.userRooms.ContainsKey(identifier))
      {
        this.userRooms.Remove(identifier);
      }
    }

    internal UserRoom FindUserRoom(string identifier)
    {
      if (this.userRooms.ContainsKey(identifier))
      {
        return this.userRooms[identifier];
      } else
      {
        return null;
      }
    }

    internal void AddPlayer(PlayerHandle player)
    {
      this.players[player.Name] = player;
    }

    internal void RemovePlayer(PlayerHandle player)
    {
      this.players.Remove(player.Name);

      foreach (var room in this.userRooms.Where(a => a.Value.User == player).ToList())
      {
        this.RemoveUserRoom(room.Key);
      }
    }
  }
}
