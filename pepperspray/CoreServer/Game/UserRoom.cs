using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pepperspray.CoreServer.Game
{
  internal class UserRoom
  {
    internal enum AccessType
    {
      ForAll
    }

    internal string Name;
    internal string Identifier;
    internal AccessType Access;
    internal PlayerHandle User;
    internal int NumberOfPlayers;
  }
}
