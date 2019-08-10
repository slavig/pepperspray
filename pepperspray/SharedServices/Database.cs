using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

using SQLite;

namespace pepperspray.SharedServices
{
  internal class Database
  {
    internal class NotFoundException : Exception { }

    private SQLiteConnection connection = new SQLiteConnection(Path.Combine("peppersprayData", "database", "database.sqlite"));

    public Database()
    {
      this.connection.CreateTable<Character>();
      this.connection.CreateTable<User>();
    }

    internal void CharacterInsert(Character ch)
    {
      this.connection.Insert(ch);
    }

    internal void CharacterUpdate(Character ch)
    {
      this.connection.Update(ch);
    }

    internal void CharacterDelete(Character ch)
    {
      this.connection.Delete(ch);
    }

    internal void CharacterDeleteById(uint uid)
    {
      this.connection.Table<Character>().Where(c => c.Id.Equals(uid)).Delete();
    }

    internal Character CharacterFindByName(string name)
    {
      return this.connection.Table<Character>().Where(c => c.Name.Equals(name)).RetrieveFirst();
    }

    internal Character CharacterFindById(uint id)
    {
      return this.connection.Table<Character>().Where(c => c.Id == id).RetrieveFirst();
    }

    internal Character CharacterFindByIdAndUser(uint id, uint userid)
    {
      return this.connection.Table<Character>().Where(c => c.Id.Equals(id) && c.UserId == userid).RetrieveFirst();
    }

    internal IEnumerable<Character> CharactersFindByUser(User user)
    {
      return this.connection.Table<Character>().Where(c => c.UserId == user.Id);
    }

    internal void UserInsert(User user)
    {
      this.connection.Insert(user);
    }

    internal void UserUpdate(User user)
    {
      this.connection.Update(user);
    }

    internal void UserDelete(User user)
    {
      this.connection.Delete(user);
    }

    internal User UserFind(string username)
    {
      return this.connection.Table<User>().Where(u => u.Username.Equals(username)).RetrieveFirst();
    }

    internal User UserFindByToken(string token)
    {
      return this.connection.Table<User>().Where(u => u.Token.Equals(token)).RetrieveFirst();
    }
  }

  internal static class Extension
  {
    internal static T RetrieveFirst<T>(this TableQuery<T> query)
    {
      if (query.Count() == 0)
      {
        throw new Database.NotFoundException();
      } else
      {
        return query.First();
      }
    }
  }
}
