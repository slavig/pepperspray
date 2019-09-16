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
using System.Text.RegularExpressions;

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
      Log.Debug("Logging in character {id}/{name}/{sex} of {user}", id, name, sex, user.Username);
      var character = this.FindAndAuthorize(user.Token, id);
      if (!character.Name.Equals(name) || !character.Sex.Equals(sex))
      {
        Log.Verbose("LoginCharacter FindAndAuthorize invalid name");
        throw new InvalidNameException();
      }

      character.LastLogin = DateTime.Now;
      this.db.Write((c) => c.CharacterUpdate(character));

      lock (this)
      {
        this.loggedCharacters[id] = character;
      }

      return character;
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
        var character = this.db.Read((c) => c.CharacterFindByNameIgnoreCase(name));
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
        user = this.db.Read((c) => c.UserFindByToken(token));
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

      this.db.Write((c) => c.CharacterInsert(character));
      return character;
    }

    internal void UpdateCharacter(uint id, string token, string newName, string newSex)
    {
      Log.Debug("Client {token} updating character {uid} - new {name}/{sex}", token, id, newName, newSex);

      var character = this.FindAndAuthorize(token, id);
      if (character.Name.ToLower() != newName.ToLower())
      {
        this.CheckName(newName);
      }

      character.Name = newName;
      character.Sex = newSex;

      this.db.Write((c) => c.CharacterUpdate(character));
    }

    internal void UpdateCharacterAppearance(string token, uint id, string data)
    {
      Log.Debug("Client {token} updating character appearance of {uid}", token, id);

      var character = this.FindAndAuthorize(token, id);
      if (!character.Appearance.Equals(data))
      {
        character.Appearance = data;
        this.db.Write((c) => c.CharacterUpdate(character));
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

      this.db.Write((c) =>
      {
        c.CharacterUpdate(character);
        c.CharacterUpdate(spouse);
      });
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
          this.db.Write((c) => c.CharacterUpdate(spouse));
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
      this.db.Write((c) => c.CharacterUpdate(character));
    }

    internal void DeleteCharacter(string token, uint id, string name)
    {
      Log.Debug("Client {token} deleting character {uid}", token, id);

      var character = this.FindAndAuthorize(token, id);
      this.db.Write((c) => c.CharacterDeleteById(character.Id));
    }

    internal string GetCharacterProfile(uint id)
    {
      Log.Debug("Client requesting chracter profile of {uid}", id);

      Character character = this.Find(id);
      Character spouse = null;

      if (character.SpouseId != 0)
      {
        try
        {
          spouse = this.Find(character.SpouseId);
        }
        catch (NotFoundException) { }
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

      var character = this.FindAndAuthorize(token, id);
      character.ProfileJSON = json;

      this.db.Write((c) => c.CharacterUpdate(character));
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
            character = this.db.Read((c) => c.CharacterFindById(id));
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
            return this.db.Read((c) => c.CharacterFindByName(name));
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
        User user = this.db.Read((c) => c.UserFindByToken(token));
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

    internal static string StripCharacterName(string name)
    {
      return Regex.Replace(name, "<.*?>", String.Empty);
    }

    private Dictionary<string, string> getPhotos(uint id)
    {
      var result = new Dictionary<string, string>();
      IEnumerable<PhotoSlot> slots = this.db.Read((c) => c.PhotoSlotFindByCharacterId(id));

      foreach (var slot in slots)
      {
        result[slot.Identifier] = slot.Hash;
      }

      return result;
    }

    private uint getGiftCount(uint id)
    {
      return this.db.Read((c) => c.GiftsCount(id));
    }
  }
}
