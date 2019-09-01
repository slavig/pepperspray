using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Threading.Tasks;

using SQLite;
using System.Collections.Concurrent;

namespace pepperspray.SharedServices
{
  internal class Database: IDIService
  {
    internal class NotFoundException : Exception { }

    private ReaderWriterLockSlim rwLock = new ReaderWriterLockSlim();
    private ConcurrentQueue<DatabaseConnection> pool = new ConcurrentQueue<DatabaseConnection>();
    private DatabaseConnection mutatingConnection;

    public void Inject()
    {

    }

    public Database()
    {
      var connection = new SQLiteConnection(Path.Combine("peppersprayData", "database", "database.sqlite"));
      connection.CreateTable<Character>();
      connection.CreateTable<User>();
      connection.CreateTable<PhotoSlot>();
      connection.CreateTable<FriendLiaison>();
      connection.CreateTable<Gift>();
      connection.CreateTable<OfflineMessage>();

      this.mutatingConnection = new DatabaseConnection(connection);
    }

    internal T Read<T>(Func<DatabaseConnection, T> action)
    {
      DatabaseConnection connection;
      if (!this.pool.TryDequeue(out connection))
      { 
        connection = this.makeNewConnection();
      }

      T result;

      try
      {
        this.rwLock.EnterReadLock();
        result = action(connection);
      }
      finally
      {
        this.rwLock.ExitReadLock();
      }

      this.pool.Enqueue(connection);
      return result;
    }

    internal void Write(Action<DatabaseConnection> action)
    {
      try
      {
        this.rwLock.EnterWriteLock();
        action(this.mutatingConnection);
      }
      finally
      {
        this.rwLock.ExitWriteLock();
      }
    }

    private DatabaseConnection makeNewConnection()
    {
      return new DatabaseConnection(this.makeSqliteConnection());
    }

    private SQLiteConnection makeSqliteConnection()
    {
      return new SQLiteConnection(Path.Combine("peppersprayData", "database", "database.sqlite"));
    }
  }

  internal class DatabaseConnection
  {
    private SQLiteConnection connection;

    internal DatabaseConnection(SQLiteConnection connection)
    {
      this.connection = connection;
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

    internal void LiaisonInsert(FriendLiaison liaison)
    {
      this.connection.Insert(liaison);
    }

    internal void LiaisonDelete(FriendLiaison liaison)
    {
      this.connection.Delete(liaison);
    }

    internal IEnumerable<FriendLiaison> LiaisonFindByCharacter(Character character)
    {
      return this.connection.Table<FriendLiaison>().Where(c => c.InitiatorId == character.Id || c.ReceiverId == character.Id);
    }

    internal IEnumerable<FriendLiaison> LiaisonFindByParticipants(Character char1, Character char2)
    {
      return this.connection.Table<FriendLiaison>().Where(c => (c.InitiatorId == char1.Id && c.ReceiverId == char2.Id) || (c.InitiatorId == char2.Id && c.ReceiverId == char1.Id));
    }

    internal void LiaisonDeleteByParticipants(uint ch1, uint ch2)
    {
      this.connection.Table<FriendLiaison>().Where(c => (c.InitiatorId == ch1 && c.ReceiverId == ch2) || (c.InitiatorId == ch2 && c.ReceiverId == ch1)).Delete();
    }

    internal void PhotoSlotInsert(PhotoSlot slot)
    {
      this.connection.Insert(slot);
    }

    internal void PhotoSlotUpdate(PhotoSlot slot)
    {
      this.connection.Update(slot);
    }

    internal void PhotoSlotDelete(PhotoSlot slot)
    {
      this.connection.Delete(slot);
    }

    internal PhotoSlot PhotoSlotGet(Character character, string slot)
    {
      return this.connection.Table<PhotoSlot>().Where(c => c.CharacterId == character.Id && c.Identifier == slot).RetrieveFirst();
    }

    internal IEnumerable<PhotoSlot> PhotoSlotFindByCharacterId(uint id)
    {
      return this.connection.Table<PhotoSlot>().Where(c => c.CharacterId == id).OrderBy(p => p.Identifier);
    }

    internal PhotoSlot PhotoSlotFind(uint id, string slot)
    {
      return this.connection.Table<PhotoSlot>().Where(c => c.CharacterId == id && c.Identifier == slot).RetrieveFirst();
    }

    internal Gift GiftFindById(uint id)
    {
      return this.connection.Table<Gift>().Where(c => c.Id == id).RetrieveFirst();
    }

    internal IEnumerable<Gift> GiftsFind(uint id, uint offset, uint limit)
    {
      return this.connection.Query<Gift>("SELECT * FROM Gift WHERE RecepientId = ? ORDER BY Date DESC LIMIT ?, ?", id, offset, limit);
    }

    internal uint GiftsCount(uint id)
    {
      return this.connection.ExecuteScalar<uint>("SELECT Count(id) FROM Gift WHERE RecepientId = ?", id);
    }

    internal void GiftInsert(Gift gift)
    {
      this.connection.Insert(gift);
    }

    internal void GiftDelete(Gift gift)
    {
      this.connection.Delete(gift);
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

    internal void OfflineMessageInsert(OfflineMessage msg)
    {
      this.connection.Insert(msg);
    }

    internal IEnumerable<OfflineMessage> OfflineMessageFind(uint recepientId)
    {
      return this.connection.Table<OfflineMessage>().Where(m => m.RecepientId == recepientId);
    }

    internal void OfflineMessageDelete(uint recepientId)
    {
      this.connection.Table<OfflineMessage>().Where(m => m.RecepientId == recepientId).Delete();
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
