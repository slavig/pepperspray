using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RSG;
using pepperspray.CIO;
using pepperspray.ChatServer.Game;
using pepperspray.ChatServer.Services;
using pepperspray.SharedServices;

namespace pepperspray.ChatServer.Protocol.Requests
{
  internal class GetOnline: ARequest
  {
    private FriendsService friendsService = DI.Get<FriendsService>();

    internal static GetOnline Parse(Message ev)
    {
      return new GetOnline();
    }

    internal override IPromise<Nothing> Process(PlayerHandle sender, ChatManager server)
    {
      var users = this.friendsService.GetFriendIDs(sender.Character).Where(ch => server.World.FindPlayerById(ch) != null);
      return sender.Stream.Write(Responses.OnlineUsers(users));
    }
  }
}
