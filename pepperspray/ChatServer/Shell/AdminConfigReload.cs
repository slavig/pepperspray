using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Serilog;
using RSG;
using pepperspray.CIO;
using pepperspray.ChatServer.Game;
using pepperspray.ChatServer.Services;
using pepperspray.SharedServices;
using pepperspray.Resources;

namespace pepperspray.ChatServer.Shell
{
  internal class AdminConfigReload: AShellCommand
  {
    private Configuration config = DI.Get<Configuration>();
    private UserRoomService userRoomService = DI.Get<UserRoomService>();

    internal override bool RequireAdmin()
    {
      return true;
    }

    internal override bool WouldDispatch(string tag)
    {
      return tag.Equals("aconfigreload");
    }

    internal override IPromise<Nothing> Dispatch(ShellDispatcher dispatcher, PlayerHandle sender, ChatManager server, string tag, IEnumerable<string> arguments)
    {
#if !DEBUG
      try
      {
#endif
        this.config.LoadConfiguration();
        this.userRoomService.LoadPermanentRooms();

        return dispatcher.Output(sender, server, Strings.CONFIGURATION_FILE_RELOADED);
#if !DEBUG
      }
      catch (Configuration.LoadException e)
      {
        Log.Warning("Failed to reload config (stage {stage}): {exception}", e.Stage, e.UnderlyingException);
        return dispatcher.Error(sender, server, Strings.FAILED_TO_RELOAD_CONFIGURATION + e);
      }
#endif
    }
  }
}
