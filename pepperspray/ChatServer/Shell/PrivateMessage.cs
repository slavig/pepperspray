using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RSG;
using pepperspray.CIO;
using pepperspray.ChatServer.Game;
using pepperspray.ChatServer.Protocol;
using pepperspray.SharedServices;
using pepperspray.Resources;

namespace pepperspray.ChatServer.Shell
{
  class PrivateMessage: AShellCommand
  {
    private CharacterService characterService = DI.Get<CharacterService>();
    private ShellDispatcher dispatcher = DI.Get<ShellDispatcher>();
    private ChatManager manager = DI.Get<ChatManager>();

    internal override bool WouldDispatch(string tag, IEnumerable<string> arguments)
    {
      return tag.Equals("/pm");
    }

    internal override IPromise<Nothing> Dispatch(PlayerHandle sender, CommandDomain domain, string tag, IEnumerable<string> arguments)
    {
      if (arguments.Count() != 1)
      {
        return this.dispatcher.InvalidUsage(sender);
      }

      var name = arguments.First().Trim();

      lock (this.manager)
      {
        var player = this.manager.World.FindPlayer(name);
        if (player != null)
        {
          return sender.Stream.Write(Responses.PrivateChatMessage(player, Strings.PLAYER_IS_ONLINE_YOU_CAN_MESSAGE_HIM));
        } 
        else
        {
          try
          {
            var character = this.characterService.Find(name);
            return sender.Stream.Write(Responses.ServerPrivateChatMessage(character.Name, character.Id, Strings.PLAYER_IS_OFFLINE_MESSAGES_WILL_BE_DELIVERED));
          }
          catch (CharacterService.NotFoundException)
          {
            return this.dispatcher.Error(sender, Strings.PLAYER_NOT_FOUND, name);
          }
        }
      }
    }
  }
}
