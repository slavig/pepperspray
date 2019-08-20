using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SQLite;
using Newtonsoft.Json;

namespace pepperspray.SharedServices
{
  public class Character
  {
    [PrimaryKey, AutoIncrement]
    public uint Id { get; set; }

    [Indexed]
    public uint UserId { get; set; }

    [Indexed]
    public uint SpouseId { get; set; }

    public string AvatarSlot { get; set; }

    public DateTime LastLogin { get; set; }

    public string Name { get; set; }
    public string Sex { get; set; }
    public string Appearance { get; set; }

    public string ProfileJSON { get; set; }

    internal List<FriendLiaison> Liaisons;

    internal void AppendLiaison(FriendLiaison liaison)
    {
      if (this.Liaisons == null)
      {
        return;
      }
      else
      {
        this.Liaisons.Add(liaison);
      }
    }

    internal void RemoveLiaison(uint id)
    {
      if (this.Liaisons == null)
      {
        return;
      }
      else
      {
        this.Liaisons.RemoveAll(a => (a.InitiatorId == id) || (a.ReceiverId == id));
      }
    }
  }
}

