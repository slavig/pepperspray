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

    public void Inject()
    {
      this.config = DI.Get<Configuration>();
      this.nameValidator = DI.Get<NameValidator>();
      this.db = DI.Get<Database>();
      this.loginServer = DI.Get<LoginServerListener>();
    }

    internal Character LoginCharacter(User user, uint id, string name, string sex)
    {
      try
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
      catch (Database.NotFoundException)
      {
        throw new NotFoundException();
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

      try
      {
        lock(this.db)
        {
          var character = this.FindAndAuthorize(token, id);
          character.Name = newName;
          character.Sex = newSex;

          this.db.CharacterUpdate(character);
        }
      }
      catch (Database.NotFoundException)
      {
        Log.Warning("Client {token} failed to update character - not found");
        throw new NotFoundException();
      }
    }

    internal void UpdateCharacterAppearance(string token, uint id, string data)
    {
      Log.Debug("Client {token} updating character appearance of {uid}", token, id);

      try
      {
        lock(this.db)
        {
          var character = this.FindAndAuthorize(token, id);
          character.Appearance = data;

          this.db.CharacterUpdate(character);
        }
      }
      catch (Database.NotFoundException)
      {
        Log.Warning("Client {token} failed to update character appearance - not found");
        throw new NotFoundException();
      }
    }

    internal void DeleteCharacter(string token, uint id, string name)
    {
      Log.Debug("Client {token} deleting character {uid}", token, id);

      try
      {
        lock(this.db)
        {
          var character = this.FindAndAuthorize(token, id);
          this.db.CharacterDeleteById(character.Id);
        }
      }
      catch (Database.NotFoundException)
      {
        Log.Warning("Client {token} failed to delete character - not found");
        throw new NotFoundException();
      }
    }

    internal string GetCharacterProfile(uint id)
    {
      Log.Debug("Client requesting chracter profile of {uid}", id);

      try
      {
        Character character = null;
        lock (this.db)
        {
          character = this.Find(id);
        }

        return JsonConvert.SerializeObject(new Dictionary<string, object> {
          { "id", character.Id },
          { "name", character.Name },
          { "sex", character.Sex },
          { "profile", character.ProfileJSON },
          { "gifts", this.getGiftCount(character.Id) },
          { "married", new Dictionary<string, object> { { "id", 0 }, { "name", null }, {"sex", null } } },
          { "ava", character.AvatarSlot != null ? character.AvatarSlot : "0" },
          { "photos", this.config.PlayerPhotoSlots },
          { "photoSlots", this.getPhotos(character.Id) }
        });
      }
      catch (Database.NotFoundException)
      {
        Log.Warning("Client failed to get character profile - not found");
        throw new NotFoundException();
      }
    }

    internal void UpdateCharacterProfile(string token, uint id, string json)
    {
      Log.Debug("Client {token} updating character profile of {uid}", token, id);

      try
      {
        lock (this.db)
        {
          var character = this.FindAndAuthorize(token, id);
          character.ProfileJSON = json;

          this.db.CharacterUpdate(character);
        }
      }
      catch (Database.NotFoundException)
      {
        Log.Warning("Client {token} failed to update character profile - not found");
        throw new NotFoundException();
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
          lock (this.db)
          {
            character = this.db.CharacterFindById(id);
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
          lock(this.db)
          {
            return this.db.CharacterFindByName(name);
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
        throw new NotAuthorizedException();
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
