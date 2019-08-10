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
  internal class GroupJoin: ARequest
  {
    private GroupService groupService = DI.Auto<GroupService>();

    private int identifier;
    private Group group;

    internal static GroupJoin Parse(Message ev)
    {
      return new GroupJoin
      {
        identifier =  Convert.ToInt32(ev.data.ToString())
      };
    }

    internal override bool Validate(PlayerHandle sender, ChatManager server)
    {
      if (!base.Validate(sender, server))
      {
        return false;
      }

      lock(server)
      {
        this.group = server.World.FindGroup(identifier);
      }

      return this.group != null;
    }

    internal override IPromise<Nothing> Process(PlayerHandle sender, ChatManager server)
    {
      return this.groupService.JoinGroup(sender, this.group);
    }
  }
}
