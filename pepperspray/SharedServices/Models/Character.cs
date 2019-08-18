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

    public string AvatarSlot { get; set; }

    public DateTime LastLogin { get; set; }

    public string Name { get; set; }
    public string Sex { get; set; }
    public string Appearance { get; set; }

    public string ProfileJSON { get; set; }

  }
}

