using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pepperspray.CoreServer.Game
{
  internal class Group
  {
    internal int Identifier;
    internal List<PlayerHandle> Players;

    internal Group(int identifier)
    {
      this.Identifier = identifier;
      this.Players = new List<PlayerHandle>();
    }

    internal void AddPlayer(PlayerHandle player)
    {
      this.Players.Add(player);
    }

    internal void RemovePlayer(PlayerHandle player)
    {
      if (this.Players.Contains(player))
      {
        this.Players.Remove(player);
      }
    }

    internal bool ContainsPlayer(PlayerHandle player)
    {
      return this.Players.Contains(player);
    }
  }
}
