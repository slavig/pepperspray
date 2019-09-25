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
using pepperspray.Utils;
using pepperspray.SharedServices;
using pepperspray.Resources;

namespace pepperspray.ChatServer.Shell
{
  internal class Sex: AShellCommand
  {
    private ShellDispatcher dispatcher = DI.Get<ShellDispatcher>();

    internal override bool WouldDispatch(string tag, IEnumerable<string> arguments)
    {
      return tag.Equals("/sex");
    }

    internal override IPromise<Nothing> Dispatch(PlayerHandle sender, CommandDomain domain, string tag, IEnumerable<string> arguments)
    {
      var didChange = true;
      switch (arguments.FirstOrDefault())
      {
        case null:
          didChange = false;
          break;
        case "reset":
          sender.SexPermissions = SexPermissionMode.Default;
          break;
        default:
          try
          {
            sender.SexPermissions = SexPermissionModeUtils.FromString(arguments.ElementAt(0));
          }
          catch (ArgumentException) { return dispatcher.InvalidUsage(domain); }
          catch (IndexOutOfRangeException) { return dispatcher.InvalidUsage(domain); }

          break;
      }

      var message = "";
      if (didChange)
      {
        message += Strings.YOUR_MODE_HAS_BEEN_CHANGED + " ";
      }

      var explanation = "";
      switch (sender.SexPermissions)
      {
        case SexPermissionMode.Default:
          explanation = Strings.SEX_MODE_DESCRIPTION_DEFAULT;
          break;
        case SexPermissionMode.Deny:
          explanation = Strings.SEX_MODE_DESCRIPTION_DENY;
          break;
        case SexPermissionMode.AllowPose:
          explanation = Strings.SEX_MODE_DESCRIPTION_POSEAGREE;
          break;
      }

      message += String.Format(Strings.CURRENT_SEX_MODE, sender.SexPermissions.Identifier(), explanation);
      return this.dispatcher.Output(domain, message);
    }
  }
}
