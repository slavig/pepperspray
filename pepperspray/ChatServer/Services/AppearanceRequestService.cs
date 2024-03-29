﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RSG;
using Serilog;
using pepperspray.CIO;
using pepperspray.ChatServer.Game;
using pepperspray.ChatServer.Protocol;
using pepperspray.SharedServices;

namespace pepperspray.ChatServer.Services
{
  internal class AppearanceRequestService: IDIService
  {
    public void Inject()
    {

    }

    internal bool ShouldDispatch(string text)
    {
      return text.StartsWith("~action2/givemeCharData|");
    }

    internal IPromise<Nothing> Dispatch(PlayerHandle sender, IEnumerable<PlayerHandle> recipients, ChatManager manager, string text)
    {
      if (recipients.Count() < 1)
      {
        Log.Debug("Failed to dispatch message from {sender} - no recipients", sender.Digest);
        return Nothing.Resolved();
      }

      return sender.Stream.Write(Responses.CharacterAppearance(recipients.First()));
    }
  }
}
