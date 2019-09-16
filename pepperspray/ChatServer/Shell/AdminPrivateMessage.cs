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
  class AdminPrivateMessage: AShellCommand
  {
    private CharacterService characterService = DI.Get<CharacterService>();

    internal override bool RequireAdmin()
    {
      return true;
    }

    internal override bool WouldDispatch(string tag)
    {
      return tag.Equals("apm");
    }

    internal override IPromise<Nothing> Dispatch(ShellDispatcher dispatcher, PlayerHandle sender, ChatManager server, string tag, IEnumerable<string> arguments)
    {
      if (arguments.Count() != 1)
      {
        return dispatcher.InvalidUsage(sender, server);
      }

      var name = arguments.First().Trim();

      lock (server)
      {
        var player = server.World.FindPlayer(name);
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
            return dispatcher.Error(sender, server, Strings.PLAYER_NOT_FOUND, name);
          }
        }
      }
    }
  }
}
