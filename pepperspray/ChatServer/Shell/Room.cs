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
  internal class Room: AShellCommand
  {
    private UserRoomService userRoomService = DI.Get<UserRoomService>();
    private Configuration config = DI.Get<Configuration>();
    private ShellDispatcher dispatcher = DI.Get<ShellDispatcher>();
    private ChatManager manager = DI.Get<ChatManager>();
    
    private static int PromptMaxLineLength = 120;
    private static int PromptMaxLines = 25;
    
    internal override bool WouldDispatch(string tag, IEnumerable<string> arguments)
    {
      return tag.Equals("/room");
    }

    internal override IPromise<Nothing> Dispatch(PlayerHandle sender, CommandDomain domain, string tag, IEnumerable<string> arguments)
    {
      if (arguments.Count() < 1 || ((arguments.ElementAt(0).Equals("sex") || arguments.First().Equals("prompt")) && arguments.Count() < 2))
      {
        return this.dispatcher.InvalidUsage(domain);
      }

      var userRoom = this.manager.World.FindUserRoom(sender);
      if (userRoom == null)
      {
        return this.dispatcher.Error(domain, Strings.YOU_DONT_CURRENTLY_OWN_A_ROOM);
      }

      var command = arguments.ElementAt(0);
      if (command.Equals("persist"))
      {
        if (!this.config.DanglingRoom.Enabled)
        {
          return this.dispatcher.Error(domain, Strings.THIS_FEATURE_IS_NOT_ENABLED);
        }

        userRoom.IsSemiPersistent = true;

        var message = String.Format(Strings.ROOM_IS_NOW_PERSISTENT, this.config.DanglingRoom.Timeout.TotalMinutes);
        return this.dispatcher.Output(domain, message);
      } 
      else if (command.Equals("close"))
      {
        return this.userRoomService.CloseRoom(userRoom)
          .Then((a) => this.dispatcher.Output(domain, Strings.ROOM_CLOSED));
      }
      else if (command.Equals("sex"))
      {
        userRoom.IsSexAllowed = !arguments.ElementAtOrDefault(1).Equals("forbid");
        return this.dispatcher.Output(domain, userRoom.IsSexAllowed ? Strings.SEX_IS_NOW_ALLOWED_IN_ROOM : Strings.SEX_IS_NOW_FORBIDDEN_IN_ROOM);
      }
      else if (command.Equals("prompt"))
      {
        var contents = String.Join(" ", arguments.Skip(1));
        var lines = contents.Split('\\');
        if (lines.Count() > Room.PromptMaxLines) {
          return this.dispatcher.Error(domain, Strings.MAXIMUM_LINE_COUNT_EXCEEDED, Room.PromptMaxLines);
        }

        var builder = new StringBuilder();
        foreach (var line in contents.Split('\\'))
        {
          if (line.Length > Room.PromptMaxLineLength)
          {
            return this.dispatcher.Error(domain, Strings.MAXIMUM_LINE_LENGTH_EXCEEDED, Room.PromptMaxLineLength);
          }

          builder.AppendLine(line);
        }

        userRoom.Prompt = builder.ToString();
        return this.dispatcher.Output(domain, Strings.ROOM_PROMPT_HAS_BEEN_SET);
      }
      else if (command.Equals("slowmode"))
      {
        string newChatMode = null;
        var value = arguments.ElementAtOrDefault(1);

        switch (value)
        {
          case "reset":
            userRoom.IsMuted = false;
            userRoom.SlowmodeInterval = TimeSpan.Zero;
            newChatMode = Strings.CHAT_MODE_NO_RESTRICTIONS;
            break;

          case "mute":
            userRoom.IsMuted = true;
            userRoom.SlowmodeInterval = TimeSpan.Zero;
            newChatMode = Strings.CHAT_MODE_MUTED;
            break;

          case null:
            if (userRoom.IsMuted)
            {
              newChatMode = Strings.CHAT_MODE_MUTED;
            }
            else if (userRoom.SlowmodeInterval == TimeSpan.Zero)
            {
              newChatMode = Strings.CHAT_MODE_NO_RESTRICTIONS;
            }
            else
            {
              newChatMode = String.Format(Strings.CHAT_MODE_SLOWMODE, userRoom.SlowmodeInterval);
            }
            break;

          default:
            try
            {
              userRoom.IsMuted = false;
              userRoom.SlowmodeInterval = TimeSpan.FromSeconds(Convert.ToDouble(value));
              newChatMode = String.Format(Strings.CHAT_MODE_SLOWMODE, userRoom.SlowmodeInterval);
            }
            catch (FormatException)
            {
              return this.dispatcher.Error(domain, Strings.INVALID_AMOUNT);
            }
            break;
        }

        return this.dispatcher.Output(domain, Strings.CURRENT_CHAT_MODE, newChatMode);
      }
      else
      {
        return this.dispatcher.Error(domain, Strings.UNKNOWN_COMMAND, command);
      }
    }
  }
}
