using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Serilog;
using Newtonsoft.Json;
using pepperspray.LoginServer;
using pepperspray.Utils;

namespace pepperspray.SharedServices
{
  internal class FriendsService: IDIService
  {
    internal class NotFoundException: Exception { }

    private Database db;
    private LoginServerListener loginServer;
    private CharacterService characterService;

    public void Inject()
    {
      this.db = DI.Get<Database>();
      this.loginServer = DI.Get<LoginServerListener>();
      this.characterService = DI.Get<CharacterService>();
    }

  internal void AcceptFriendRequest(string token, uint id, uint friendId)
    {
      Log.Debug("Client {token} of {id} accepting friend request of {friendId}", token, id, friendId);
      try
      {
        Character friendCharacter = this.characterService.Find(friendId);
        Character character = this.characterService.FindAndAuthorize(token, id);

        if (this.db.Read((c) => c.LiaisonFindByParticipants(character, friendCharacter)).Count() != 0)
        {
          Log.Warning("Client {token} can't accept friend request of {id} - already have liaison with {friendId}", token, id, friendId);
          throw new InvalidOperationException();
        }

        var liaison = new FriendLiaison
        {
          InitiatorId = character.Id,
          ReceiverId = friendCharacter.Id
        };

        friendCharacter.AppendLiaison(liaison);
        character.AppendLiaison(liaison);
        this.db.Write((c) => c.LiaisonInsert(liaison));

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
        Character character = this.characterService.FindAndAuthorize(token, id);
        this.db.Write((c) => c.LiaisonDeleteByParticipants(character.Id, friendId));
        character.RemoveLiaison(friendId);

        try
        {
          Character friendCharacter = this.characterService.Find(friendId);
          friendCharacter.RemoveLiaison(character.Id);
          this.loginServer.Emit(friendId, "unfriend", new Dictionary<string, string>
            {
              { "for", friendCharacter.Id.ToString() },
              { "id", character.Id.ToString() },
            });
        }
        catch (LoginServerListener.NotFoundException) { }
      }
      catch (Database.NotFoundException)
      {
        throw new NotFoundException();
      }
    }

    internal IEnumerable<uint> GetFriendIDs(Character character)
    {
      IEnumerable<FriendLiaison> liaisons = character.Liaisons;

      if (liaisons == null)
      {
        liaisons = this.db.Read((c) => c.LiaisonFindByCharacter(character));
      }

      return liaisons.Select(a => a.InitiatorId == character.Id ? a.ReceiverId : a.InitiatorId);
    }

    internal string GetFriends(string token, uint id)
    {
      Log.Debug("Client {token} requesting friends of user id {id} ", token, id);
      try
      {
        Character character = null;
        IEnumerable<FriendLiaison> liaisons = null;

        character = this.characterService.FindAndAuthorize(token, id);
        liaisons = this.db.Read((c) => c.LiaisonFindByCharacter(character));

        var result = new List<Dictionary<string, string>>();
        foreach (var liaison in liaisons)
        {
          Character friend = null;
          try
          {
            friend = this.characterService.Find(liaison.InitiatorId == character.Id ? liaison.ReceiverId : liaison.InitiatorId);
          }
          catch (CharacterService.NotFoundException)
          {
            continue;
          }

          result.Add(new Dictionary<string, string>
          {
            { "id", friend.Id.ToString() },
            { "n", friend.Name },
            { "s", friend.Sex }
          });
        }

        return JsonConvert.SerializeObject(result);
      }
      catch (Database.NotFoundException)
      {
        throw new NotFoundException();
      }
    }
  }
}
