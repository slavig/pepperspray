using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SQLite;

namespace pepperspray.SharedServices
{
  public class OfflineMessage
  {
    [PrimaryKey, AutoIncrement]
    public uint Id { get; set; }

    [Indexed]
    public uint SenderId { get; set; }

    [Indexed]
    public uint RecepientId { get; set; }

    public string Message { get; set; }
  }
}
