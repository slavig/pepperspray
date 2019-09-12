using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Serilog;
using pepperspray.SharedServices;
using pepperspray.Utils;

namespace pepperspray.RestAPIServer.Services
{
  internal class BlacklistService: IDIService
  {
    internal class NotFoundException: Exception { }
    internal class AlreadySubmittedException : Exception { }

    private Database db;
    private LoginService loginService;
    private CharacterService characterService;

    public void Inject()
    {
      this.db = DI.Get<Database>();
      this.loginService = DI.Get<LoginService>();
      this.characterService = DI.Get<CharacterService>();
    }

    internal IEnumerable<Character> GetIgnoredCharacters(string token)
    {
      var user = this.loginService.AuthorizeUser(token);
      return this.db.Read((c) => c.BlacklistFindById(user.Id));
    }

    internal void IgnoreCharacter(string token, uint id)
    {

      try
      {
        var user = this.loginService.AuthorizeUser(token);
        try
        {
          this.db.Read((c) => c.BlacklistFind(user.Id, id));
          throw new AlreadySubmittedException();
        }
        catch (Database.NotFoundException) { }

        var character = this.characterService.Find(id);
        var ignoredCharacter = new BlacklistRecord
        {
          UserId = user.Id,
          ViolatorId = character.Id
        };

        this.db.Write((c) => c.BlacklistInsert(ignoredCharacter));
      }
      catch (CharacterService.NotFoundException)
      {
        throw new NotFoundException();
      }
    }

    internal void UnignoreCharacter(string token, uint id)
    {
      try
      {
        var user = this.loginService.AuthorizeUser(token);
        var character = this.characterService.Find(id);

        var ignoredCharacter = this.db.Read((c) => c.BlacklistFind(user.Id, character.Id));
        this.db.Write((c) => c.BlacklistDelete(ignoredCharacter));
      }
      catch (CharacterService.NotFoundException)
      {
        throw new NotFoundException();
      }
    }
  }
}
