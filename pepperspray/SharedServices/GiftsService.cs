using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Serilog;
using pepperspray.LoginServer;

namespace pepperspray.SharedServices
{
  internal class GiftsService: IDIService
  {
    internal class NotFoundException : Exception { }
    internal class NotEnoughCurrencyException : Exception { }

    private Configuration config;
    private LoginServerListener loginServer;
    private LoginService loginService;
    private CharacterService characterService;
    private Database db;

    private uint PageCount = 10;

    public void Inject()
    {
      this.config = DI.Get<Configuration>();
      this.loginServer = DI.Get<LoginServerListener>();
      this.loginService = DI.Get<LoginService>();
      this.characterService = DI.Get<CharacterService>();
      this.db = DI.Get<Database>();
    }

    internal List<Dictionary<string, object>> GetGifts(uint id, uint offset)
    {
      var result = new List<Dictionary<string, object>>();
      IEnumerable<Gift> gifts = null;
      lock(this.db)
      {
        gifts = this.db.GiftsFind(id, offset, this.PageCount);
      }

      foreach (var gift in gifts)
      {
        Character sender = null;
        try
        {
          sender = this.characterService.Find(gift.SenderId);
        }
        catch (CharacterService.NotFoundException)
        {
          continue;
        }

        result.Add(new Dictionary<string, object>
        {
          { "id", String.Format("{0}{1}", this.giftIdPrefix(), gift.Id) },
          { "from", new Dictionary<string, object>
            {
              { "id", sender.Id },
              { "name", sender.Name },
              { "sex", sender.Sex }
            }
          },
          { "pic", gift.Identifier },
          { "date", gift.Date },
          { "message", gift.Message },
        });
      }

      return result;
    }

    internal void SendGift(string token, uint senderId, uint recepientId, string giftIdentifier, string message)
    {
      Log.Debug("Client {token} of {sender} sending gift to {recepient}: {gift_idetnfier} {message}", token, senderId, recepientId, giftIdentifier, message);

      try
      {
        var character = this.characterService.FindAndAuthorize(token, senderId);

        if (this.config.Currency.Enabled)
        {
          var user = this.loginService.AuthorizeUser(token);
          
          if (user.Currency < 300)
          {
            throw new NotEnoughCurrencyException();
          }

          user.Currency -= 300;
          lock(this.db)
          {
            this.db.UserUpdate(user);
          }
        }

        var gift = new Gift
        {
          SenderId = senderId,
          RecepientId = recepientId,
          Identifier = giftIdentifier,
          Message = message,
          Date = DateTime.Now
        };

        lock(this.db)
        {
          this.db.GiftInsert(gift);
        }

        try
        {
          this.loginServer.Emit(recepientId, "gift", new Dictionary<string, string>
          {
            { "for", recepientId.ToString() },
          });
        } catch (LoginServerListener.NotFoundException) {
          Log.Debug("GiftsService failed to send gift notification to {recepient} - login server connection not found", recepientId);
        }
      }
      catch (Database.NotFoundException)
      {
        Log.Warning("Client {token} from {sender}: failed to send gift - not found", token, senderId);
        throw new NotFoundException();
      }
    }

    internal void DeleteGift(string token, uint characterId, string giftIdentifier)
    {
      Log.Debug("Client {token} of {sender} deleting gift {id}", token, characterId, giftIdentifier);

      try
      {
        var giftId = Convert.ToUInt32(giftIdentifier.Substring(this.giftIdPrefix().Length));
        var character = this.characterService.FindAndAuthorize(token, characterId);
        Gift gift = null;

        lock (this.db)
        {
          gift = this.db.GiftFindById(giftId);
        }

        if (gift.RecepientId != character.Id)
        {
          throw new NotFoundException();
        }

        lock(this.db)
        {
          this.db.GiftDelete(gift);
        }
      }
      catch (Database.NotFoundException)
      {
        Log.Warning("Client {token} from {sender}: failed to send gift - {id} not found", token, characterId, giftIdentifier);
        throw new NotFoundException();
      }
    }

    internal void ChangeCurrency(User user, int amount)
    {
      Log.Information("Changing currency amount of user {username} by {amount}", user.Username, amount);

      var current = (int)user.Currency;
      user.Currency = (current + amount) > 0 ? (uint)(current + amount) : 0;

      lock(this.db)
      {
        this.db.UserUpdate(user);
      }
    }

    internal void TransferCurrency(User sender, User recepient, uint amount)
    {
      Log.Information("User {sender} transferring currency to {recepient} in amount of {amount}", sender.Username, recepient.Username, amount);

      if (sender.Currency < amount)
      {
        throw new NotEnoughCurrencyException();
      }

      sender.Currency -= amount;
      recepient.Currency += amount;

      lock (this.db)
      {
        this.db.UserUpdate(sender);
        this.db.UserUpdate(recepient);
      }
    }

    private string giftIdPrefix()
    {
      return "gift_";
    }
  }
}
