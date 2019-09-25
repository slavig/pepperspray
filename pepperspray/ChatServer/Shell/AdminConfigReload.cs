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
    private ShellDispatcher dispatcher = DI.Get<ShellDispatcher>();
    private ChatManager manager = DI.Get<ChatManager>();

    internal override bool HasPermissionToExecute(PlayerHandle sender)
    {
      return sender.AdminOptions.HasFlag(AdminFlags.ConfigReload);
    }

    internal override bool WouldDispatch(string tag, IEnumerable<string> arguments)
    {
      return tag.Equals("/aconfigreload");
    }

    internal override IPromise<Nothing> Dispatch(PlayerHandle sender, CommandDomain domain, string tag, IEnumerable<string> arguments)
    {
#if !DEBUG
      try
      {
#endif
        this.config.LoadConfiguration();
        this.userRoomService.LoadPermanentRooms();

        return this.dispatcher.Output(domain, Strings.CONFIGURATION_FILE_RELOADED);
#if !DEBUG
      }
      catch (Configuration.LoadException e)
      {
        Log.Warning("Failed to reload config (stage {stage}): {exception}", e.Stage, e.UnderlyingException);
        return this.dispatcher.Error(domain, Strings.FAILED_TO_RELOAD_CONFIGURATION + e);
      }
#endif
    }
  }
}
