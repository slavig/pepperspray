using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Serilog;
using pepperspray.SharedServices;
using pepperspray.Utils;

namespace pepperspray.SharedServices
{
  internal class PhotoService: IDIService
  {
    private Database db;
    private CharacterService characterService;

    public void Inject()
    {
      this.db = DI.Get<Database>();
      this.characterService = DI.Get<CharacterService>();
    }

    internal void SetPhoto(string token, uint id, string slot, string hash)
    {
      Log.Debug("Client {token} of {id} setting photo {slot} to {hash}", token, id, slot, hash);
      var character = this.characterService.FindAndAuthorize(token, id);
      try
      {
        var photoSlot = this.db.Read((c) => c.PhotoSlotGet(character, slot));
        photoSlot.Hash = hash;

        this.db.Write((c) => c.PhotoSlotUpdate(photoSlot));
      }
      catch (Database.NotFoundException)
      {
        var photoSlot = new PhotoSlot
        {
          CharacterId = character.Id,
          Identifier = slot,
          Hash = hash,
        };

        this.db.Write((c) => c.PhotoSlotInsert(photoSlot));
      }
    }

    internal void DeletePhoto(Character character, PhotoSlot slot)
    {
      if (character.AvatarSlot == slot.Identifier)
      {
        character.AvatarSlot = null;
        this.db.Write((c) => c.CharacterUpdate(character));
      }

      this.db.Write((c) => c.PhotoSlotDelete(slot));
    }

    internal PhotoSlot GetPhoto(uint id, string slot)
    {
        return this.db.Read((c) => c.PhotoSlotFind(id, slot));
    }

    internal void SetAvatar(string token, uint id, string slot)
    {
      var character = this.characterService.FindAndAuthorize(token, id);
      character.AvatarSlot = slot;

      this.db.Write((c) => c.CharacterUpdate(character));
    }
  }
}
