using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pepperspray.CoreServer.Game
{
  internal class Lobby
  {
    internal string Identifier;
    internal UserRoom UserRoom;
    internal bool IsUserRoom { get { return this.UserRoom != null; } }
    internal List<PlayerHandle> Players;

    internal Lobby(string id)
    {
      this.Identifier = id;
      this.Players = new List<PlayerHandle>();
    }

    internal void AddPlayer(PlayerHandle player)
    {
      this.Players.Add(player);

      if (this.IsUserRoom)
      {
        this.UserRoom.NumberOfPlayers += 1;
      }
    }

    internal void RemovePlayer(PlayerHandle player)
    {
      this.Players.Remove(player);

      if (this.IsUserRoom)
      {
        this.UserRoom.NumberOfPlayers -= 1;
      }
    }
  }
}
