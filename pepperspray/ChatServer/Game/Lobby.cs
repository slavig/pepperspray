using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pepperspray.ChatServer.Game
{
  internal class Lobby
  {
    private static Dictionary<string, string> locationsMapping = new Dictionary<string, string>
    {
      { "SALOON", "Saloon" },
      { "beach", "Beach" },
      { "island", "Island" },
      { "CLUB_NEW", "Fresco" },
      { "YACHT", "Yacht" },
      { "club", "Nightclub" },
      { "BDSM_club", "Sin Club" },
    };

    internal string Identifier;
    internal List<PlayerHandle> Players;
    internal UserRoom UserRoom;
    internal string RadioURL;

    internal string Name {
      get
      {
        string name = null;
        Lobby.locationsMapping.TryGetValue(this.Identifier, out name);
        return name;
      }
    }
    internal bool IsUserRoom { get { return this.UserRoom != null; } }
    internal bool IsPrivateRoom { get { return this.UserRoom == null && this.Identifier.EndsWith("_room"); } }
    internal bool IsStandard { get { return Lobby.locationsMapping.Keys.Contains(this.Identifier);  } }

    internal Lobby(string id)
    {
      this.Identifier = id;
      this.Players = new List<PlayerHandle>();
    }

    internal void AddPlayer(PlayerHandle player)
    {
      this.Players.Add(player);
    }

    internal void RemovePlayer(PlayerHandle player)
    {
      this.Players.Remove(player);
    }
  }
}
