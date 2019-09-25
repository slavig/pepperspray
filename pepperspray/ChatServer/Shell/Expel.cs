using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Serilog;
using RSG;
using pepperspray.CIO;
using pepperspray.ChatServer.Game;
using pepperspray.ChatServer.Protocol;
using pepperspray.ChatServer.Services;
using pepperspray.SharedServices;
using pepperspray.Utils;
using pepperspray.Resources;

namespace pepperspray.ChatServer.Shell
{
  internal class Expel: AShellCommand
  {
    private UserRoomService userRoomService = DI.Get<UserRoomService>();
    private Configuration config = DI.Get<Configuration>();
    private ShellDispatcher dispatcher = DI.Get<ShellDispatcher>();
    private ChatManager manager = DI.Get<ChatManager>();

    internal override bool WouldDispatch(string tag, IEnumerable<string> arguments)
    {
      return tag.Equals("/expel");
    }

    internal override IPromise<Nothing> Dispatch(PlayerHandle sender, CommandDomain domain, string tag, IEnumerable<string> arguments)
    {
      var currentLobby = sender.CurrentLobby;
      if (currentLobby == null || !currentLobby.IsUserRoom)
      {
        return this.dispatcher.Error(domain, Strings.YOU_ARE_NOT_IN_ROOM);
      }

      if (!this.userRoomService.PlayerCanModerateRoom(sender, currentLobby.UserRoom))
      {
        Log.Information("Player {sender} attempted to run /expel, but didn't have permission for that", sender.Digest);
        return this.dispatcher.Error(domain, Strings.YOU_DONT_HAVE_PERMISSION_TO_MODERATE_ROOM);
      }

      var userRoom = currentLobby.UserRoom;

      string name;
      uint duration;
      try
      {
        try
        {
          // single argument - duration
          name = ".";
          duration = UInt32.Parse(arguments.ElementAt(0).Trim());
        }
        catch (ArgumentOutOfRangeException)
        {
          // no arguments
          name = ".";
          duration = 0;
        }
        catch (FormatException)
        {
          // single or two arguments
          name = arguments.First().Trim();
          if (arguments.Count() > 1)
          {
            duration = UInt32.Parse(arguments.ElementAt(1).Trim());
          } 
          else
          {
            duration = 0;
          }
        }
      }
      catch (ArgumentOutOfRangeException)
      {
        return this.dispatcher.InvalidUsage(domain);
      }
      catch (FormatException)
      {
        return this.dispatcher.InvalidUsage(domain);
      }

      if (name.Equals("\\all"))
      {
        Log.Information("Player {sender} expelling everyone from user room {room}", sender.Digest, userRoom.Identifier);
        return this.userRoomService.ExpellAll(userRoom);
      }

      PlayerHandle player = CommandUtils.GetPlayer(name, domain, this.manager);
      if (player == null || player.Character.Equals(sender.Character) || player.Character.Id == userRoom.OwnerId)
      {
        return this.dispatcher.Error(domain, Strings.PLAYER_NOT_FOUND, name);
      }

      if (duration > this.config.Expel.MaxDuration.TotalMinutes)
      {
        return this.dispatcher.Error(domain, Strings.DURATION_CANT_BE_MORE_MINUTES, this.config.Expel.MaxDuration.TotalMinutes);
      }

      if (sender.CurrentLobby == null || !sender.CurrentLobby.IsUserRoom)
      {
        return this.dispatcher.Error(domain, Strings.YOU_SHOULD_BE_IN_THE_ROOM_TO_MODERATE_IT);
      }

      Log.Information("Player {sender} expelling player {player} from user room {room} for {duration} m.", sender.Digest, player.Digest, userRoom.Identifier, duration);
      return this.userRoomService.ExpelPlayer(player, userRoom, TimeSpan.FromMinutes(duration))
        .Then(a => this.dispatcher.Output(domain, Strings.PLAYER_HAS_BEEN_EXPELLED));
    }
  }
}
