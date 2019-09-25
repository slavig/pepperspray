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
    private ShellDispatcher dispatcher = DI.Get<ShellDispatcher>();
    private ChatManager manager = DI.Get<ChatManager>();

    internal override bool HasPermissionToExecute(PlayerHandle sender)
    {
      return sender.AdminOptions.HasFlag(AdminFlags.PlayerManagement);
    }

    internal override bool WouldDispatch(string tag, IEnumerable<string> arguments)
    {
      return tag.Equals("/aplayer");
    }

    internal override IPromise<Nothing> Dispatch(PlayerHandle sender, CommandDomain domain, string tag, IEnumerable<string> arguments)
    {
      var name = arguments.FirstOrDefault() ?? ".";

      var command = "info";
      if (arguments.Count() > 1)
      {
        command = arguments.ElementAt(1).Trim();
      }

      if (command.Equals("info"))
      {
        return this.showInfo(sender, domain, name);
      }
      else if (command.Equals("setstatus"))
      {
        string status = null;
        if (arguments.Count() > 2)
        {
          status = arguments.ElementAt(2).Trim();
        }

        return this.status(sender, domain, name, status);
      } 
      else
      {
        return this.dispatcher.Error(domain, Strings.UNKNOWN_COMMAND, command);
      }
    }

    internal IPromise<Nothing> status(PlayerHandle sender, CommandDomain domain, string name, string status)
    {
      PlayerHandle player = CommandUtils.GetPlayer(name, domain, this.manager);
      if (player != null)
      {
        name = player.Name;
      }

      Character character;
      try
      {
        character = this.characterService.Find(name);
      } 
      catch (CharacterService.NotFoundException)
      {
        return this.dispatcher.Error(domain, Strings.PLAYER_NOT_FOUND, name);
      }

      User user = null;
      try
      {
        user = this.loginService.FindUser(character.UserId);
      }
      catch (LoginService.NotFoundException) {
        return this.dispatcher.Error(domain, Strings.USER_HAS_NOT_BEEN_FOUND_FOR_PLAYER);
      }

      if (user != null && user.AdminFlags > 0)
      {
        return this.dispatcher.Error(domain, Strings.FORBIDDEN);
      }

      this.loginService.UpdateStatus(user, status);
      return this.dispatcher.Output(domain, Strings.STATUS_FOR_PLAYER_HAS_BEEN_SET, name, status);
    }

    internal IPromise<Nothing> showInfo(PlayerHandle sender, CommandDomain domain, string name)
    {
      PlayerHandle player = CommandUtils.GetPlayer(name, domain, this.manager);
      if (player != null)
      {
        name = player.Name;
      }

      Character character;
      try
      {
        character = this.characterService.Find(name);
      } 
      catch (CharacterService.NotFoundException)
      {
        return this.dispatcher.Error(domain, Strings.PLAYER_NOT_FOUND, name);
      }

      User user = null;
      try
      {
        user = this.loginService.FindUser(character.UserId);
      }
      catch (LoginService.NotFoundException) { }

      if (user != null && (user.AdminFlags > 0 && !sender.AdminOptions.HasFlag(AdminFlags.AdminPlayerManagement)))
      {
        return this.dispatcher.Error(domain, Strings.FORBIDDEN);
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

        if (user.AdminFlags > 0)
        {
          messages.Add(String.Format("Is admin (flags {0})", user.AdminFlags));
        }
      }

      return new CombinedPromise<Nothing>(messages.Select((m) => this.dispatcher.Output(domain, m)));
    }
  }
}
