using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RSG;
using Serilog;
using pepperspray.CIO;
using pepperspray.ChatServer.Game;
using pepperspray.ChatServer.Protocol;
using pepperspray.Utils;
using pepperspray.SharedServices;

namespace pepperspray.ChatServer.Services
{
  internal class GroupService
  {
    private ChatManager server = DI.Get<ChatManager>();
    private Random random = DI.Auto<Random>();

    internal void PlayerLoggedIn(PlayerHandle sender)
    {
      this.ResetGroup(sender);
      this.JoinGroup(sender, sender.CurrentGroup, true);
    }

    internal void PlayerLoggedOff(PlayerHandle sender)
    {
      if (sender.CurrentGroup != null)
      {
        this.LeaveGroup(sender);
      }
    }

    internal IPromise<Nothing> JoinGroup(PlayerHandle player, Group group, bool onLogin = false)
    {
      List<IPromise<Nothing>> promises = new List<IPromise<Nothing>>();
      lock(this.server)
      {
        if (player.CurrentGroup != null)
        {
          promises.Add(this.LeaveGroup(player));
        }

        promises.AddRange(group.Players.Select(a => a.Stream.Write(Responses.GroupAdd(player))));
        player.CurrentGroup = group;
        group.AddPlayer(player);
      }

      promises.Add(player.Stream.Write(Responses.MyGroup(group)));
      if (onLogin == false)
      {
        promises.Add(player.Stream.Write(Responses.GroupList(group.Players)));
      }
      return new CombinedPromise<Nothing>(promises);
    }

    internal IPromise<Nothing> LeaveGroup(PlayerHandle player)
    {
      List<IPromise<Nothing>> promises = new List<IPromise<Nothing>>();
      var group = player.CurrentGroup;

      this.ResetGroup(player);
      promises.Add(player.Stream.Write(Responses.MyGroup(player.CurrentGroup)));

      lock (this.server)
      {
        group.RemovePlayer(player);
        promises.AddRange(group.Players.Select(a => a.Stream.Write(Responses.GroupLeave(player))));
      }

      return new CombinedPromise<Nothing>(promises);
    }

    internal void ResetGroup(PlayerHandle sender)
    {
      var group = new Group(this.NextGroupIdentifier());
      group.AddPlayer(sender);

      sender.CurrentGroup = group;
    }

    internal int NextGroupIdentifier()
    {
      return random.Next();
    }
  }
}
