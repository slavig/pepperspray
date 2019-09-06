using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

using Serilog;
using RSG;
using Newtonsoft.Json;
using pepperspray.CIO;
using pepperspray.SharedServices;
using pepperspray.Utils;

namespace pepperspray.LoginServer
{
  internal class LoginServerListener: IDIService
  {
    internal class NotFoundException : Exception { }

    private Configuration config;
    private LoginService loginService;

    private Dictionary<string, Client> loggedClients = new Dictionary<string, Client>();

    public void Inject()
    {
      this.loginService = DI.Get<LoginService>();
      this.config = DI.Get<Configuration>();
    }

    internal IPromise<Nothing> Listen()
    {
      var address = this.config.LoginServerAddress;
      var port = this.config.LoginServerPort;

      var task = new CIO.CIOListener("LoginServer")
        .Bind(address, port)
        .Incoming()
        .Map(connection => new Client(connection))
        .Map(client =>
        {
          return client.EventStream().Map(ev =>
          {
            Log.Debug("LoginServer client {endpoint} processing command {cmd}", client.Endpoint, JsonConvert.SerializeObject(ev));

            switch (ev.First().ToString())
            {
              case "addr":
                if (ev.Count() < 2)
                {
                  throw new Exception("invalid request");
                }

                var addr = ev.ElementAt(1).ToString() + ":0";
                client.Endpoint = addr.Substring("::ffff:".Length);
                break;

              case "retokennect":
                if (ev.Count() < 2)
                {
                  throw new Exception("invalid request");
                }

                var token = ev.ElementAt(1).ToString();
                try
                {
                  var previousClient = this.FindClient(token);
                  client.Token = token;
                  client.LoggedCharacter = previousClient.LoggedCharacter;
                }
                catch (NotFoundException) {}

                this.loggedClients[token] = client;
                break;

              case "login request":
                if (ev.Count() < 4)
                {
                  throw new Exception("invalid request");
                }

                var username = ev.ElementAt(1).ToString();
                var passwordHash = ev.ElementAt(2).ToString();
                var protocolVersion = ev.ElementAt(3).ToString();

                try
                {
                  this.loginService.CheckProtocolVersion(protocolVersion);

                  var user = this.loginService.Login(client.Endpoint, username, passwordHash);
                  lock(this)
                  {
                    this.loggedClients[user.Token] = client;
                  }

                  client.Emit("login response", this.loginService.GetLoginResponseText(user));
                }
                catch (LoginService.InvalidPasswordException)
                {
                  client.Emit("login response", this.loginService.GetLoginFailedResponseText());
                }
                catch (LoginService.EndpointBannedException)
                {
                  client.Emit("login response", this.loginService.GetBannedResponseText());
                }
                catch (LoginService.NotFoundException)
                {
#if DEBUG
                    this.loginService.SignUp(client.Endpoint, username, passwordHash);
#endif

                  client.Emit("login response", this.loginService.GetLoginFailedResponseText());
                }
                catch (LoginService.UnsupportedProtocolVersionException)
                {
                  client.Emit("login response", this.loginService.GetUnsupportedProtocolVersionResponseText());
                }
                break;
            }

            return Nothing.Resolved();
          }).Catch(e =>
          {
            Log.Warning("Caught exception during LoginServer: {exception}, terminating connection of {endpoint}/{token}", e, client.Endpoint, client.Token);
            if (client.Token != null && this.loggedClients.ContainsKey(client.Token))
            {
              this.loggedClients.Remove(client.Token);
            }

            client.Terminate();
          });
        });

      return task.Then(a => Nothing.Resolved());
    }

    internal bool HasClient(string token)
    {
      lock (this)
      {
        return this.loggedClients.ContainsKey(token);
      }
    }

    internal Client FindClient(string token)
    {
      lock (this)
      {
        Client client = null;
        if (this.loggedClients.TryGetValue(token, out client))
        {
          return client;
        }
        else
        {
          throw new NotFoundException();
        }
      }
    }

    internal void AssociateCharacter(string token, Character character)
    {
      try
      {
        var client = this.FindClient(token);
        client.LoggedCharacter = character;
      }
      catch (NotFoundException)
      { 
      }
    }

    internal IPromise<Nothing> Emit(uint characterId, params object[] data)
    {
      var promises = new List<IPromise<Nothing>>();
      lock(this)
      {
        foreach (var client in this.loggedClients.ToArray())
        {
          if (client.Value.LoggedCharacter != null && client.Value.LoggedCharacter.Id == characterId)
          {
            try
            {
              promises.Add(this.Emit(client.Key, data));
            }
            catch (NotFoundException)
            {

            }
          }
        }
      }

      if (promises.Count() == 0)
      {
        throw new NotFoundException();
      } else
      {
        return Combined.Promise(promises);
      }
    }

    internal IPromise<Nothing> Emit(string token, params object[] data)
    {
      lock (this)
      {
        Client client = null;
        if (this.loggedClients.TryGetValue(token, out client))
        {
          try
          {
            return client.Emit(data.ToArray());
          }
          catch (Exception)
          {
            this.loggedClients.Remove(token);
            throw new NotFoundException();
          }
        }
        else
        {
          throw new NotFoundException();
        }
      }
    }

    internal IPromise<Nothing> Broadcast(params object[] data)
    {
      var promises = new List<IPromise<Nothing>>();
      lock(this)
      {
        foreach (var token in this.loggedClients.Keys)
        {
          try
          {
            this.Emit(token, data);
          }
          catch (NotFoundException) { }
        }
      }

      return Combined.Promise(promises);
    }
  }
}
