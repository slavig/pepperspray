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

    internal override bool WouldDispatch(string tag)
    {
      return tag.Equals("expel");
    }

    internal override IPromise<Nothing> Dispatch(ShellDispatcher dispatcher, PlayerHandle sender, ChatManager server, string tag, IEnumerable<string> arguments)
    {
      if (arguments.Count() < 1)
      {
        return dispatcher.InvalidUsage(sender, server);
      }

      var currentLobby = sender.CurrentLobby;
      if (currentLobby == null || !currentLobby.IsUserRoom)
      {
        return dispatcher.Error(sender, server, Strings.YOU_ARE_NOT_IN_ROOM);
      }

      if (!this.userRoomService.PlayerCanModerateRoom(sender, currentLobby.UserRoom))
      {
        Log.Information("Player {sender} attempted to run /expel, but didn't have permission for that", sender.Digest);
        return dispatcher.Error(sender, server, Strings.YOU_DONT_HAVE_PERMISSION_TO_MODERATE_ROOM);
      }

      var userRoom = currentLobby.UserRoom;

      if (arguments.ElementAt(0).Equals("\\all"))
      {
        Log.Information("Player {sender} expelling everyone from user room {room}", sender.Digest, userRoom.Identifier);
        return this.userRoomService.ExpellAll(userRoom);
      }

      var player = server.World.FindPlayer(arguments.ElementAt(0));
      if (player == null || player.Character.Equals(sender.Character) || player.Character.Id == userRoom.OwnerId)
      {
        return dispatcher.Error(sender, server, Strings.PLAYER_NOT_FOUND, arguments.First());
      }

      uint duration = 0;
      if (arguments.Count() > 1)
      {
        try
        {
          duration = UInt32.Parse(arguments.ElementAt(1));
        }
        catch (FormatException)
        {
          return dispatcher.Error(sender, server, Strings.DURATION_HAS_BEEN_SPECIFIED_INCORRECTLY);
        }
      } 

      if (duration > 30)
      {
        return dispatcher.Error(sender, server, Strings.DURATION_CANT_BE_MORE_MINUTES);
      }

      if (sender.CurrentLobby == null || !sender.CurrentLobby.IsUserRoom)
      {
        return dispatcher.Error(sender, server, Strings.YOU_SHOULD_BE_IN_THE_ROOM_TO_MODERATE_IT);
      }

      Log.Information("Player {sender} expelling player {player} from user room {room} for {duration} m.", sender.Digest, player.Digest, userRoom.Identifier, duration);
      return this.userRoomService.ExpelPlayer(player, userRoom, TimeSpan.FromMinutes(duration))
        .Then(a => dispatcher.Output(sender, server, Strings.PLAYER_HAS_BEEN_EXPELLED));
    }
  }
}
