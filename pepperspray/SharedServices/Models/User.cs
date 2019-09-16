using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SQLite;

namespace pepperspray.SharedServices
{
  internal class User
  {
    [PrimaryKey, AutoIncrement]
    public uint Id { get; set; }
    public bool IsAdmin { get; set; }
    public uint Currency { get; set; }

    public string Username { get; set; }
    public string PasswordHash { get; set; }
    public string Token { get; set; }
    public string Status { get; set; }

    public DateTime LastSeenAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public double TotalSecondsOnline { get; set; }
  }
}
