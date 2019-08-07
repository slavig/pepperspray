﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RSG;
using Serilog;
using pepperspray.CIO;
using pepperspray.CoreServer.Game;
using pepperspray.CoreServer.Shell;
using pepperspray.CoreServer.Services;
using pepperspray.Utils;
using pepperspray.SharedServices;

namespace pepperspray.CoreServer.Protocol.Requests
{
  internal abstract class Send: ARequest
  {
    protected string contents;
    private string[] messagePrefixes = new string[]
    {
      "~worldchat/",
      "~chat/",
      "~private/",
      "~groupchat/",
    };

    protected ShellDispatcher shellDispatcher = DI.Auto<ShellDispatcher>();
    protected ActionsAuthenticator actionsAuthenticator = DI.Auto<ActionsAuthenticator>();

    internal abstract IEnumerable<PlayerHandle> Recepients(PlayerHandle sender, CoreServer server);

    internal override bool Validate(PlayerHandle sender, CoreServer server)
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

    internal override IPromise<Nothing> Process(PlayerHandle sender, CoreServer server)
    {
      var text = "";
      foreach (string prefix in this.messagePrefixes)
      {
        if (this.contents.StartsWith(prefix))
        {
          text = this.contents.Substring(prefix.Count());
          break;
        }
      }

      if (this.shellDispatcher.ShouldDispatch(text))
      {
        return this.shellDispatcher.Dispatch(sender, server, text);
      }
      else
      {
        var commands = this.Recepients(sender, server)
          .Select(r => r.Stream.Write(Responses.Message(sender, this.contents)));

        return new CombinedPromise<Nothing>(commands);
      }
    }
  }

  internal class SendPM: Send
  {
    private string recepientName;
    private PlayerHandle recepient;

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
        recepientName = arguments[0].ToString(),
        contents = arguments[1].ToString()
      };
    }

    internal override bool Validate(PlayerHandle sender, CoreServer server)
    {
      if (!base.Validate(sender, server))
      {
        return false;
      }

      if (this.recepientName == server.ServerName)
      {
        return true;
      }

      this.recepient = server.World.FindPlayer(this.recepientName);
      if (!this.actionsAuthenticator.ShouldProcess(sender, this.recepient, this.contents))
      {
        return false;
      }

      return true;
    }

    internal override IEnumerable<PlayerHandle> Recepients(PlayerHandle sender, CoreServer server)
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

    internal override IPromise<Nothing> Process(PlayerHandle sender, CoreServer server)
    {
      if (this.recepient != null || this.recepientName.Equals(server.ServerName))
      {
        return base.Process(sender, server);
      }
      else
      {
        return sender.Stream.Write(Responses.ServerPrivateChatMessage(this.recepientName, "Player is offline."));
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

    internal override bool Validate(PlayerHandle sender, CoreServer server)
    {
      if (!base.Validate(sender, server))
      {
        return false;
      }
 
      if (!this.actionsAuthenticator.ShouldProcess(sender, null, this.contents))
      {
        return false;
      }

      return sender.CurrentGroup != null;
    }

    internal override IEnumerable<PlayerHandle> Recepients(PlayerHandle sender, CoreServer server)
    {
      return sender.CurrentGroup.Players.Except(new PlayerHandle[] { sender });
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

    internal override bool Validate(PlayerHandle sender, CoreServer server)
    {
      if (!base.Validate(sender, server))
      {
        return false;
      }
 
      if (!this.actionsAuthenticator.ShouldProcess(sender, null, this.contents))
      {
        return false;
      }

      return sender.CurrentLobby != null;
    }

    internal override IEnumerable<PlayerHandle> Recepients(PlayerHandle sender, CoreServer server)
    {
      return sender.CurrentLobby.Players.Except(new PlayerHandle[] { sender });
    }
  }

  internal class SendWorld: Send
  {
    internal static SendWorld Parse(Message ev)
    {
      return new SendWorld
      {
        contents = ev.data.ToString()
      };
    }

    internal override bool Validate(PlayerHandle sender, CoreServer server)
    {
      if (!base.Validate(sender, server))
      {
        return false;
      }
 
      if (!this.actionsAuthenticator.ShouldProcess(sender, null, this.contents))
      {
        return false;
      }

      return true;
    }

    internal override IEnumerable<PlayerHandle> Recepients(PlayerHandle sender, CoreServer server)
    {
      List<PlayerHandle> recepients = null;
      lock(server)
      {
        recepients = server.World.Players.Except(new PlayerHandle[] { sender }).ToList();
      }

      return recepients;
    }
  }
}
