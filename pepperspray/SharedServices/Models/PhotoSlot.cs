using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SQLite;

namespace pepperspray.SharedServices
{
  public class PhotoSlot
  {
    [PrimaryKey, AutoIncrement]
    public uint Id { get; set; }

    [Indexed]
    public uint CharacterId { get; set; }

    public string Identifier { get; set; }
    public string Hash { get; set; }
  }
}
