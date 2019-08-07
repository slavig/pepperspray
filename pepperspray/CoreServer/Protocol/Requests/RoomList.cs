﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RSG;
using pepperspray.CIO;
using pepperspray.CoreServer.Game;
using pepperspray.CoreServer.Services;
using pepperspray.SharedServices;

namespace pepperspray.CoreServer.Protocol.Requests
{
  internal class RoomList: ARequest
  {
    private UserRoomService userRoomService = DI.Auto<UserRoomService>();

    internal static RoomList Parse(Message ev)
    {
      return new RoomList();
    }

    internal override IPromise<Nothing> Process(PlayerHandle sender, CoreServer server)
    {
      return this.userRoomService.ListRooms(sender);
    }
  }
}
