using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Serilog;
using RSG;
using pepperspray.ChatServer.Game;
using pepperspray.CIO;

namespace pepperspray.ChatServer.Services
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

    private List<PoseAgreement> activeAgreements = new List<PoseAgreement>();
    private Dictionary<string, AuthenticationTemplate> authTemplates = new Dictionary<string, AuthenticationTemplate>
    {
      { "walk", new AuthenticationTemplate {ArgumentIndex  = 3} },
      { "askForPose", new AuthenticationTemplate {ArgumentIndex  = 2} },
      { "acceptPoseAsk", new AuthenticationTemplate {ArgumentIndex  = 0} },
      { "runPropAction", new AuthenticationTemplate {ArgumentIndex  = 1} },
      { "takeFood", new AuthenticationTemplate {ArgumentIndex  = 0} },
      { "eatFood", new AuthenticationTemplate {ArgumentIndex  = 0} },
      { "acceptDance", new AuthenticationTemplate {ArgumentIndex  = 0} },
      { "ApplyCoupleDance", new AuthenticationTemplate {ArgumentIndex  = 0} },
      { "ApplyStopCoupleDance", new AuthenticationTemplate {ArgumentIndex  = 0} },
    };

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

          var agreement = this.findAgreement(recepient.Name);
          if (agreement != null)
          {
            Log.Debug("Agreement of {initiator} - added {name}", recepient.Name, sender.Name);
            agreement.Participants.Add(sender.Name);
          }
          else
          {
            return false;
          }
        }
        else if (command.Name.Equals("askForPose"))
        {
          Log.Debug("Agreement containing {player} removed, moving to next agreement", sender.Name);
          this.removeAgreement(sender.Name);
          this.findOrCreateAgreement(sender.Name);
        }
        else if (command.Name.Equals("stopSexPS"))
        {
          Log.Debug("Agreement containing {player} removed due to pose stop request", sender.Name);
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

        case "stopSexPS":
          foreach (string participant in command.Arguments.Skip(1))
          {
            if (participant.Equals(sender.Name)) 
            {
              return true;
            }
          }

          return false;

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

    internal IPromise<Nothing> PlayerLoggedOff(PlayerHandle player)
    {
      Log.Debug("Removing agreements containing {name} - logged off", player.Name);
      this.removeAgreement(player.Name);

      return Nothing.Resolved();
    }

    private PoseAgreement findOrCreateAgreement(string initiatorName)
    {
      lock (this)
      {
        var agreement = this.findAgreement(initiatorName);
        if (agreement != null)
        {
          return agreement;
        }

        agreement = new PoseAgreement { Initiator = initiatorName };
        this.activeAgreements.Add(agreement);

        return agreement;
      }
    }

    private PoseAgreement findAgreement(string initiatorName)
    {
      lock(this)
      {
        foreach (var agreement in this.activeAgreements)
        {
          if (agreement.Contains(initiatorName))
          {
            return agreement;
          }
        }
      }

      return null;
    }

    private void removeAgreement(string playerName)
    {
      lock(this)
      {
        foreach (var agreement in this.activeAgreements.ToArray())
        {
          if (agreement.Contains(playerName))
          {
            this.activeAgreements.Remove(agreement);
          }
        }
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
