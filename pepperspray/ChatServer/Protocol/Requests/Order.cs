using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RSG;
using Serilog;
using pepperspray.CIO;
using pepperspray.ChatServer.Game;
using pepperspray.ChatServer.Services;
using pepperspray.SharedServices;

namespace pepperspray.ChatServer.Protocol.Requests
{
  internal class Order: ARequest
  {
    private UserRoom userRoom;

    private UserRoomService userRoomService = DI.Get<UserRoomService>();
    private GiftsService giftService = DI.Get<GiftsService>();

    internal static Order Parse(Message ev)
    {
      return new Order();
    }

    internal override bool Validate(PlayerHandle sender, ChatManager server)
    {
      if (!base.Validate(sender, server))
      {
        return false;
      }

      if (sender.User.Currency < 300)
      {
        return false;
      }

      lock (server)
      {
        this.userRoom = server.World.FindUserRoom(sender);
      }

      return this.userRoom != null && this.userRoom.IsPrioritized == false;
    }

    internal override IPromise<Nothing> Process(PlayerHandle sender, ChatManager server)
    {
      try
      {
        this.giftService.ChangeCurrency(sender.User, -300);
        this.userRoom.IsPrioritized = true;
        Log.Information("User {username} prioritized room {identifier}", sender.User.Username, this.userRoom.Identifier);
        return sender.Stream.Write(Responses.OrderOk());

      }
      catch (GiftsService.NotEnoughCurrencyException)
      {
        Log.Warning("Failed to prioritize {identifier} room - not enough currency on {user}", this.userRoom.Identifier, sender.User.Username);
      }

      return Nothing.Resolved();
    }
  }
}
