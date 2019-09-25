using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Serilog;
using Newtonsoft.Json;
using pepperspray.SharedServices;
using pepperspray.LoginServer;
using pepperspray.ChatServer.Game;
using pepperspray.ChatServer.Services.Events;
using pepperspray.RestAPIServer.Services;
using pepperspray.Utils;

namespace pepperspray.SharedServices
{
  internal class LoginService: IDIService, PlayerLoggedOffEvent.IListener
  {
    internal class InvalidPasswordException : Exception {}
    internal class NotFoundException : Exception {}
    internal class InvalidTokenException : Exception {}
    internal class UnsupportedProtocolVersionException: Exception {}
    internal class EndpointBannedException : Exception {}
    internal class SignupDisabledByConfig : Exception {}

    private Configuration config;
    private Database db;
    private MailService mailService;
    private Random random;
    private LoginServerListener socialServer;
    private CharacterService characterService;

    public void Inject()
    {
      this.config = DI.Get<Configuration>();
      this.db = DI.Get<Database>();
      this.mailService = DI.Get<MailService>();
      this.random = new Random();
      this.socialServer = DI.Get<LoginServerListener>();
      this.characterService = DI.Get<CharacterService>();
    }

    internal void CheckProtocolVersion(string versionString)
    {
      if (versionString.Equals(this.config.WebfrontProtocolVersion))
      {
        return;
      }

      try
      {
        var version = Convert.ToUInt32(versionString);
        if (version >= this.config.MinimumProtocolVersion)
        {
          return;
        }
      }
      catch (FormatException) { }

      throw new UnsupportedProtocolVersionException();
    }

    internal User Login(string endpoint, string username, string passwordHash)
    {
      Log.Information("Client {endpoint} logging in with {username}:{passwordHash}", endpoint, username, passwordHash);
      this.checkEndpointIfBanned(endpoint);

      try
      {
        User user = this.db.Read((c) => c.UserFind(username));

        if (user.PasswordHash.Equals(passwordHash))
        {
          this.generateToken(user);
          user.LastSeenAt = DateTime.Now;

          this.db.Write((c) => c.UserUpdate(user));
          return user;
        }
        else
        {
          throw new InvalidPasswordException();
        }
      }
      catch (Database.NotFoundException)
      {
        throw new NotFoundException();
      }
      catch (Exception e)
      {
        Log.Warning("Client {endpoint} failed to login: {exception}", endpoint, e);
        throw e;
      }
    }

    internal void ChangePassword(string endpoint, string token, string passwordHash, string newPasswordHash)
    {
      Log.Information("Client {endpoint} changing password with {token}:{passwordHash} to {newPasswordHash}", endpoint, token, passwordHash, newPasswordHash);

      try
      {
        User user = this.AuthorizeUser(token);
        if (user.PasswordHash.Equals(passwordHash))
        {
          user.PasswordHash = newPasswordHash;

          this.db.Write((c) => c.UserUpdate(user));
        }
        else
        {
          Log.Debug("Change password failed: password doesn't match");
          throw new InvalidPasswordException();
        }
      }
      catch (Database.NotFoundException)
      {
        throw new NotFoundException();
      }
    }

    internal void ForgotPassword(string endpoint, string username)
    {
      Log.Information("Client {endpoint} asking for password forgot of {username}", endpoint, username);

      try
      {
        User user = this.db.Read((c) => c.UserFind(username));

        var newPassword = this.generateRandomPassword();
        var newPasswordHash = this.hashPassword(username, newPassword);

        user.PasswordHash = newPasswordHash;
        this.db.Write((c) => c.UserUpdate(user));

        this.mailService.SendMessage(username, "pepperspray - password changed", "New password: {0}", newPassword);
      }
      catch (Database.NotFoundException)
      {
        throw new NotFoundException();
      }
    }

    internal User SignUp(string endpoint, string username, string passwordHash)
    {
      if (!this.config.SignUpEnabled)
      {
        throw new SignupDisabledByConfig();
      }

      try
      {
        this.db.Read((c) => c.UserFind(username));
        return null;
      }
      catch (Database.NotFoundException) { }

      var user = new User
      {
        Username = username,
        PasswordHash = passwordHash,
        Token = Hashing.Md5(this.random.Next().ToString()),
        CreatedAt = DateTime.Now,
      };

      Log.Debug("Client {endpoint} signing up {name}/{password}",
        endpoint,
        username,
        passwordHash
      );

      this.checkEndpointIfBanned(endpoint);

      this.db.Write((c) => c.UserInsert(user));
      return user;
    }

    internal void DeleteAccount(string endpoint, string token, string passwordHash)
    {
      Log.Information("Client {endpoint} deleting account with {username}:{passwordHash}", endpoint, token, passwordHash);

      try
      {
        User user = this.AuthorizeUser(token);

        if (user.PasswordHash.Equals(passwordHash))
        {
          var characters = this.db.Read(c => c.CharactersFindByUser(user));
          foreach (var character in characters)
          {
            this.characterService.DeleteCharacter(token, character.Id);
          }

          this.db.Write((c) => c.UserDelete(user));
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
      Log.Verbose("Authorizing client by token {token}", token);

      try
      {
        return this.db.Read((c) => c.UserFindByToken(token));
      }
      catch (Database.NotFoundException)
      {
        throw new InvalidTokenException();
      }
    }

    internal Client AuthorizeClient(string token)
    {
      Log.Verbose("Authorizing client on social by token {token}", token);

      try
      {
        return this.socialServer.FindClient(token);
      }
      catch (LoginServer.LoginServerListener.NotFoundException)
      {
        throw new InvalidTokenException();
      }
    }

    internal User FindUser(uint id)
    {
      try
      {
        return this.db.Read((c) => c.UserFind(id));
      }
      catch (Database.NotFoundException)
      {
        throw new NotFoundException();
      }
    }

    internal void UpdateStatus(User user, string status)
    {
      user.Status = status;
      this.db.Write(c => c.UserUpdate(user));
    }

    public void PlayerLoggedOff(PlayerLoggedOffEvent ev)
    {
      var beenOnlineFor = DateTime.Now - ev.Handle.LoggedAt;
      var user = this.FindUser(ev.Handle.User.Id);

      user.TotalSecondsOnline += beenOnlineFor.TotalSeconds;
      this.db.Write(c => c.UserUpdate(user));
    }

    internal string GetLoginResponseText(User user)
    {
      var builder = new StringBuilder();

      var status = user.Status != null && user.Status != "" ? user.Status : "ok";
      builder.AppendLine("answer=" + (status));
      builder.AppendLine("token=" + user.Token);

      int i = 1;
      foreach (var ch in this.db.Read((c) => c.CharactersFindByUser(user)))
      {
        builder.AppendLine("id" + i + "=" + ch.Id);
        builder.AppendLine("name" + i + "=" + ch.Name);
        builder.AppendLine("sex" + i + "=" + ch.Sex);
        builder.AppendLine("data" + i + "=" + ch.Appearance);

        i++;
      }

      return builder.ToString();
    }

    internal string GetLoginFailedResponseText()
    {
      return "answer=fail";
    }

    internal string GetUnsupportedProtocolVersionResponseText()
    {

      return "answer=expired";
    }

    internal string GetBannedResponseText()
    {
      return "answer=banned";
    }

    private void checkEndpointIfBanned(string endpoint)
    {
      if (this.config.BannedAddresses == null)
      {
        return;
      }

      var addr = endpoint.Substring(0, endpoint.LastIndexOf(':'));

      foreach (var bannedAddress in this.config.BannedAddresses)
      {
        if (addr.Equals(bannedAddress))
        {
          Log.Information("Client {endpoint} denied action: address match with banned {ip}", endpoint, bannedAddress);
          throw new EndpointBannedException();
        }
      }
    }

    private void generateToken(User user)
    {
      user.Token = Hashing.Md5(this.config.TokenSalt + this.random.Next());
    }

    private string generateRandomPassword()
    {
      var builder = new StringBuilder();
      for (int i = 0; i < 8; i++)
      {
        builder.Append(this.random.Next(9));
      }

      return builder.ToString();
    }

    private string hashPassword(string username, string password)
    {
      return Hashing.Md5(username + password + "login");
    }
  }
}
