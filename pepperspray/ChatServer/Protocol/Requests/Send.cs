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

    internal abstract IEnumerable<PlayerHandle> Recepients(PlayerHandle sender, ChatManager server);

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
      var recepients = this.Recepients(sender, server);
      var text = Send.StripCommand(this.contents);
      var domain = new CommandDomain(Send.TextCommand(this.contents), recepients);

      if (this.shellDispatcher.ShouldDispatch(text))
      {
        return this.shellDispatcher.Dispatch(sender, domain, text);
      }
      else if (this.appearanceRequestService.ShouldDispatch(text))
      {
        return this.appearanceRequestService.Dispatch(sender, recepients, server, text);
      }
      else if (this.roomRadioService.ShouldDispatch(text))
      {
        return this.roomRadioService.Dispatch(sender, server, recepients, text);
      }
      else
      {
        var authenticationResult = this.actionsAuthenticator.Authenticate(sender, recepients.Count() == 1 ? recepients.First() : null, this.contents);
        switch (authenticationResult)
        {
          case ChatActionsAuthenticator.AuthenticationResult.Ok:
            return new CombinedPromise<Nothing>(recepients.Select((r) => r.Stream.Write(Responses.Message(sender, this.contents))));
          case ChatActionsAuthenticator.AuthenticationResult.SexDisabled:
            return sender.Stream.Write(Responses.ServerPrivateChatMessage(server.Monogram, 0, Strings.SEX_IS_FORBIDDEN_IN_ROOM));
          case ChatActionsAuthenticator.AuthenticationResult.NotAuthenticated:
            return Nothing.Resolved();
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
    private string recepientName;
    private PlayerHandle recepient;

    internal override string DebugDescription()
    {
      return base.DebugDescription() + ", to " + this.recepientName;
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
        recepientName = CharacterService.StripCharacterName(arguments[0].ToString()),
        contents = arguments[1].ToString()
      };
    }

    internal override bool Validate(PlayerHandle sender, ChatManager server)
    {
      if (!base.Validate(sender, server))
      {
        return false;
      }

      if (this.recepientName == server.Monogram)
      {
        return true;
      }

      this.recepient = server.World.FindPlayer(this.recepientName);
      return true;
    }

    internal override IEnumerable<PlayerHandle> Recepients(PlayerHandle sender, ChatManager server)
    {
      if (this.recepient != null)
      {
        return new PlayerHandle[] { this.recepient };
      }
      else
      {
        return new PlayerHandle[] { };
      }
    }

    internal override IPromise<Nothing> Process(PlayerHandle sender, ChatManager server)
    {
      if (this.recepient != null || this.recepientName.Equals(server.Monogram) || this.WillServicesDispatch())
      {
        return base.Process(sender, server);
      }
      else if (this.contents.StartsWith("~private/"))
      {
        return this.offlineMessageService.QueueMessage(sender, this.recepientName, Send.StripCommand(this.contents));
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

    internal override IEnumerable<PlayerHandle> Recepients(PlayerHandle sender, ChatManager server)
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
 
      return sender.CurrentLobby != null;
    }

    internal override IEnumerable<PlayerHandle> Recepients(PlayerHandle sender, ChatManager server)
    {
      lock(server)
      {
        return sender.CurrentLobby.Players.Except(new PlayerHandle[] { sender }).ToList();
      }
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

    internal override IEnumerable<PlayerHandle> Recepients(PlayerHandle sender, ChatManager server)
    {
      List<PlayerHandle> recepients = null;
      lock(server)
      {
        recepients = server.World.Players.Except(new PlayerHandle[] { sender }).ToList();
      }

      return recepients;
    }

    internal override IPromise<Nothing> Process(PlayerHandle sender, ChatManager server)
    {
      if (this.WillServicesDispatch())
      {
        return base.Process(sender, server);
      }

      if (server.CheckWorldMessagePermission(sender))
      {
        return base.Process(sender, server);
      }
      else
      {
        return Nothing.Resolved();
      }
    }
  }
}
