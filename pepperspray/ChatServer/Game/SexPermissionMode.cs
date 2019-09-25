using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pepperspray.ChatServer.Game
{
  internal enum SexPermissionMode
  {
    Default = 0,
    Deny = 1,
    AllowPose = 2,
  }

  static class SexPermissionModeUtils
  {
    internal static String Identifier(this SexPermissionMode mode)
    {
      switch (mode)
      {
        case SexPermissionMode.Default:
          return "default";
        case SexPermissionMode.Deny:
          return "deny";
        case SexPermissionMode.AllowPose:
          return "sub";
        default:
          throw new ArgumentException();
      }
    }

    internal static SexPermissionMode FromString(String mode)
    {
      switch (mode)
      {
        case "default":
          return SexPermissionMode.Default;
        case "deny":
          return SexPermissionMode.Deny;
        case "sub":
          return SexPermissionMode.AllowPose;
        default:
          throw new ArgumentException();
      }
    }
  }
}
