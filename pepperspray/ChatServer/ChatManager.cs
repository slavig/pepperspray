﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;

using RSG;
using Serilog;
using pepperspray.CIO;
using pepperspray.ChatServer.Protocol;
using pepperspray.ChatServer.Game;
using pepperspray.ChatServer.Services;
using pepperspray.Utils;
using pepperspray.SharedServices;

namespace pepperspray.ChatServer
{

  internal class ChatManager
  {
    internal World World;
    internal string Name = "pepperspray";

    private NameValidator nameValidator;
    private Configuration config;
    private UserRoomService userRoomService;
    private GroupService groupService;
    private ActionsAuthenticator actionsAuthenticator;
    private EventDispatcher dispatcher;
    private CharacterService characterService;

    public ChatManager()
    {
      DI.Register(this);

      this.config = DI.Get<Configuration>();
      this.nameValidator = DI.Auto<NameValidator>();
      this.userRoomService = DI.Auto<UserRoomService>();
      this.actionsAuthenticator = DI.Auto<ActionsAuthenticator>();
      this.groupService = DI.Auto<GroupService>();
      this.dispatcher = DI.Auto<EventDispatcher>();
      this.characterService = DI.Auto<CharacterService>();

      this.World = new World();
      this.nameValidator.ServerName = this.Name;

      CIOReactor.Spawn("playerTimeoutWatchdog", () =>
      {
        while (true)
        {
          Thread.Sleep(TimeSpan.FromSeconds(this.config.PlayerInactivityTimeout));

          lock (this)
          {
            Log.Debug("Checking players timeouts");

            try
            {
              var players = this.World.Players.ToArray();
              foreach (var handle in players)
              {
                this.CheckPlayerTimedOut(handle);
              }
            } catch (Exception e)
            {
              Log.Error("Caught exception in playerTimeoutWatchdog: {ex}", e);
            }
          }
        }
      });
    }

    internal void Sink<T>(IPromise<T> promise)
    {

    }

    internal PlayerHandle ConnectPlayer(CIOSocket socket)
    {
      var handle = new PlayerHandle(new EventStream(socket));
      Log.Information("Connecting player {hash}/{endpoint}", handle.Stream.ConnectionHash, handle.Stream.ConnectionEndpoint);
      this.Sink(handle.Stream.Write(Responses.Connected()));

      return handle;
    }

    internal Nothing ProcessCommand(PlayerHandle handle, Message msg)
    {
      Log.Debug("<= {player}@{lobby} {event_description}", 
        handle.Name, 
        handle.CurrentLobby != null ? handle.CurrentLobby.Identifier : null, 
        msg.DebugDescription());

      var promise = this.dispatcher.Dispatch(handle, msg);
      if (promise != null)
      {
        this.Sink(promise);
      }

      return new Nothing();
    }

    internal IPromise<Nothing> KickPlayer(PlayerHandle handle, string reason = "")
    {
      Log.Debug("Kicking player {player}/{hash}/{endpoint} due to {reason}",
        handle.Name,
        handle.Stream.ConnectionHash,
        handle.Stream.ConnectionEndpoint,
        reason);

      this.PlayerLoggedOff(handle);
      return handle.Terminate(new ErrorException("kicked", reason));
    }

    internal void PlayerLoggedIn(PlayerHandle player)
    {
      Log.Information("Player {name} logged in, connection {hash}/{endpoint}", player.Name, player.Stream.ConnectionHash, player.Stream.ConnectionEndpoint);
      this.groupService.PlayerLoggedIn(player);

      lock (this)
      {
        this.World.AddPlayer(player);
      }
    }

    internal void PlayerLoggedOff(PlayerHandle player)
    {
      Log.Information("Player {name} logged off (connection {hash}/{endpoint})", player.Name, player.Stream.ConnectionHash, player.Stream.ConnectionEndpoint);
      this.userRoomService.PlayerLoggedOff(player);
      this.actionsAuthenticator.PlayerLoggedOff(player);
      this.groupService.PlayerLoggedOff(player);

      if (player.Character != null)
      {
        this.characterService.LogoutCharacter(player.Character);
      }

      PlayerHandle[] playersToNotify = new PlayerHandle[] { };
      lock (this)
      {
        if (player.CurrentLobby != null)
        {
          player.CurrentLobby.RemovePlayer(player);
          playersToNotify = player.CurrentLobby.Players.ToArray();

          if (player.CurrentLobby.Players.Count() == 0)
          {
            this.World.RemoveLobby(player.CurrentLobby);
          }
        }

        if (player.IsLoggedIn)
        {
          this.World.RemovePlayer(player);
        }
      }

      Log.Debug("Notifying {number_of_players} that {name} logged off.", playersToNotify.Count(), player.Name);
      this.Sink(new CombinedPromise<Nothing>(playersToNotify.Select(b => b.Stream.Write(Responses.PlayerLeave(player)))));
    }

    internal bool CheckPlayerTimedOut(PlayerHandle handle)
    {
      var delta = DateTime.Now - handle.Stream.LastCommunicationDate;
      Log.Verbose("Player {name} last seen in {delta}", handle.Name, delta.Seconds);

      if (delta.Seconds > this.config.PlayerInactivityTimeout)
      {
        Log.Debug("Disconnecting player {player}/{hash}/{endpoint} due to time out (last heard of {delta} ago)",
          handle.Name,
          handle.Stream.ConnectionHash,
          handle.Stream.ConnectionEndpoint,
          delta);

        this.KickPlayer(handle, "Timed out.");
        return true;
      }

      return false;
    }
  }
}
