using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

using Serilog;
using Newtonsoft.Json;
using pepperspray.LoginServer;
using pepperspray.Utils;

namespace pepperspray.SharedServices
{
  internal class CharacterService: IDIService
  {
    internal class NameTakenException: Exception { }
    internal class InvalidNameException: Exception { }

    internal class NotFoundException: Exception { }
    internal class NotAuthorizedException: Exception { }

    internal static string characterPresetsDirectoryPath = Path.Combine("peppersprayData", "presets");
    private Dictionary<uint, Character> loggedCharacters = new Dictionary<uint, Character>();

    private Configuration config;
    private NameValidator nameValidator;
    private Database db;
    private LoginServerListener loginServer;
    private ChatActionsAuthenticator actionsAuthenticator;

    public void Inject()
    {
      this.config = DI.Get<Configuration>();
      this.nameValidator = DI.Get<NameValidator>();
      this.db = DI.Get<Database>();
      this.loginServer = DI.Get<LoginServerListener>();
      this.actionsAuthenticator = DI.Get<ChatActionsAuthenticator>();
    }

    internal Character LoginCharacter(User user, uint id, string name, string sex)
    {
      lock(this.db)
      {
        var character = this.FindAndAuthorize(user.Token, id);
        if (!character.Name.Equals(name) || !character.Sex.Equals(sex))
        {
          throw new InvalidNameException();
        }

        character.LastLogin = DateTime.Now;
        this.db.CharacterUpdate(character);
        lock(this)
        {
          this.loggedCharacters[id] = character;
        }

        return character;
      }
    }

    internal void LogoutCharacter(Character character)
    {
      lock(this)
      {
        this.loggedCharacters.Remove(character.Id);
      }
    }

    internal void CheckName(string name)
    {
      if (!this.nameValidator.Validate(name))
      {
        throw new InvalidNameException();
      }

      try
      {
        lock(this.db)
        {
          this.db.CharacterFindByName(name);
        }
        throw new NameTakenException();
      }
      catch (Database.NotFoundException) { }
    }

    internal Character CreateCharacter(string name, string sex, string token)
    {
      Log.Debug("Client {token} creating character {name}/{sex}", token, name, sex);

      User user = null;
      try
      {
        lock (this.db)
        {
          user = this.db.UserFindByToken(token);
        }
      }
      catch (Database.NotFoundException)
      {
        throw new NotAuthorizedException();
      }

      var character = new Character
      {
        UserId = user.Id,
        Name = name,
        Sex = sex,
        Appearance = this.GetDefaultAppearance(sex),
        AvatarSlot = "0",
        ProfileJSON = JsonConvert.SerializeObject(new Dictionary<string, object>
        {
          { "age", 18 },
          { "interest", "?" },
          { "location", "Unknown" },
          { "about", "Empty" },
        })
      };

      lock(this.db)
      {
        this.db.CharacterInsert(character);
      }

      return character;
    }

    internal void UpdateCharacter(uint id, string token, string newName, string newSex)
    {
      Log.Debug("Client {token} updating character {uid} - new {name}/{sex}", token, id, newName, newSex);

      lock(this.db)
      {
        var character = this.FindAndAuthorize(token, id);
        character.Name = newName;
        character.Sex = newSex;

        this.db.CharacterUpdate(character);
      }
    }

    internal void UpdateCharacterAppearance(string token, uint id, string data)
    {
      Log.Debug("Client {token} updating character appearance of {uid}", token, id);

      lock(this.db)
      {
        var character = this.FindAndAuthorize(token, id);
        character.Appearance = data;

        this.db.CharacterUpdate(character);
      }
    }

    internal void SetCharacterSpouse(string token, uint id, uint spouseId)
    {
      Log.Debug("Client {token} updating character {id} spouse to {spouseId}", token, id, spouseId);

      var character = this.FindAndAuthorize(token, id);
      var spouse = this.Find(spouseId);

      if (spouse.SpouseId != 0)
      {
        Log.Warning("Client {token} from {id} were unable to update spouse of {spouseId} - married to another character!", token, id, spouseId);
        throw new NotAuthorizedException();
      }

      if (!this.actionsAuthenticator.AuthenticateAndFullfillMarryAgreement(character.Name, spouse.Name))
      {
        Log.Warning("Client {token} from {id} failed to set character spouse of {spouseId} - not authenticated!", token, id, spouseId);
        throw new NotAuthorizedException();
      }

      spouse.SpouseId = character.Id;
      character.SpouseId = spouseId;

      lock (this.db)
      {
        this.db.CharacterUpdate(character);
        this.db.CharacterUpdate(spouse);
      }
    }

    internal void UnsetCharacterSpouse(string token, uint id)
    {
      Log.Debug("Client {token} removing character {id} spouse", token, id);

      var character = this.FindAndAuthorize(token, id);

      try
      {
        var spouse = this.Find(character.SpouseId);
        if (spouse.SpouseId == character.Id)
        {
          spouse.SpouseId = 0;
          lock (this.db)
          {
            this.db.CharacterUpdate(spouse);
          }
        }
        else
        {
          Log.Warning("Client {token} of {id} failed to unset spouse {spouseId} - not married!", token, id, spouse.Id);
        }
      }
      catch (NotFoundException)
      {
        Log.Warning("Client {token} of {id} - failed to unset spouse - spouse not found!", token, id);
      }

      character.SpouseId = 0;
      lock(this.db)
      {
        this.db.CharacterUpdate(character);
      }
    }

    internal void DeleteCharacter(string token, uint id, string name)
    {
      Log.Debug("Client {token} deleting character {uid}", token, id);

      lock(this.db)
      {
        var character = this.FindAndAuthorize(token, id);
        this.db.CharacterDeleteById(character.Id);
      }
    }

    internal string GetCharacterProfile(uint id)
    {
      Log.Debug("Client requesting chracter profile of {uid}", id);

      Character character = null;
      Character spouse = null;
      lock (this.db)
      {
        character = this.Find(id);

        if (character.SpouseId != 0)
        {
          try
          {
            spouse = this.Find(character.SpouseId);
          }
          catch (NotFoundException) { }
        }
      }

      return JsonConvert.SerializeObject(new Dictionary<string, object> {
        { "id", character.Id },
        { "name", character.Name },
        { "sex", character.Sex },
        { "profile", character.ProfileJSON },
        { "gifts", this.getGiftCount(character.Id) },
        { "married", spouse == null 
          ? new Dictionary<string, object> { { "id", 0 } } 
          : new Dictionary<string, object> { { "id", spouse.Id }, { "name", spouse.Name }, {"sex", spouse.Sex } } },
        { "ava", character.AvatarSlot ?? "0" },
        { "photos", this.config.PlayerPhotoSlots },
        { "photoSlots", this.getPhotos(character.Id) }
      });
    }

    internal void UpdateCharacterProfile(string token, uint id, string json)
    {
      Log.Debug("Client {token} updating character profile of {uid}", token, id);

      lock (this.db)
      {
        var character = this.FindAndAuthorize(token, id);
        character.ProfileJSON = json;

        this.db.CharacterUpdate(character);
      }
    }

    internal string GetDefaultAppearance(string sex)
    {
      string path = null;
      if (sex.Equals("m"))
      {
        path = "defaultMale.base64";
      } else if (sex.Equals("f"))
      {
        path = "defaultFemale.base64";
      } else
      {
        throw new ArgumentException();
      }

      return File.ReadAllText(Path.Combine(CharacterService.characterPresetsDirectoryPath, path));
    }

    internal string GetBotAppearance(string sex)
    {
      string path = null;
      if (sex.Equals("m"))
      {
        path = "botMale.base64";
      } else if (sex.Equals("f"))
      {
        path = "botFemale.base64";
      } else
      {
        throw new ArgumentException();
      }

      return File.ReadAllText(Path.Combine(CharacterService.characterPresetsDirectoryPath, path));
    }

    internal Character Find(uint id)
    {
      Character character;
      lock (this)
      {
        if (!this.loggedCharacters.TryGetValue(id, out character))
        {
          try
          {
            lock (this.db)
            {
              character = this.db.CharacterFindById(id);
            }
          }
          catch (Database.NotFoundException)
          {
            throw new NotFoundException();
          }
        }
      }

      return character;
    }

    internal Character Find(string name)
    {
      lock(this)
      {
        var matching = this.loggedCharacters.Where(c => c.Value.Name.Equals(name));

        if (matching.Count() == 0)
        {
          try
          {
            lock (this.db)
            {
              return this.db.CharacterFindByName(name);
            }
          }
          catch (Database.NotFoundException)
          {
            throw new NotFoundException();
          }
        }
        else
        {
          return matching.First().Value;
        }
      }
    }

    internal Character FindAndAuthorize(string token, uint id)
    {
      try
      {
        User user = null;
        lock (this.db)
        {
          user = this.db.UserFindByToken(token);
        }

        var character = this.Find(id);

        if (character.UserId != user.Id)
        {
          throw new NotAuthorizedException();
        }

        return character;
      }
      catch (Database.NotFoundException)
      {
        throw new NotFoundException();
      }
    }

    private Dictionary<string, string> getPhotos(uint id)
    {
      var result = new Dictionary<string, string>();
      IEnumerable<PhotoSlot> slots = null;
      lock (this.db)
      {
        slots = this.db.PhotoSlotFindByCharacterId(id);
      }

      foreach (var slot in slots)
      {
        result[slot.Identifier] = slot.Hash;
      }

      return result;
    }

    private uint getGiftCount(uint id)
    {
      lock(this.db)
      {
        return this.db.GiftsCount(id);
      }
    }
  }
}
