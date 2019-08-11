using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pepperspray.ChatServer.Game
{
  internal class UserRoom
  {
    internal enum AccessType
    {
      ForAll,
      ForGroup
    }

    internal string Name;
    internal string Identifier;
    internal AccessType Access;
    internal PlayerHandle User;
    internal bool Prioritized;
    internal int NumberOfPlayers;
  }
}
