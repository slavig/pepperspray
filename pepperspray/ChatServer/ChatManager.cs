using System;
using System.Collections.Generic;
using System.Globalization;
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
using pepperspray.ChatServer.Services.Events;
using pepperspray.Utils;
using pepperspray.SharedServices;
using pepperspray.Resources;

namespace pepperspray.ChatServer
{

  internal class ChatManager: IDIService
  {
    internal World World;
    internal string Name = "pepperspray";
    internal string Monogram = "$";

    private Configuration config;
    private NameValidator nameValidator;
    private UserRoomService userRoomService;
    private GiftsService giftService;

    private EventDispatcher dispatcher;

    private IDIService[] services;

    public void Inject()
    {
      Log.Information("ChatManager initializing");
      this.config = DI.Get<Configuration>();
      this.nameValidator = DI.Get<NameValidator>();
      this.userRoomService = DI.Get<UserRoomService>();
      this.giftService = DI.Get<GiftsService>();

      this.dispatcher = DI.Get<EventDispatcher>();

      this.services = new IDIService[]
      {
        this.userRoomService,
        this.giftService,

        DI.Get<ChatActionsAuthenticator>(),
        DI.Get<ServerPromptService>(),
        DI.Get<GroupService>(),
        DI.Get<LobbyService>(),

        DI.Get<LoginService>(),
        DI.Get<CharacterService>(),
      };

      this.nameValidator.ServerName = this.Monogram;
      this.World = new World();
      
      Log.Information("Loading permanent rooms ({count} total)", this.config.PermanentRooms.Count);
      this.userRoomService.LoadPermanentRooms();

      CIOReactor.Spawn("playerTimeoutWatchdog", () =>
      {
        while (true)
        {
          Thread.Sleep(TimeSpan.FromSeconds(this.config.PlayerInactivityTimeout));

          lock (this)
          {
            Log.Verbose("Checking players timeouts");

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

      CIOReactor.Spawn("excelRecordsWatchdog", () =>
      {
        Log.Debug("Cleaning up expel records");

        while (true)
        {
#if !DEBUG
          var duration = TimeSpan.FromMinutes(10);
#else
          var duration = TimeSpan.FromSeconds(30);
#endif

          Thread.Sleep(duration);

          try
          {
            this.userRoomService.CleanupExpelRecords();
          }
          catch (Exception e)
          {
            Log.Error("Caught exception in excelRecordsWatchdog: {ex}", e);
          }
        }
      });

      CIOReactor.Spawn("danglingRoomWatchdog", () =>
      {
        while (true)
        {
#if !DEBUG
          var duration = TimeSpan.FromMinutes(3);
#else
          var duration = TimeSpan.FromSeconds(30);
#endif

          Thread.Sleep(duration);

          if (!this.config.DanglingRoom.Enabled)
          {
            continue;
          }

          Log.Debug("Cleaning up dangling rooms");
          try
          {
            this.userRoomService.CleanupDanglingRooms();
          }
          catch (Exception e)
          {
            Log.Error("Caught exception in danglingRoomWatchdog: {ex}", e);
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

    internal Nothing ProcessMessage(PlayerHandle handle, Message msg)
    {
      var promise = this.dispatcher.Dispatch(handle, msg);
      if (promise != null)
      {
        this.Sink(promise);
      }

      return new Nothing();
    }

    internal IPromise<Nothing> KickPlayer(PlayerHandle handle, string reason = "")
    {
      Log.Information("Kicking player {player}/{hash}/{endpoint} due to {reason}, terminating shortly after",
        handle.Name,
        handle.Stream.ConnectionHash,
        handle.Stream.ConnectionEndpoint,
        reason);

      this.PlayerLoggedOff(handle);
      return handle.Terminate(new ErrorException("terminated", reason));
    }

    internal void DispatchEvent(ServerEvent ev) {
      ServerEvent.Dispatch(ev, this.services);
    }

    internal void PlayerLoggedIn(PlayerHandle player)
    {
      Log.Information("Player {player} logged in, connection {hash}/{endpoint}", player.Digest, player.Stream.ConnectionHash, player.Stream.ConnectionEndpoint);
      this.DispatchEvent(new PlayerLoggedInEvent { Handle = player });

      lock (this)
      {
        this.World.AddPlayer(player);
      }
    }

    internal void PlayerLoggedOff(PlayerHandle player)
    {
      Log.Information("Player {digest} logged off (connection {hash}/{endpoint})", player.Digest, player.Stream.ConnectionHash, player.Stream.ConnectionEndpoint);
      if (player.IsLoggedIn)
      {
        this.DispatchEvent(new PlayerLoggedOffEvent { Handle = player });
        this.World.RemovePlayer(player);
      }
    }

    internal bool CheckPlayerTimedOut(PlayerHandle handle)
    {
      var delta = DateTime.Now - handle.Stream.LastCommunicationDate;
      if (delta.Seconds > this.config.PlayerInactivityTimeout)
      {
        Log.Information("Disconnecting player {player}/{hash}/{endpoint} due to time out (last heard of {delta} ago)",
          handle.Name,
          handle.Stream.ConnectionHash,
          handle.Stream.ConnectionEndpoint,
          delta);

        this.KickPlayer(handle, Strings.TIMED_OUT);
        return true;
      }

      return false;
    }

    internal bool CheckWorldMessagePermission(PlayerHandle sender)
    {
      var cost = this.config.Currency.WorldChatCost;
      var userCurrency = this.giftService.GetCurrency(sender.User);

      try
      {
        this.giftService.ChangeCurrency(sender.User, -cost);

        var message = String.Format(Strings.MESSAGE_TO_WORLD_HAS_BEEN_SENT, cost, userCurrency - cost);
        this.Sink(sender.Stream.Write(Responses.ServerDirectMessage(this, message)));

        return true;
      }
      catch (GiftsService.NotEnoughCurrencyException e)
      {
        this.Sink(sender.Stream.Write(Responses.ServerWorldMessage(this, String.Format(Strings.YOU_DONT_HAVE_COINS_TO_WRITE_IN_WORLD, cost, userCurrency))));

        return false;
      }
    }
  }
}
