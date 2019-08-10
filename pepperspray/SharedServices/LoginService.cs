using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Serilog;
using Newtonsoft.Json;
using pepperspray.SharedServices;
using pepperspray.LoginServer;
using pepperspray.Utils;

namespace pepperspray.SharedServices
{
  internal class LoginService
  {
    internal class InvalidPasswordException : Exception {}
    internal class NotFoundException : Exception {}
    internal class InvalidTokenException : Exception {}

    private Configuration config = DI.Get<Configuration>();
    private Database db = DI.Auto<Database>();
    private Random random = DI.Auto<Random>();
    private LoginServer.LoginServerListener socialServer = DI.Auto<LoginServer.LoginServerListener>();

    internal User Login(string endpoint, string username, string passwordHash)
    {
      Log.Information("Client {endpoint} logging in with {username}:{passwordHash}", endpoint, username, passwordHash);

      try
      {
        User user = null;
        lock (this.db)
        {
          user = this.db.UserFind(username);
        }

        if (user.PasswordHash.Equals(passwordHash))
        {
          lock(this.db)
          {
            this.generateToken(user);
            this.db.UserUpdate(user);
          }

          return user;
        }
        else
        {
          Log.Debug("Login failed: password doesn't match");
          throw new InvalidPasswordException();
        }
      }
      catch (Database.NotFoundException)
      {
        throw new NotFoundException();
      }
    }

    internal bool SignUp(string endpoint, string username, string passwordHash)
    {
      lock (this.db)
      {
        try
        {
          this.db.UserFind(username);
          return false;
        }
        catch (Database.NotFoundException) { }
      }

      var user = new User
      {
        Username = username,
        PasswordHash = passwordHash,
        Token = Hashing.Md5(this.random.Next().ToString())
      };

      Log.Debug("Client {endpoint} signing up {name}/{password}",
        endpoint,
        username,
        passwordHash
      );

      lock (this.db)
      {
        this.db.UserInsert(user);
      }

      return true;
    }

    internal void DeleteAccount(string endpoint, string username, string passwordHash)
    {
      Log.Information("Client {endpoint} deleting account with {username}:{passwordHash}", endpoint, username, passwordHash);

      try
      {
        User user = null;
        lock (this.db)
        {
          user = this.db.UserFind(username);
        }

        if (user.PasswordHash.Equals(passwordHash))
        {
          lock(this.db)
          {
            // delete
            foreach (var character in this.db.CharactersFindByUser(user))
            {
              this.db.CharacterDelete(character);
            }

            this.db.UserDelete(user);
          }
        }
        else
        {
          Log.Debug("Delete failed: password doesn't match");
          throw new InvalidPasswordException();
        }
      }
      catch (Database.NotFoundException)
      {
        throw new NotFoundException();
      }
    }

    internal User AuthorizeUser(string token)
    {
      Log.Debug("Authorizing client by token {token}", token);

      try
      {
        lock(this.db)
        {
          return this.db.UserFindByToken(token);
        }
      } 
      catch (Database.NotFoundException)
      {
        throw new InvalidTokenException();
      }
    }

    internal Client AuthorizeClient(string token)
    {
      Log.Debug("Authorizing client on social by token {token}", token);

      try
      {
        return this.socialServer.FindClient(token);
      }
      catch (LoginServer.LoginServerListener.NotFoundException)
      {
        throw new InvalidTokenException();
      }
    }

    internal string GetLoginResponseText(User user)
    {
      lock (this.db)
      {
        var builder = new StringBuilder();
        builder.AppendLine("answer=" + (user.Status ?? "ok"));
        builder.AppendLine("token=" + user.Token);

        int i = 1;
        foreach (var ch in this.db.CharactersFindByUser(user))
        {
          builder.AppendLine("id" + i + "=" + ch.Id);
          builder.AppendLine("name" + i + "=" + ch.Name);
          builder.AppendLine("sex" + i + "=" + ch.Sex);
          builder.AppendLine("data" + i + "=" + ch.Appearance);

          i++;
        }

        return builder.ToString();
      }
    }

    internal string GetLoginFailedResponseText()
    {
      return "answer=fail";
    }

    private void generateToken(User user)
    {
      user.Token = Hashing.Md5(this.config.TokenSalt + this.random.Next());
    }
  }
}
