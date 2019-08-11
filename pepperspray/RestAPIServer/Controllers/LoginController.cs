using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Serilog;
using Newtonsoft.Json;
using WatsonWebserver;
using pepperspray.Utils;
using pepperspray.RestAPIServer.Services;
using pepperspray.SharedServices;

namespace pepperspray.RestAPIServer.Controllers
{
  internal class LoginController
  {
    private LoginService loginService = DI.Auto<LoginService>();

    internal LoginController(Server s)
    {
      s.StaticRoutes.Add(HttpMethod.POST, "/login", this.Login);
      s.StaticRoutes.Add(HttpMethod.POST, "/signup", this.SignUp);
      s.StaticRoutes.Add(HttpMethod.POST, "/delete", this.DeleteAccount);
    }

    internal HttpResponse Login(HttpRequest req)
    {
      var str = Encoding.UTF8.GetString(req.Data);
      var parameters = JsonConvert.DeserializeObject<IDictionary<string, string>>(str);

      var name = parameters["username"];
      var passwordHash = parameters["passwordHash"];

      try
      {
        var user = this.loginService.Login(req.GetEndpoint(), name, passwordHash);
        return req.TextResponse(this.loginService.GetLoginResponseText(user));
      }
      catch (LoginService.InvalidPasswordException)
      {
        return req.TextResponse("invalid_password");
      }
      catch (LoginService.NotFoundException)
      {
        return req.TextResponse("not_found");
      }
    }

    internal HttpResponse SignUp(HttpRequest req)
    {
      var str = Encoding.UTF8.GetString(req.Data);
      var parameters = JsonConvert.DeserializeObject<IDictionary<string, string>>(str);

      var username = parameters["username"];
      var passwordHash = parameters["passwordHash"];

      return req.TextResponse(this.loginService.SignUp(req.GetEndpoint(), username, passwordHash) ? "ok" : "fail");
    }

    internal HttpResponse DeleteAccount(HttpRequest req)
    {
      var str = Encoding.UTF8.GetString(req.Data);
      var parameters = JsonConvert.DeserializeObject<IDictionary<string, string>>(str);

      var username = parameters["username"];
      var passwordHash = parameters["passwordHash"];

      try
      {
        this.loginService.DeleteAccount(req.GetEndpoint(), username, passwordHash);
        return req.TextResponse("ok");
      } 
      catch (Exception e)
      {
        Log.Debug("Client {endpoint} failed to delete account: {exception}", req.GetEndpoint(), e);
        if (e is LoginService.NotFoundException || e is LoginService.InvalidPasswordException)
        {
          return req.TextResponse("fail");
        }
        else
        {
          throw e;
        }
      }
    }
  }
}
