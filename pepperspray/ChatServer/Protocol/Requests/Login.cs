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
using pepperspray.SharedServices;

namespace pepperspray.ChatServer.Protocol.Requests
{
  internal class Login : ARequest
  {
    private Configuration config = DI.Get<Configuration>();
    private CharacterService characterService = DI.Auto<CharacterService>();
    private LoginService loginService = DI.Auto<LoginService>();

    private string name, sex, token;
    private uint id;
    private Character character;
    private User user;
    private Client client;

    internal static Login Parse(Message ev)
    {
      if (ev.data is Dictionary<string, object> == false)
      {
        return null;
      }

      try
      {
        var arguments = ev.data as Dictionary<string, object>;
        string name = arguments["name"].ToString();
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
        this.client = this.loginService.AuthorizeClient(token);
        this.character = this.characterService.LoginCharacter(user, this.id, this.name, this.sex);

        var sameUserPlayer = server.World.FindPlayerByUser(user);
        if (sameUserPlayer != null)
        {
          server.KickPlayer(sameUserPlayer, "Logged as another character.");
        }

        var sameNamePlayer = server.World.FindPlayer(this.name);
        if (sameNamePlayer != null)
        {
          server.KickPlayer(sameNamePlayer, "Logged in another instance.");
        }
      }
      catch (LoginService.InvalidTokenException)
      {
        exception = new ErrorException("invalid token", "Login token invalid.");
      }
      catch (CharacterService.NotAuthorizedException)
      {
        exception = new ErrorException("not authorized", "You are not authorized to play as this character.");
      }
      catch (CharacterService.NotFoundException)
      {
        exception = new ErrorException("not found", "Character not found.");
      }
      catch (ErrorException e)
      {
        exception = e;
      }

      if (exception != null)
      {
        Log.Information("Terminating connection of {name}/{id} from {hash}/{address} - {message}",
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
      sender.Client = this.client;

      this.client.LoggedCharacter = this.character;

      lock (server)
      {
        server.PlayerLoggedIn(sender);
      }

      return Nothing.Resolved();
    }
  }
}
