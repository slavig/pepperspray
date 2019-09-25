using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RSG;
using Serilog;
using pepperspray.CIO;
using pepperspray.ChatServer.Game;
using pepperspray.Utils;
using pepperspray.LoginServer;
using pepperspray.SharedServices;
using pepperspray.Resources;

namespace pepperspray.ChatServer.Protocol.Requests
{
  internal class Login : ARequest
  {
    private Configuration config = DI.Get<Configuration>();
    private CharacterService characterService = DI.Get<CharacterService>();
    private LoginService loginService = DI.Get<LoginService>();
    private LoginServerListener loginServer = DI.Get<LoginServerListener>();

    private string name, sex, token;
    private uint id;
    private Character character;
    private User user;

    internal static Login Parse(Message ev)
    {
      if (ev.data is Dictionary<string, object> == false)
      {
        return null;
      }

      try
      {
        var arguments = ev.data as Dictionary<string, object>;
        string name = CharacterService.StripCharacterName(arguments["name"].ToString());
        var id = Convert.ToUInt32(arguments["id"].ToString());
        string sex = arguments["sex"].ToString();
        string token = arguments["token"].ToString();

        if (name != null && sex != null && token != null)
        {
          return new Login
          {
            name = name,
            id = id,
            sex = sex,
            token = token
          };
        } else
        {
          return null;
        }
      }
      catch (FormatException)
      {
        return null;
      }
    }

    internal override bool Validate(PlayerHandle sender, ChatManager server)
    {
      ErrorException exception = null;
      try
      {
        this.user = this.loginService.AuthorizeUser(this.token);
        this.character = this.characterService.LoginCharacter(user, this.id, this.name, this.sex);

        PlayerHandle sameUserPlayer;
        lock (server)
        {
          sameUserPlayer = server.World.FindPlayerByUser(user);
        }

        if (sameUserPlayer != null)
        {
          server.KickPlayer(sameUserPlayer, Strings.LOGGED_IN_AS_ANOTHER_CHAR);
        }

        PlayerHandle sameNamePlayer;
        lock (server)
        {
          sameNamePlayer = server.World.FindPlayer(this.name);
        }

        if (sameNamePlayer != null)
        {
          server.KickPlayer(sameNamePlayer, Strings.LOGGED_IN_IN_ANOTHER_INSTANCE);
        }
      }
      catch (LoginService.InvalidTokenException)
      {
        exception = new ErrorException("invalid token", Strings.LOGIN_TOKEN_INVALID);
      }
      catch (CharacterService.NotAuthorizedException)
      {
        exception = new ErrorException("not authorized", Strings.NOT_AUTHORIZED_TO_PLAY_AS_THIS_CHARACTER);
      }
      catch (CharacterService.NotFoundException)
      {
        exception = new ErrorException("not found", Strings.CHARACTER_NOT_FOUND);
      }
      catch (ErrorException e)
      {
        exception = e;
      }
      catch (Exception e)
      {
        Log.Warning("Failed to login character {name}/{token}: {exception}", this.name, this.token, e);
        exception = new ErrorException("internal", Strings.INTERNAL_SERVER_ERROR);
      }

      if (exception != null)
      {
        Log.Information("Kicking connection of {name}/{id} from {hash}/{address} - {message}, terminating shortly after",
          this.name,
          this.id,
          sender.Stream.ConnectionHash,
          sender.Stream.ConnectionEndpoint,
          exception.Message);

        sender.Terminate(exception);
        return false;
      }
      else
      {
        return true;
      }
    }

    internal override IPromise<Nothing> Process(PlayerHandle sender, ChatManager server)
    {
      sender.Name = this.name;
      sender.Id = this.id;
      sender.Sex = this.sex;
      sender.IsLoggedIn = true;
      sender.Character = this.character;
      sender.User = this.user;
      sender.AdminOptions = new PlayerHandle.AdminOptionsConfiguration(this.user.AdminFlags);
      sender.Token = this.token;

      this.loginServer.AssociateCharacter(this.token, this.character);

      lock (server)
      {
        server.PlayerLoggedIn(sender);
      }

      return Nothing.Resolved();
    }
  }
}
