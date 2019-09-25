using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using Serilog;
using RSG;
using pepperspray.CIO;
using pepperspray.ChatServer.Game;
using pepperspray.ChatServer.Protocol;
using pepperspray.SharedServices;
using pepperspray.Utils;
using pepperspray.Resources;

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

    internal IPromise<Nothing> QueueMessage(PlayerHandle sender, string recipientName, string messageText)
    {
      try
      {
        Log.Debug("Offline message queue: from {sender} to {recipientName}", sender.Digest, recipientName);
        var recipient = this.characterService.Find(recipientName);
        var dateString = DateTime.Now.ToShortDateString();
        var timeString = DateTime.Now.ToShortTimeString();

        var message = new OfflineMessage
        {
          SenderId = sender.Id,
          RecepientId = recipient.Id,
          Message = String.Format("{0} {1}: {2}", dateString, timeString, messageText),
        };

        this.db.Write((c) => c.OfflineMessageInsert(message));
        return sender.Stream.Write(Responses.ServerPrivateChatMessage(
          recipient.Name, 
          recipient.Id, 
          Strings.PLAYER_IS_OFFLINE_MESSAGES_WILL_BE_DELIVERED
          ));
      }
      catch (Database.NotFoundException) 
      {
        Log.Warning("Failed to queue offline message from {sender} - {recipientName} not found!", sender.Digest, recipientName);
        return Nothing.Resolved();
      }
    }

    internal IEnumerable<OfflineMessage> PopMessages(string token, uint recipientId)
    {
      var character = this.characterService.FindAndAuthorize(token, recipientId);

      List<OfflineMessage> messages = this.db.Read((c) => c.OfflineMessageFind(character.Id).ToList());
      this.db.Write((c) => c.OfflineMessageDelete(character.Id));

      return messages;
    }

    internal Dictionary<uint, bool> CheckMessages(string token)
    {
      var user = this.loginService.AuthorizeUser(token);
      IEnumerable<Character> characters = this.db.Read((c) => c.CharactersFindByUser(user));

      var result = new Dictionary<uint, bool>();
      foreach (var character in characters)
      {
        lock(this.db)
        {
          result[character.Id] = this.db.Read((c) => c.OfflineMessageFind(character.Id).Any());
        }
      }

      return result;
    }
  }
}
