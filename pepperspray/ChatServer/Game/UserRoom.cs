using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pepperspray.ChatServer.Game
{
  internal class UserRoom: IEquatable<UserRoom>
  {
    internal enum AccessType
    {
      ForAll,
      ForFriends,
      ForGroup
    }

    internal string Name;
    internal string Identifier;
    internal string Type;
    internal Lobby Lobby;
    internal AccessType Access;
    internal string RadioURL;

    internal uint OwnerId;
    internal string OwnerName;
    internal DateTime OwnerLastSeen;

    internal string[] ModeratorNames = new string[] { };

    internal bool IsPrioritized = false;
    internal bool IsVisibilityRestricted = false;

    internal bool IsSexAllowed = true;

    internal bool IsPermanent = false;
    internal bool IsSemiPersistent = false;
    internal bool IsDangling = false;

    internal int NumberOfPlayers
    {
      get
      {
        return this.Lobby != null ? this.Lobby.Players.Count : 0;
      }
    }

    public bool Equals(UserRoom other)
    {
      return this.Identifier.Equals(other.Identifier);
    }
  }
}
