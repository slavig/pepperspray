using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RSG;
using Serilog;
using pepperspray.CIO;
using pepperspray.ChatServer.Game;
using pepperspray.SharedServices;
using pepperspray.ChatServer.Services;

namespace pepperspray.ChatServer.Protocol.Requests
{
  internal class GroupLeave: ARequest
  {
    private GroupService groupService = DI.Auto<GroupService>();

    internal static GroupLeave Parse(Message ev)
    {
      return new GroupLeave {};
    }

    internal override IPromise<Nothing> Process(PlayerHandle sender, ChatManager server)
    {
      return this.groupService.LeaveGroup(sender);
    }
  }
}
