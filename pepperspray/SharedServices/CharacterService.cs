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
  internal class CharacterService
  {
    internal class NameTakenException: Exception { }
    internal class InvalidNameException: Exception { }

    internal class NotFoundException: Exception { }
    internal class NotAuthorizedException: Exception { }

    internal static string characterPresetsDirectoryPath = Path.Combine("peppersprayData", "presets");

    private NameValidator nameValidator = DI.Auto<NameValidator>();
    private Database db = DI.Auto<Database>();
    private LoginServerListener loginServer = DI.Auto<LoginServerListener>();

    private Dictionary<uint, Character> loggedCharacters = new Dictionary<uint, Character>();

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
        ProfileJSON = JsonConvert.SerializeObject(new Dictionary<string, object>
        {
          { "age", 18 },
          { "interest", "?" },
          { "location", "Unknown" },
          { "about", "Empty" },
        }),
        FriendsJSON = "[]"
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

        return character.GetProfileJSON();
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

    internal void AcceptFriendRequest(string token, uint id, uint friendId)
    {
      Log.Debug("Client {token} accepting friend request of {id}", token, friendId);
      try
      {
        Character friendCharacter = null;
        Character character = null;

        lock(this.db)
        {
          friendCharacter = this.Find(friendId);
          character = this.FindAndAuthorize(token, id);
        }

        character.AddFriend(friendCharacter);
        friendCharacter.AddFriend(character);

        lock (this.db)
        {
          this.db.CharacterUpdate(character);
          this.db.CharacterUpdate(friendCharacter);
        }

        this.loginServer.Emit(friendId, "friend", new Dictionary<string, string>
        {
          { "for", friendCharacter.Id.ToString() },
          { "id", character.Id.ToString() },
          { "name", character.Name },
          { "sex", character.Sex },
        });
      }
      catch (LoginService.NotFoundException)
      {
        throw new NotFoundException();
      }
      catch (Database.NotFoundException)
      {
        throw new NotFoundException();
      }
    }

    internal void DeleteFriend(string token, uint id, uint friendId)
    {
      Log.Debug("Client {token} of user id {id} deleting friend {friendId}", token, id, friendId);
      try
      {
        Character character = null;
        Character friendCharacter = null;

        lock (this.db)
        {
          character = this.FindAndAuthorize(token, id);
        }

        character.RemoveFriend(friendId);

        try
        {
          lock (this.db)
          {
            friendCharacter = this.Find(friendId);
          }

          friendCharacter.RemoveFriend(character.Id);
          this.db.CharacterUpdate(friendCharacter);
        } 
        catch (Database.NotFoundException) { }

        lock (this.db)
        {
          this.db.CharacterUpdate(character);
        }

        try
        {
          if (friendCharacter != null)
          {
            this.loginServer.Emit(friendId, "unfriend", new Dictionary<string, string>
            {
              { "for", friendCharacter.Id.ToString() },
              { "id", character.Id.ToString() },
            });
          }
        }
        catch (LoginServerListener.NotFoundException) { }
      }
      catch (Database.NotFoundException)
      {
        throw new NotFoundException();
      }
    }

    internal string GetFriends(string token, uint id)
    {
      Log.Debug("Client {token} requesting friends of user id {id} ", token, id);
      try
      {
        Character character = null;

        lock(this.db)
        {
          character = this.FindAndAuthorize(token, id);
        }

        return character.FriendsJSON;
      }
      catch (Database.NotFoundException)
      {
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
      if (!this.loggedCharacters.TryGetValue(id, out character))
      {
        character = this.db.CharacterFindById(id);
      }

      return character;
    }

    internal Character FindAndAuthorize(string token, uint id)
    {
      try
      {
        var user = this.db.UserFindByToken(token);
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
  }
}
