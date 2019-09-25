using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Serilog;
using pepperspray.ChatServer.Game;
using pepperspray.ChatServer.Services.Events;
using pepperspray.LoginServer;
using pepperspray.Resources;

namespace pepperspray.SharedServices
{
  internal class GiftsService: IDIService, PlayerLoggedOffEvent.IListener
  {
    internal class NotFoundException : Exception { }
    internal class NotEnoughCurrencyException : Exception { }
    internal class SlotsAmountExceeded : Exception { }

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
      IEnumerable<Gift> gifts = this.db.Read((c) => c.GiftsFind(id, offset, this.PageCount));

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

    internal void SendGift(string token, uint senderId, uint recipientId, string giftIdentifier, string message)
    {
      Log.Debug("Client {token} of {sender} sending gift to {recipient}: {gift_idetnfier} {message}", token, senderId, recipientId, giftIdentifier, message);

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
          this.db.Write((c) => c.UserUpdate(user));
        }

        var gift = new Gift
        {
          SenderId = senderId,
          RecepientId = recipientId,
          Identifier = giftIdentifier,
          Message = message,
          Date = DateTime.Now
        };

        this.db.Write((c) => c.GiftInsert(gift));

        try
        {
          this.loginServer.Emit(recipientId, "gift", new Dictionary<string, string>
          {
            { "for", recipientId.ToString() },
          });
        } catch (LoginServerListener.NotFoundException) {
          Log.Debug("GiftsService failed to send gift notification to {recipient} - login server connection not found", recipientId);
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
        Gift gift = this.db.Read((c) => c.GiftFindById(giftId));

        if (gift.RecepientId != character.Id)
        {
          throw new NotFoundException();
        }

        this.db.Write((c) => c.GiftDelete(gift));
      }
      catch (Database.NotFoundException)
      {
        Log.Warning("Client {token} from {sender}: failed to send gift - {id} not found", token, characterId, giftIdentifier);
        throw new NotFoundException();
      }
    }

    internal void BuySlot(string token, uint fromId, uint toId)
    {
      Log.Debug("Client {token} of {sender} buying slot for {id}", token, fromId, toId);

      try
      {
        var senderCharacter = this.characterService.FindAndAuthorize(token, fromId);
        var recipientCharacter = this.characterService.Find(toId);

        if (recipientCharacter.NumberOfSlots + 1 > this.config.PlayerMaxPhotoSlots)
        {
          try
          {
            this.loginServer.Emit(token, "alert", String.Format(Strings.PHOTO_SLOTS_NUMBER_EXCEEDED, this.config.PlayerMaxPhotoSlots));
          }
          catch (LoginServerListener.NotFoundException) {
            Log.Warning("Failed to notify {token} about failed buy slot operation - not found on login server!", token);
          }

          throw new SlotsAmountExceeded();
        }

        var senderUser = this.db.Read((c) => c.UserFind(senderCharacter.UserId));
        var price = this.nextSlotPrice(recipientCharacter.NumberOfSlots);
        if (senderUser.Currency < price)
        {
          try
          {
            this.loginServer.Emit(token, "alert", String.Format(Strings.NOT_ENOUGH_COINS_REQUIRED, price, senderUser.Currency));
          }
          catch (LoginServerListener.NotFoundException) {
            Log.Warning("Failed to notify {token} about failed buy slot operation - not found on login server!", token);
          }

          throw new NotEnoughCurrencyException();
        }
        else
        {
          senderUser.Currency -= price;
          recipientCharacter.NumberOfSlots += 1;

          this.db.Write((c) => {
            c.UserUpdate(senderUser);
            c.CharacterUpdate(recipientCharacter);
          });
        }
      }
      catch (Database.NotFoundException)
      {
        Log.Warning("Client {token} from {sender}: failed to send gift - {id} not found", token, fromId, toId);
        throw new NotFoundException();
      }
    }

    internal void ChangeCurrency(User user_, int amount)
    {
      Log.Information("Changing currency amount of user {username} by {amount}", user_.Username, amount);

      User user = this.db.Read((c) => c.UserFind(user_.Username));

      var current = (int)user.Currency;
      if (current + amount < 0)
      {
        throw new NotEnoughCurrencyException();
      }

      user.Currency = (uint)(current + amount);

      this.db.Write((c) => c.UserUpdate(user));
    }

    internal void TransferCurrency(User sender_, User recipient_, uint amount)
    {
      if (sender_.Id.Equals(recipient_.Id))
      {
        throw new ArgumentException();
      }

      User sender = this.db.Read((c) => c.UserFind(sender_.Username));
      User recipient = this.db.Read((c) => c.UserFind(recipient_.Username));
      Log.Information("User {sender} transferring currency to {recipient} in amount of {amount}", sender.Username, recipient.Username, amount);

      if (sender.Currency < amount)
      {
        throw new NotEnoughCurrencyException();
      }

      sender.Currency -= amount;
      recipient.Currency += amount;

      this.db.Write((c) =>
      {
        c.UserUpdate(sender);
        c.UserUpdate(recipient);
      });
    }

    internal uint GetCurrency(User user_)
    {
      return this.db.Read((c) => c.UserFind(user_.Username)).Currency;
    }

    internal uint GetCurrency(uint id)
    {
      return this.db.Read((c) => c.UserFind(id)).Currency;
    }

    internal uint GrantOnlineBonus(User user_, TimeSpan timeSpan)
    {
      if (this.config.Currency.Enabled && this.config.Currency.BonusPerHourOnline != 0)
      {
        User user = this.db.Read((c) => c.UserFind(user_.Username));

        var delta = (uint)Math.Floor(timeSpan.TotalHours * this.config.Currency.BonusPerHourOnline);

        if (this.config.Currency.SoftCap != -1 && user.Currency + delta > this.config.Currency.SoftCap)
        {
          user.Currency = Math.Max(Convert.ToUInt32(this.config.Currency.SoftCap), user.Currency);
          Log.Information("Currency bonus for {user} has not been granted, amount is at soft cap", user.Username);
        }
        else
        {
          user.Currency += delta;
          Log.Information("Granting online currency bonus of {delta} for {user}, has been online for {timeSpan}", delta, user.Username, timeSpan);
        }

        this.db.Write((c) => c.UserUpdate(user));

        return delta;
      }
      else
      {
        throw new InvalidOperationException();
      }
    }

    public void PlayerLoggedOff(PlayerLoggedOffEvent ev)
    {
      if (this.config.Currency.Enabled)
      {
        try
        {
          this.GrantOnlineBonus(ev.Handle.User, DateTime.Now -ev.Handle.LoggedAt);
        } catch (Database.NotFoundException)
        {
          Log.Warning("Failed to grant bonus - user {username} not found in the db!", ev.Handle.User.Username);
        }
      }
    }

    private string giftIdPrefix()
    {
      return "gift_";
    }

    private uint nextSlotPrice(uint number)
    {
      return (uint)(150 * Math.Pow(2, number - 1));
    }
  }
}
