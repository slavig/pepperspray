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

    private List<PlayerHandle> players;

    internal Lobby(string id)
    {
      this.Identifier = id;
      this.players = new List<PlayerHandle>();
    }

    internal void AddPlayer(PlayerHandle player)
    {
      this.players.Add(player);

      if (this.IsUserRoom)
      {
        this.UserRoom.NumberOfPlayers += 1;
      }
    }

    internal void RemovePlayer(PlayerHandle player)
    {
      this.players.Remove(player);

      if (this.IsUserRoom)
      {
        this.UserRoom.NumberOfPlayers -= 1;
      }
    }

    internal IEnumerable<PlayerHandle> Players()
    {
      return this.players;
    }
  }
}
