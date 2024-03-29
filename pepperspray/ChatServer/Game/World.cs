﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RSG;
using Serilog;
using pepperspray.CIO;
using pepperspray.SharedServices;

namespace pepperspray.ChatServer.Game
{
  internal class World
  {
    private Dictionary<string, UserRoom> userRooms = new Dictionary<string, UserRoom>();
    private Dictionary<string, PlayerHandle> players = new Dictionary<string, PlayerHandle>();

    internal Dictionary<string, Lobby> Lobbies = new Dictionary<string, Lobby>();

    internal IEnumerable<PlayerHandle> Players { get { return this.players.Values;  } }
    internal IEnumerable<UserRoom> UserRooms
    {
      get { return this.userRooms.Values; }
    }

    internal Lobby FindLobby(string identifier)
    {
      Lobby lobby = null;
      this.Lobbies.TryGetValue(identifier, out lobby);
      return lobby;
    }

    internal Lobby FindOrCreateLobby(string identifier)
    {
      var lobby = this.FindLobby(identifier);
      var room = this.FindUserRoom(identifier);

      if (lobby == null)
      {
        lobby = this.CreateLobby(identifier);
        if (room != null)
        {
          lobby.RadioURL = room.RadioURL;
          lobby.UserRoom = room;
          room.Lobby = lobby;
        }
      }

      return lobby;
    }

    internal Group FindGroup(int identifier)
    {
      foreach (var player in this.Players)
      {
        if (player.CurrentGroup.Identifier == identifier)
        {
          return player.CurrentGroup;
        }
      }

      return null;
    }

    internal PlayerHandle FindPlayer(string name, uint id = 0)
    {
      if (this.players.ContainsKey(name))
      {
        return this.players[name];
      }

      if (id == 0)
      {
        return null;
      }

      foreach (PlayerHandle player in this.players.Values)
      {
        if (player.Id == id)
        {
          return player;
        }
      }

      return null;
    }

    internal PlayerHandle FindPlayerByUser(User user)
    {
      foreach (PlayerHandle player in this.players.Values)
      {
        if (player.User.Id == user.Id)
        {
          return player;
        }
      }

      return null;
    }

    internal PlayerHandle FindPlayerById(uint id)
    {
      foreach (PlayerHandle player in this.players.Values)
      {
        if (player.Id == id)
        {
          return player;
        }
      }

      return null;
    }

    internal Lobby CreateLobby(string identifier)
    {
      Log.Information("Creating lobby {identifier}", identifier);

      var lobby = new Lobby(identifier);
      this.Lobbies.Add(identifier, lobby);
      return lobby;
    }

    internal void RemoveLobby(Lobby lobby)
    {
      if (this.Lobbies.ContainsKey(lobby.Identifier))
      {
        Log.Information("Removing lobby {identifier}", lobby.Identifier);

        this.Lobbies.Remove(lobby.Identifier);
        if (lobby.UserRoom != null)
        {
          lobby.UserRoom.Lobby = null;
        }

      } else
      {
        Log.Warning("Couldn't remove lobby - {identifier} doesn't exist", lobby.Identifier);
      }
    }

    internal void AddUserRoom(UserRoom room)
    {
      Log.Information("Adding user room {id}/{name} from {player_name}", room.Identifier, room.Name, room.OwnerName);

      this.userRooms[room.Identifier] = room;
    }

    internal void RemoveUserRoom(string identifier)
    {
      if (this.userRooms.ContainsKey(identifier))
      {
        Log.Information("Removing user room {id}", identifier);
        this.userRooms.Remove(identifier);
      }
      else
      {
        Log.Warning("Cound't remove user room - {id} doesn't exist", identifier);
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

    internal UserRoom FindUserRoom(PlayerHandle handle)
    {
      foreach (var room in this.userRooms.Values)
      {
        if (room.OwnerId == handle.Id)
        {
          return room;
        }
      }

      return null;
    }

    internal void AddPlayer(PlayerHandle player)
    {
      this.players[player.Name] = player;
    }

    internal void RemovePlayer(PlayerHandle player)
    {
      this.players.Remove(player.Name);
    }
  }
}
