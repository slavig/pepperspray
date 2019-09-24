using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Serilog;
using RSG;
using pepperspray.CIO;
using pepperspray.ChatServer.Game;
using pepperspray.ChatServer.Protocol;
using pepperspray.SharedServices;

namespace pepperspray.SharedServices
{
  internal class ChatActionsAuthenticator: IDIService
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

    private class MarryAgreement
    {
      internal string Initiator;
      internal string Recepient;

      internal bool IsBetween(string player1, string player2)
      {
        if (this.Initiator == null || this.Recepient == null)
        {
          return false;
        }

        return (this.Initiator.Equals(player1) && this.Recepient.Equals(player2)) || (this.Initiator.Equals(player2) && this.Recepient.Equals(player1));
      }
    }

    private class Command
    {
      internal enum CommandType
      {
        action,
        action2,
        ask,
        none
      }

      internal CommandType Type;
      internal string Name;
      internal string[] Arguments;
    }

    private class AuthenticationTemplate
    {
      internal int ArgumentIndex;

      internal AuthenticationResult Authenticate(PlayerHandle sender, Command command)
      {
        if (command.Arguments.Count() < this.ArgumentIndex)
        {
          return AuthenticationResult.NotAuthenticated;
        }

        if (ChatActionsAuthenticator.GhostNames.Contains(command.Arguments.ElementAt(this.ArgumentIndex)))
        {
          return AuthenticationResult.Ok;
        }

        var matchingArgument = CharacterService.StripCharacterName(command.Arguments.ElementAt(this.ArgumentIndex));
        if (!matchingArgument.Equals(sender.Name))
        {
          return AuthenticationResult.NotAuthenticated;
        }

        return AuthenticationResult.Ok;
      }
    }
    
    internal enum AuthenticationResult
    {
      Ok,
      NotAuthenticated,
      SexDisabled
    }

    private List<PoseAgreement> activePoseAgreements = new List<PoseAgreement>();
    private List<MarryAgreement> activeMarryAgreements = new List<MarryAgreement>();
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
      { "marry", new AuthenticationTemplate {ArgumentIndex = 0 } }
    };

    private List<string> safePoses = new List<string>
    {
      "wall_1",
      "bed47",
      "bed46",
      "bed23",
      "bed32",
      "bed44",
      "bed9",
      "bed22",
      "bed24",
      "bed31",
      "bed32",
      "chair_G_1",
      "pillow_1",
      "sofa4",
      "sofa6",
    };

    internal static string[] GhostNames = new string[] { "Female Ghost", "Male Ghost" };

    public void Inject()
    {

    }

    internal AuthenticationResult Authenticate(PlayerHandle sender, PlayerHandle recepient, string message)
    {
      var command = this.parseCommand(message);
      if (command.Type == Command.CommandType.none)
      {
        return AuthenticationResult.Ok;
      }

      var authenticationResult = this.authenticateCommand(sender, command);
      if (authenticationResult == AuthenticationResult.Ok)
      {
        if (command.Name.Equals("acceptPoseAsk"))
        {
          if (recepient == null)
          {
            Log.Warning("Failed to authenticate command {command} from {sender} - recepient not set", command.Name, sender.Digest);
            return AuthenticationResult.NotAuthenticated;
          }

          var agreement = this.findPoseAgreement(recepient.Name);
          if (agreement != null)
          {
            Log.Debug("Agreement of {initiator} - added {name}", recepient.Digest, sender.Digest);
            agreement.Participants.Add(sender.Name);
          }
          else
          {
            return AuthenticationResult.NotAuthenticated;
          }
        }
        else if (command.Name.Equals("askForPose"))
        {
          Log.Debug("Agreement containing {player} removed, moving to next agreement", sender.Digest);
          this.removePoseAgreement(sender.Name);
          this.findOrCreatePoseAgreement(sender.Name);
        }
        else if (command.Name.Equals("stopSexPS"))
        {
          Log.Debug("Agreement containing {player} removed due to pose stop request", sender.Digest);
          this.removePoseAgreement(sender.Name);
        }
        else if (command.Name.Equals("marry"))
        {
          Log.Debug("Marry agreement of {initiator} - added", sender.Digest);
          this.findOrCreateMarryAgreement(sender.Name);
        }
        else if (command.Name.Equals("marry_agree"))
        {
          var agreement = this.findMarryAgreement(recepient.Name);
          if (agreement != null)
          {
            Log.Debug("Marry agreement of {initiator} - agreed upon from {sender}", recepient.Digest, sender.Digest);
            agreement.Recepient = sender.Name;
          }
          else
          {
            Log.Debug("Player {recepient} is unable to agree to marry proposal of {initiator} - couldn't find agreement", sender.Digest, recepient.Digest);
            return AuthenticationResult.NotAuthenticated;
          }
        }

        return AuthenticationResult.Ok;
      }
      else
      {
        Log.Warning("Failed to authenticate message {message} from {sender}", message, sender.Digest);
        return authenticationResult;
      }
    }

    private AuthenticationResult authenticateCommand(PlayerHandle sender, Command command)
    {
#if !DEBUG
      if (sender.AdminOptions.HasFlag(AdminFlags.DisabledAuthenticator))
      {
        return AuthenticationResult.Ok;
      }
#endif

      switch (command.Name)
      {
        case "useSexPose":
          if (this.checkIfPoseIsForbiddenInLobby(sender, command))
          {
            return AuthenticationResult.SexDisabled;
          }

          var agreement = this.findPoseAgreement(sender.Name);
          if (agreement == null)
          {
            return AuthenticationResult.NotAuthenticated;
          }

          foreach (string participantRaw in command.Arguments.Skip(3).Take(3))
          {
            if (participantRaw.Count() == 0)
            {
              continue;
            }

            var participant = CharacterService.StripCharacterName(participantRaw);

            string[] participantArguments = participant.Split('=');
            if (participantArguments.Count() != 2)
            {
              return AuthenticationResult.NotAuthenticated;
            }

            string participantName = participantArguments.First();
            if (!agreement.Contains(participantName))
            {
              return AuthenticationResult.NotAuthenticated;
            }
          }

          return AuthenticationResult.Ok;

        case "stopSexPS":
          foreach (string participantRaw in command.Arguments.Skip(1))
          {
            var participant = CharacterService.StripCharacterName(participantRaw);
            if (participant.Equals(sender.Name)) 
            {
              return AuthenticationResult.Ok;
            }
          }

          return AuthenticationResult.NotAuthenticated;

        case "askForPose":
          if (this.checkIfPoseIsForbiddenInLobby(sender, command))
          {
            return AuthenticationResult.SexDisabled;
          }
          else
          {
            return this.authenticateByTemplate(sender, command);
          }

        default:
          return this.authenticateByTemplate(sender, command);
      }
    }

    internal IPromise<Nothing> PlayerLoggedOff(PlayerHandle player)
    {
      if (player.Name != null)
      {
        Log.Debug("Removing agreements containing {player} - logged off", player.Digest);
        this.removePoseAgreement(player.Name);
        this.removeMarryAgreement(player.Name);
      }

      return Nothing.Resolved();
    }

    internal bool AuthenticateAndFullfillMarryAgreement(string participant1, string participant2)
    {
      lock(this)
      {
        foreach (var agreement in this.activeMarryAgreements.ToArray())
        {
          if (agreement.IsBetween(participant1, participant2))
          {
            this.activeMarryAgreements.Remove(agreement);
            return true;
          }
        }
      }

      return false;
    }

    private AuthenticationResult authenticateByTemplate(PlayerHandle sender, Command command)
    {
      AuthenticationTemplate tpl = null;
      if (this.authTemplates.TryGetValue(command.Name, out tpl))
      {
        return tpl.Authenticate(sender, command);
      }
      else
      {
        return AuthenticationResult.Ok;
      }

    }

    private PoseAgreement findOrCreatePoseAgreement(string initiatorName)
    {
      lock (this)
      {
        var agreement = this.findPoseAgreement(initiatorName);
        if (agreement != null)
        {
          return agreement;
        }

        agreement = new PoseAgreement { Initiator = initiatorName };
        this.activePoseAgreements.Add(agreement);

        return agreement;
      }
    }

    private PoseAgreement findPoseAgreement(string initiatorName)
    {
      lock(this)
      {
        foreach (var agreement in this.activePoseAgreements)
        {
          if (agreement.Contains(initiatorName))
          {
            return agreement;
          }
        }
      }

      return null;
    }

    private void removePoseAgreement(string playerName)
    {
      lock(this)
      {
        foreach (var agreement in this.activePoseAgreements.ToArray())
        {
          if (agreement.Contains(playerName))
          {
            this.activePoseAgreements.Remove(agreement);
          }
        }
      }
    }

    private MarryAgreement findOrCreateMarryAgreement(string initiatorName)
    {
      lock(this)
      {
        var agreement = this.findMarryAgreement(initiatorName);

        if (agreement == null)
        {
          agreement = new MarryAgreement
          {
            Initiator = initiatorName
          };

          this.activeMarryAgreements.Add(agreement);
        }

        return agreement;
      }
    }

    private MarryAgreement findMarryAgreement(string initiatorName)
    {
      lock(this)
      {
        foreach (var argreement in this.activeMarryAgreements)
        {
          if (argreement.Initiator.Equals(initiatorName))
          {
            return argreement;
          }
        }
      }

      return null;
    }

    private void removeMarryAgreement(string participantName)
    {
      lock(this)
      {
        foreach (var agreement in this.activeMarryAgreements.ToArray())
        {
          if (agreement.Initiator.Equals(participantName) || (agreement.Recepient != null && agreement.Recepient.Equals(participantName)))
          {
            this.activeMarryAgreements.Remove(agreement);
          }
        }
      }
    }

    private bool checkIfPoseIsForbiddenInLobby(PlayerHandle sender, Command command)
    {
      if (sender.CurrentLobby != null && sender.CurrentLobby.IsUserRoom && !sender.CurrentLobby.UserRoom.IsSexAllowed)
      {
        string poseIdentifier = null;
        switch (command.Name)
        {
          case "askForPose":
            poseIdentifier = command.Arguments.ElementAtOrDefault(1);
            break;
          case "useSexPose":
            poseIdentifier = command.Arguments.ElementAtOrDefault(command.Arguments.Length - 2);
            foreach (var arg in command.Arguments.Skip(3).Take(3))
            {
              var components = arg.Split('=');
              if (components.Count() > 1 && components.Last() != poseIdentifier)
              {
                return true;
              }
            }
            break;
        }

        return !this.safePoses.Contains(poseIdentifier);
      }

      return false;
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
      else if (message.StartsWith("~ask/"))
      {
        var action = message.Substring("~ask/".Length);
        var args = action.Split('|');

        return new Command
        {
          Type = Command.CommandType.ask,
          Name = args[0],
          Arguments = args.Skip(1).ToArray()
        };
      }
      else if (message.StartsWith("~action2/"))
      {
        var action = message.Substring("~action2/".Length);
        var args = action.Split('|');

        return new Command
        {
          Type = Command.CommandType.action2,
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
