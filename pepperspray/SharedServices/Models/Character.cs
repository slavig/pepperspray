using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SQLite;
using Newtonsoft.Json;

namespace pepperspray.SharedServices
{
  internal class Character
  {
    [PrimaryKey, AutoIncrement]
    public uint Id { get; set; }

    [Indexed]
    public uint UserId { get; set; }

    public DateTime LastLogin { get; set; }

    public string Name { get; set; }
    public string Sex { get; set; }
    public string Appearance { get; set; }

    public string ProfileJSON { get; set; }
    public string FriendsJSON { get; set; }

    public string GetProfileJSON()
    {
      return JsonConvert.SerializeObject(new Dictionary<string, object> {
        { "id", this.Id },
        { "name", this.Name },
        { "sex", this.Sex },
        { "profile", this.ProfileJSON },
        { "gifts", 0 },
        { "married", new Dictionary<string, object> { { "id", 0 }, { "name", null }, {"sex", null } } },
        { "ava", 0 },
        { "photos", 0 },
        { "photoslots", new Dictionary<string, object> { } }
      });
    }

    public void AddFriend(Character character)
    {
      var friends = this.parseFriendsJSON();
      if (friends == null)
      {
        friends = new List<Dictionary<string, string>>() as IList<IDictionary<string, string>>;
      }

      friends.Add(new Dictionary<string, string> {
        { "id", character.Id.ToString() },
        { "n", character.Name },
        { "s", character.Sex },
      });

      this.FriendsJSON = JsonConvert.SerializeObject(friends);
    }

    public void RemoveFriend(uint characterId)
    {
      var friends = this.parseFriendsJSON();
      if (friends == null)
      {
        return;
      }

      foreach (var item in friends.ToArray())
      {
        string id = null;
        if (item.TryGetValue("id", out id))
        {
          if (id.Equals(characterId.ToString()))
          {
            friends.Remove(item);
          }
        }
      }

      this.FriendsJSON = JsonConvert.SerializeObject(friends);
    }

    public List<uint> GetFriendIDs()
    {
      var friends = this.parseFriendsJSON();
      if (friends == null)
      {
        return new List<uint>();
      }

      var result = new List<uint>();
      foreach (var record in friends)
      {
        string id = null;
        if (record.TryGetValue("id", out id))
        {
          try
          {
            result.Add(Convert.ToUInt32(id));
          }
          catch (FormatException) { }
        }
      }

      return result;
    }

    private IList<IDictionary<string, string>> parseFriendsJSON()
    {
      return JsonConvert.DeserializeObject<IList<IDictionary<string, string>>>(this.FriendsJSON);
    }
  }
}

