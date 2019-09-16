using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RSG;
using pepperspray.CIO;
using pepperspray.ChatServer.Game;
using pepperspray.ChatServer.Services;
using pepperspray.SharedServices;
using pepperspray.Resources;

namespace pepperspray.ChatServer.Shell
{
  internal class AdminPlayer: AShellCommand
  {
    private CharacterService characterService = DI.Get<CharacterService>();
    private LoginService loginService = DI.Get<LoginService>();

    internal override bool RequireAdmin()
    {
      return true;
    }

    internal override bool WouldDispatch(string tag)
    {
      return tag.Equals("aplayer");
    }

    internal override IPromise<Nothing> Dispatch(ShellDispatcher dispatcher, PlayerHandle sender, ChatManager server, string tag, IEnumerable<string> arguments)
    {
      if (arguments.Count() < 1)
      {
        return dispatcher.InvalidUsage(sender, server);
      }

      var name = arguments.First().Trim();

      var command = "info";
      if (arguments.Count() > 1)
      {
        command = arguments.ElementAt(1).Trim();
      }

      if (command.Equals("info"))
      {
        return this.showInfo(dispatcher, sender, server, name);
      }
      else if (command.Equals("setstatus"))
      {
        string status = null;
        if (arguments.Count() > 2)
        {
          status = arguments.ElementAt(2).Trim();
        }

        return this.status(dispatcher, sender, server, name, status);
      } 
      else
      {
        return dispatcher.Error(sender, server, Strings.UNKNOWN_COMMAND, command);
      }
    }

    internal IPromise<Nothing> status(ShellDispatcher dispatcher, PlayerHandle sender, ChatManager server, string name, string status)
    {
      Character character;
      try
      {
        character = this.characterService.Find(name);
      } 
      catch (CharacterService.NotFoundException)
      {
        return dispatcher.Error(sender, server, Strings.PLAYER_NOT_FOUND, name);
      }

      User user = null;
      try
      {
        user = this.loginService.FindUser(character.UserId);
      }
      catch (LoginService.NotFoundException) {
        return dispatcher.Error(sender, server, Strings.USER_HAS_NOT_BEEN_FOUND_FOR_PLAYER);
      }

      if (user != null && user.IsAdmin)
      {
        return dispatcher.Error(sender, server, Strings.FORBIDDEN);
      }

      this.loginService.UpdateStatus(user, status);
      return dispatcher.Output(sender, server, Strings.STATUS_FOR_PLAYER_HAS_BEEN_SET, name, status);
    }

    internal IPromise<Nothing> showInfo(ShellDispatcher dispatcher, PlayerHandle sender, ChatManager server, string name)
    {
      Character character;
      try
      {
        character = this.characterService.Find(name);
      } 
      catch (CharacterService.NotFoundException)
      {
        return dispatcher.Error(sender, server, Strings.PLAYER_NOT_FOUND, name);
      }

      User user = null;
      try
      {
        user = this.loginService.FindUser(character.UserId);
      }
      catch (LoginService.NotFoundException) { }

      if (user != null && user.IsAdmin)
      {
        return dispatcher.Error(sender, server, Strings.FORBIDDEN);
      }

      PlayerHandle player = null;
      lock (server)
      {
        player = server.World.FindPlayer(name);
      }

      var messages = new List<string>();

      messages.Add(String.Format("Player {0} ({1})", name, character.Id));
      if (player == null)
      {
        messages.Add(String.Format("Last seen at {0}", character.LastLogin));
      }
      else
      {
        messages.Add(String.Format("Currently online ({0}) from {1}", player.CurrentLobbyIdentifier, player.Stream.ConnectionEndpoint));
      }

      if (user != null)
      {
        messages.Add(String.Format("User {0} ({1})", user.Username, user.Id));
        messages.Add(String.Format("Last logged: {0}", user.LastSeenAt));
        messages.Add(String.Format("Signed up: {0}", user.CreatedAt));
        messages.Add(String.Format("Total time online: {0} hours", TimeSpan.FromSeconds(user.TotalSecondsOnline).TotalHours));
        messages.Add(String.Format("Currency: {0}", user.Currency));

        if (user.Status != null)
        {
          messages.Add(String.Format("Status: {0}", user.Status));
        }

        if (user.IsAdmin)
        {
          messages.Add(String.Format("Is admin", user.Currency));
        }
      }

      return new CombinedPromise<Nothing>(messages.Select((m) => dispatcher.Output(sender, server, m)));
    }
  }
}
