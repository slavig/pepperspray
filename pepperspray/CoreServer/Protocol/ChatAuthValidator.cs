using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Serilog;
using pepperspray.CoreServer.Game;

namespace pepperspray.CoreServer.Protocol
{
  internal class ChatMessageAuthenticator
  {
    private class Agreement
    {
      internal string[] PlayerNames;
      internal bool Successfull;
      internal DateTime Date;

      internal bool Contains(string[] names)
      {
        foreach (string name in names)
        {
          if (!this.PlayerNames.Contains(name))
          {
            return false;
          }
        }

        return true;
      }
    }

    private List<Agreement> activeAgreements = new List<Agreement>();

    internal ChatMessageAuthenticator()
    {

    }

    internal bool ShouldProcess(PlayerHandle sender, PlayerHandle recepient, string message)
    {
      this.cleanupAgreements();

      var command = this.ParseCommand(message);
      if (this.Authenticate(sender, command))
      {
        if (command.Type == Command.CommandType.action)
        {
          if (command.Name.Equals("askForPose")) {
            if (recepient == null)
            {
              return false;
            }

            var agreement = this.findOrCreateAgreement(new string[] { sender.Name, recepient.Name });
          }
          else if (command.Name.Equals("acceptPoseAsk"))
          {
            if (recepient == null)
            {
              return false;
            }

            var agreement = this.findOrCreateAgreement(new string[] { sender.Name, recepient.Name });
            agreement.Successfull = true;
            agreement.Date = DateTime.Now;
          }
        }

        return true;
      } else
      {
        return false;
      }
    }

    private bool Authenticate(PlayerHandle sender, Command command)
    {
      switch (command.Type)
      {
        case Command.CommandType.action:
          switch (command.Name)
          {
            case "walk":
              if (command.Arguments.Count() < 4)
              {
                return false;
              }

              if (command.Arguments[3] != sender.Name)
              {
                return false;
              }

              return true;
            case "askForPose":
              if (command.Arguments.Count() < 3)
              {
                return false;
              }

              if (command.Arguments[2] != sender.Name)
              {
                return false;
              }

              return true;
            case "acceptPoseAsk":
              if (command.Arguments.Count() < 1)
              {
                return false;
              }

              if (command.Arguments[0] != sender.Name)
              {
                return false;
              }

              return true;
            case "useSexPose":
              foreach (string participant in command.Arguments.Skip(3).Take(3))
              {
                string[] participantArguments = participant.Split('=');

              }

              return true;
            default:
              return true;
          }
        default:
          return true;
      }
    }

    private Agreement findOrCreateAgreement(string[] playerNames)
    {
      var agreement = this.findAgreement(playerNames);
      if (agreement != null)
      {
        return agreement;
      }

      return new Agreement
      {
        PlayerNames = playerNames,
      };
    }

    private Agreement findAgreement(string[] playerNames)
    {
      foreach (var agreement in this.activeAgreements)
      {
        if (agreement.Contains(playerNames))
        {
          return agreement;
        }
      }

      return null;
    }

    private bool checkAgreement(string[] playerNames)
    {
      var agreement = this.findAgreement(playerNames);
      if (agreement != null)
      {
        return agreement.Successfull;
      } else
      {
        return false;
      }
    }

    private void cleanupAgreements()
    {
      foreach (var agreement in this.activeAgreements.ToList())
      {
        if ((DateTime.Now - agreement.Date).Minutes > 5)
        {
          this.activeAgreements.Remove(agreement);
        }
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

    private Command ParseCommand(string message)
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
