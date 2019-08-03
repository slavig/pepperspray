using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Serilog;
using pepperspray.CoreServer.Game;

namespace pepperspray.CoreServer.Protocol
{
  internal class ActionsAuthenticator
  {
    private class PoseAgreement
    {
      internal string Initiator;
      internal List<string> Participants = new List<string>();

      internal bool Contains(string playerName)
      {
        return this.Initiator.Equals(playerName) || this.Participants.Contains(playerName);
      }
    }

    private class Command
    {
      internal enum CommandType
      {
        action,
        none
      }
      internal CommandType Type;
      internal string Name;
      internal string[] Arguments;
    }

    private class AuthenticationTemplate
    {
      internal int ArgumentIndex;

      internal bool Authenticate(PlayerHandle sender, Command command)
      {
        if (command.Arguments.Count() < this.ArgumentIndex)
        {
          return false;
        }

        if (!command.Arguments.ElementAt(this.ArgumentIndex).Equals(sender.Name))
        {
          return false;
        }

        return true;
      }
    }

    private Dictionary<string, PoseAgreement> activeAgreements = new Dictionary<string, PoseAgreement>();
    private Dictionary<string, AuthenticationTemplate> authTemplates = new Dictionary<string, AuthenticationTemplate>
    {
      { "walk", new AuthenticationTemplate {ArgumentIndex  = 3} },
      { "askForPose", new AuthenticationTemplate {ArgumentIndex  = 2} },
      { "acceptPoseAsk", new AuthenticationTemplate {ArgumentIndex  = 0} },
      { "runPropAction", new AuthenticationTemplate {ArgumentIndex  = 1} },
      { "takeFood", new AuthenticationTemplate {ArgumentIndex  = 0} },
      { "eatFood", new AuthenticationTemplate {ArgumentIndex  = 0} },
    };

    internal ActionsAuthenticator()
    {

    }

    internal bool ShouldProcess(PlayerHandle sender, PlayerHandle recepient, string message)
    {
      var command = this.parseCommand(message);
      if (command.Type == Command.CommandType.none)
      {
        return true;
      }

      if (this.Authenticate(sender, command))
      {
        if (command.Name.Equals("acceptPoseAsk"))
        {
          if (recepient == null)
          {
            Log.Warning("Failed to authenticate command {command} from {sender} - recepient not set", command.Name, sender.Name);
            return false;
          }

          var agreement = this.findOrCreateAgreement(recepient.Name);
          Log.Debug("Agreement of {initiator} - added {name}", recepient.Name, sender.Name);
          agreement.Participants.Add(sender.Name);
        }
        else if (command.Name.Equals("useSexPose"))
        {
          Log.Debug("Agreement of {initiator} - fullfilled and removed");
          this.removeAgreement(sender.Name);
        }

        return true;
      }
      else
      {
        Log.Warning("Failed to authenticate message {message} from {sender}", message, sender.Name);
        return false;
      }
    }

    private bool Authenticate(PlayerHandle sender, Command command)
    {
      switch (command.Name)
      {
        case "useSexPose":
          var agreement = this.findAgreement(sender.Name);
          if (agreement == null)
          {
            return false;
          }

          foreach (string participant in command.Arguments.Skip(3).Take(3))
          {
            if (participant.Count() == 0)
            {
              continue;
            }

            string[] participantArguments = participant.Split('=');
            if (participantArguments.Count() != 2)
            {
              return false;
            }

            string participantName = participantArguments.First();
            if (!agreement.Contains(participantName))
            {
              return false;
            }
          }

          return true;

        default:
          AuthenticationTemplate tpl = null;
          if (this.authTemplates.TryGetValue(command.Name, out tpl))
          {
            return tpl.Authenticate(sender, command);
          }
          else
          {
            return true;
          }
      }
    }

    private PoseAgreement findOrCreateAgreement(string initiatorName)
    {
      var agreement = this.findAgreement(initiatorName);
      if (agreement != null)
      {
        return agreement;
      }

      agreement = new PoseAgreement { Initiator = initiatorName };
      this.activeAgreements[initiatorName] = agreement;
      return agreement;
    }

    private PoseAgreement findAgreement(string initiatorName)
    {
      if (this.activeAgreements.ContainsKey(initiatorName))
      {
        return this.activeAgreements[initiatorName];
      } else {
        return null;
      }
    }

    private void removeAgreement(string initiatorName)
    {
      if (this.activeAgreements.ContainsKey(initiatorName))
      {
        this.activeAgreements.Remove(initiatorName);
      }
    }

    private Command parseCommand(string message)
    {
      if (message.StartsWith("~action/"))
      {
        var action = message.Substring("~action/".Length);
        var args = action.Split('|');

        return new Command
        {
          Type = Command.CommandType.action,
          Name = args[0],
          Arguments = args.Skip(1).ToArray()
        };
      }

      return new Command
      {
        Type = Command.CommandType.none
      };
    }
  }
}
