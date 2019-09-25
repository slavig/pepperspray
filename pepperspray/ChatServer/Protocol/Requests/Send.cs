using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RSG;
using Serilog;
using pepperspray.CIO;
using pepperspray.ChatServer.Game;
using pepperspray.ChatServer.Shell;
using pepperspray.ChatServer.Services;
using pepperspray.Utils;
using pepperspray.SharedServices;
using pepperspray.Resources;

namespace pepperspray.ChatServer.Protocol.Requests
{
  internal abstract class Send: ARequest
  {
    protected string contents;

    protected ShellDispatcher shellDispatcher = DI.Get<ShellDispatcher>();
    protected ChatActionsAuthenticator actionsAuthenticator = DI.Get<ChatActionsAuthenticator>();
    protected AppearanceRequestService appearanceRequestService = DI.Get<AppearanceRequestService>();
    protected OfflineMessageService offlineMessageService = DI.Get<OfflineMessageService>();
    protected RoomRadioService roomRadioService = DI.Get<RoomRadioService>();
    protected LobbyService lobbyService = DI.Get<LobbyService>();

    internal abstract IEnumerable<PlayerHandle> Recipients(PlayerHandle sender, ChatManager server);

    internal override string DebugDescription()
    {
      var command = Send.TextCommand(this.contents);
      if (command != "")
      {
        return String.Format("msg (data = {0})",
#if DEBUG
          this.contents
#else
          command + "HIDDEN"
#endif
          );
      }
      else
      {
        return base.DebugDescription();
      }
    }

    internal override bool Validate(PlayerHandle sender, ChatManager server)
    {
      if (!base.Validate(sender, server))
      {
        return false;
      }
      else
      {
        return true;
      }
    }

    internal bool WillServicesDispatch()
    {
      var text = Send.StripCommand(this.contents);
      if (this.shellDispatcher.ShouldDispatch(text))
      {
        return true;
      }
      else if (this.appearanceRequestService.ShouldDispatch(text))
      {
        return true;
      }
      else if (this.roomRadioService.ShouldDispatch(text))
      {
        return true;
      } else
      {
        return false;
      }
    }

    internal override IPromise<Nothing> Process(PlayerHandle sender, ChatManager server)
    {
      var recipients = this.Recipients(sender, server);
      var text = Send.StripCommand(this.contents);
      var domain = new CommandDomain(Send.TextCommand(this.contents), sender, recipients);

      if (this.shellDispatcher.ShouldDispatch(text))
      {
        return this.shellDispatcher.Dispatch(sender, domain, text);
      }
      else if (this.appearanceRequestService.ShouldDispatch(text))
      {
        return this.appearanceRequestService.Dispatch(sender, recipients, server, text);
      }
      else if (this.roomRadioService.ShouldDispatch(text))
      {
        return this.roomRadioService.Dispatch(sender, server, recipients, text);
      }
      else
      {
        var authenticationResult = this.actionsAuthenticator.Authenticate(sender, recipients.Count() == 1 ? recipients.First() : null, this.contents);
        switch (authenticationResult)
        {
          case ChatActionsAuthenticator.AuthenticationResult.Ok:
            return new CombinedPromise<Nothing>(recipients.Select((r) => r.Stream.Write(Responses.Message(sender, this.contents))));

          case ChatActionsAuthenticator.AuthenticationResult.SexDisabledInRoom:
            return sender.Stream.Write(Responses.ServerPrivateChatMessage(server.Monogram, 0, Strings.SEX_IS_FORBIDDEN_IN_ROOM));

          case ChatActionsAuthenticator.AuthenticationResult.NotAuthenticated:
          case ChatActionsAuthenticator.AuthenticationResult.Ignored:
          default:
            return Nothing.Resolved();
        }
      }
    }

    internal static string TextCommand(string input)
    {
      string[] messagePrefixes = new string[]
      {
        "~worldchat/",
        "~chat/",
        "~private/",
        "~groupchat/",
      };

      foreach (string prefix in messagePrefixes)
      {
        if (input.StartsWith(prefix))
        {
          return prefix;
        }
      }

      return "";
    }

    internal static string StripCommand(string input)
    {
      return input.Substring(Send.TextCommand(input).Count());
    }
  }

  internal class SendPM: Send
  {
    private string recipientName;
    private PlayerHandle recipient;

    internal override string DebugDescription()
    {
      return base.DebugDescription() + ", to " + this.recipientName;
    }

    internal static SendPM Parse(Message ev)
    {
      if (!(ev.data is List<object>))
      {
        return null;
      }
      var arguments = ev.data as List<object>;
      if (arguments.Count() < 2)
      {
        return null;
      }

      return new SendPM
      {
        recipientName = CharacterService.StripCharacterName(arguments[0].ToString()),
        contents = arguments[1].ToString()
      };
    }

    internal override bool Validate(PlayerHandle sender, ChatManager server)
    {
      if (!base.Validate(sender, server))
      {
        return false;
      }

      if (this.recipientName == server.Monogram)
      {
        return true;
      }

      this.recipient = server.World.FindPlayer(this.recipientName);
      return true;
    }

    internal override IEnumerable<PlayerHandle> Recipients(PlayerHandle sender, ChatManager server)
    {
      if (this.recipient != null)
      {
        return new PlayerHandle[] { this.recipient };
      }
      else
      {
        return new PlayerHandle[] { };
      }
    }

    internal override IPromise<Nothing> Process(PlayerHandle sender, ChatManager server)
    {
      if (this.recipient != null || this.recipientName.Equals(server.Monogram) || this.WillServicesDispatch())
      {
        return base.Process(sender, server);
      }
      else if (this.contents.StartsWith("~private/"))
      {
        return this.offlineMessageService.QueueMessage(sender, this.recipientName, Send.StripCommand(this.contents));
      } 
      else
      {
        return Nothing.Resolved();
      }
    }
  }

  internal class SendGroup: Send
  {
    internal static SendGroup Parse(Message ev)
    {
      return new SendGroup
      {
        contents = ev.data.ToString()
      };
    }

    internal override bool Validate(PlayerHandle sender, ChatManager server)
    {
      if (!base.Validate(sender, server))
      {
        return false;
      }
 
      return sender.CurrentGroup != null;
    }

    internal override IEnumerable<PlayerHandle> Recipients(PlayerHandle sender, ChatManager server)
    {
      lock(server)
      {
        return sender.CurrentGroup.Players.Except(new PlayerHandle[] { sender }).ToList();
      }
    }
  }

  internal class SendLocal: Send
  {
    internal static SendLocal Parse(Message ev)
    {
      return new SendLocal
      {
        contents = ev.data.ToString()
      };
    }

    internal override bool Validate(PlayerHandle sender, ChatManager server)
    {
      if (!base.Validate(sender, server))
      {
        return false;
      }
 
      if (sender.CurrentLobby == null)
      {
        return false;
      }

      return true;
    }

    internal override IEnumerable<PlayerHandle> Recipients(PlayerHandle sender, ChatManager server)
    {
      lock(server)
      {
        return sender.CurrentLobby.Players.Except(new PlayerHandle[] { sender }).ToList();
      }
    }

    internal override IPromise<Nothing> Process(PlayerHandle sender, ChatManager server)
    {
      if (this.WillServicesDispatch())
      {
        return base.Process(sender, server);
      }

      if (this.contents.StartsWith("~chat") && !this.lobbyService.CheckSlowmodeTimer(sender.CurrentLobby, sender))
      {
        return Nothing.Resolved();
      }

      return base.Process(sender, server);
    }
  }

  internal class SendWorld: Send
  {
    private GiftsService giftsService = DI.Get<GiftsService>();
    private Configuration config = DI.Get<Configuration>();

    internal static SendWorld Parse(Message ev)
    {
      return new SendWorld
      {
        contents = ev.data.ToString()
      };
    }

    internal override bool Validate(PlayerHandle sender, ChatManager server)
    {
      if (!base.Validate(sender, server))
      {
        return false;
      }
 
      return true;
    }

    internal override IEnumerable<PlayerHandle> Recipients(PlayerHandle sender, ChatManager server)
    {
      List<PlayerHandle> recipients = null;
      lock(server)
      {
        recipients = server.World.Players.Except(new PlayerHandle[] { sender }).ToList();
      }

      return recipients;
    }

    internal override IPromise<Nothing> Process(PlayerHandle sender, ChatManager server)
    {
      if (this.WillServicesDispatch())
      {
        return base.Process(sender, server);
      }

      if (!this.lobbyService.CheckSlowmodeTimerInWorld(sender))
      {
        return Nothing.Resolved();
      }

      if (!server.CheckWorldMessagePermission(sender))
      {
        return Nothing.Resolved();
      }

      return base.Process(sender, server);
    }
  }
}
