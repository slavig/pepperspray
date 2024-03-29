﻿using System;
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
  internal class FriendRequest: ARequest
  {
    private uint recipientId;
    private PlayerHandle recipient;

    internal static FriendRequest Parse(Message ev)
    {
      if (ev.data is string == false)
      {
        return null;
      }

      uint id;
      try
      {
        id = Convert.ToUInt32(ev.data as string);
      }
      catch (FormatException)
      {
        return null;
      }

      return new FriendRequest
      {
        recipientId = id
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
        this.recipient = server.World.FindPlayerById(this.recipientId);
      }

      if (this.recipient == null)
      {
        return false;
      }

      return true;
    }

    internal override IPromise<Nothing> Process(PlayerHandle sender, ChatManager server)
    {
      return this.recipient.Stream.Write(Responses.Friend(sender, this.recipient));
    }
  }
}
