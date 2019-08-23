﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using Serilog;
using pepperspray.SharedServices;
using pepperspray.Utils;

namespace pepperspray.SharedServices
{
  internal class OfflineMessageService: IDIService
  {
    private Database db;
    private CharacterService characterService;
    private LoginService loginService;

    public void Inject()
    {
      this.db = DI.Get<Database>();
      this.characterService = DI.Get<CharacterService>();
      this.loginService = DI.Get<LoginService>();
    }

    internal void QueueMessage(uint senderId, string recepientName, string messageText)
    {
      try
      {
        var recepient = this.characterService.Find(recepientName);
        var message = new OfflineMessage
        {
          SenderId = senderId,
          RecepientId = recepient.Id,
          Message = messageText
        };

        lock (this.db)
        {
          this.db.OfflineMessageInsert(message);
        }
      }
      catch (Database.NotFoundException) 
      {
        Log.Warning("Failed to queue offline message from {senderId} - {recepientName} not found!", senderId, recepientName);
      }
    }

    internal IEnumerable<OfflineMessage> PopMessages(string token, uint recepientId)
    {
      var character = this.characterService.FindAndAuthorize(token, recepientId);

      List<OfflineMessage> messages;
      lock (this.db)
      {
        messages = this.db.OfflineMessageFind(character.Id).ToList();
        this.db.OfflineMessageDelete(character.Id);
      }

      return messages;
    }

    internal Dictionary<uint, bool> CheckMessages(string token)
    {
      var user = this.loginService.AuthorizeUser(token);
      IEnumerable<Character> characters;
      lock(this.db)
      {
        characters = this.db.CharactersFindByUser(user);
      }

      var result = new Dictionary<uint, bool>();
      foreach (var character in characters)
      {
        lock(this.db)
        {
          result[character.Id] = this.db.OfflineMessageFind(character.Id).Any();
        }
      }

      return result;
    }
  }
}