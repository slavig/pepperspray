using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Serilog;
using Newtonsoft.Json;
using WatsonWebserver;
using BitArmory.ReCaptcha;
using pepperspray.Utils;
using pepperspray.RestAPIServer.Services;
using pepperspray.SharedServices;

namespace pepperspray.RestAPIServer.Controllers
{
  internal class LoginController
  {
    internal class CaptchaValidationFailedException: Exception {}

    private Configuration config = DI.Get<Configuration>();
    private LoginService loginService = DI.Get<LoginService>();
    private Dictionary<string, DateTime> loginAttempts = new Dictionary<string, DateTime>();
    private ReCaptchaService recapchaService = new ReCaptchaService();

    internal LoginController(Server s)
    {
      s.StaticRoutes.Add(HttpMethod.POST, "/login", this.Login);
      s.StaticRoutes.Add(HttpMethod.POST, "/changepassword", this.ChangePassword);
      s.StaticRoutes.Add(HttpMethod.POST, "/forgotpassword", this.ForgotPassword);
      s.StaticRoutes.Add(HttpMethod.POST, "/signup", this.SignUp);
      s.StaticRoutes.Add(HttpMethod.POST, "/delete", this.DeleteAccount);

      Log.Information("Setting {url} as cross-origin allowed", this.crossOriginAllowedAddress());
      s.StaticRoutes.Add(HttpMethod.OPTIONS, "/login", this.CrossDomainAccessConfig);
      s.StaticRoutes.Add(HttpMethod.OPTIONS, "/changepassword", this.CrossDomainAccessConfig);
      s.StaticRoutes.Add(HttpMethod.OPTIONS, "/forgotpassword", this.CrossDomainAccessConfig);
      s.StaticRoutes.Add(HttpMethod.OPTIONS, "/signup", this.CrossDomainAccessConfig);
      s.StaticRoutes.Add(HttpMethod.OPTIONS, "/delete", this.CrossDomainAccessConfig);
    }

    internal HttpResponse Login(HttpRequest req)
    {
      try
      {
        // this.throttleLoginAttempt(req.SourceIp);

        var str = Encoding.UTF8.GetString(req.Data);
        var parameters = JsonConvert.DeserializeObject<IDictionary<string, string>>(str);

        var name = parameters["username"];
        var passwordHash = parameters["passwordHash"];
        this.validateInvisibleRecaptcha(req, parameters["captchaToken"]).Wait();

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
      catch (AggregateException)
      {
        return req.TextResponse("captcha");
      }
      catch (KeyNotFoundException)
      {
        return req.FailureResponse();
      }
    }


    internal HttpResponse ChangePassword(HttpRequest req)
    {
      try
      {
        var str = Encoding.UTF8.GetString(req.Data);
        var parameters = JsonConvert.DeserializeObject<IDictionary<string, string>>(str);

        var token = parameters["token"];
        var passwordHash = parameters["passwordHash"];
        var newPasswordHash = parameters["newPasswordHash"];

        this.loginService.ChangePassword(req.GetEndpoint(), token, passwordHash, newPasswordHash);
        return req.TextResponse("ok");
      }
      catch (LoginService.InvalidPasswordException)
      {
        return req.TextResponse("invalid_password");
      }
      catch (LoginService.NotFoundException)
      {
        return req.TextResponse("not_found");
      } 
      catch (KeyNotFoundException)
      {
        return req.FailureResponse();
      }
    }

    internal HttpResponse ForgotPassword(HttpRequest req)
    {
      try
      {
        var str = Encoding.UTF8.GetString(req.Data);
        var parameters = JsonConvert.DeserializeObject<IDictionary<string, string>>(str);

        var name = parameters["username"];
        this.validateRecaptcha(req, parameters["captchaToken"]).Wait();

        this.loginService.ForgotPassword(req.GetEndpoint(), name);
        return req.TextResponse("ok");
      }
      catch (LoginService.InvalidPasswordException)
      {
        return req.TextResponse("invalid_password");
      }
      catch (LoginService.NotFoundException)
      {
        return req.TextResponse("not_found");
      } 
      catch (AggregateException)
      {
        return req.TextResponse("captcha");
      }
      catch (KeyNotFoundException)
      {
        return req.FailureResponse();
      }
    }

    internal HttpResponse SignUp(HttpRequest req)
    {
      var str = Encoding.UTF8.GetString(req.Data);
      var parameters = JsonConvert.DeserializeObject<IDictionary<string, string>>(str);

      var username = parameters["username"];
      var passwordHash = parameters["passwordHash"];

      try
      {
        this.validateRecaptcha(req, parameters["captchaToken"]).Wait();
      } 
      catch (AggregateException)
      {
        return req.TextResponse("captcha");
      }

      try
      {
        return req.TextResponse(this.loginService.SignUp(req.GetEndpoint(), username, passwordHash) != null ? "ok" : "fail");
      }
      catch (Exception e)
      {
        Request.HandleException(req, e);
        throw e;
      }
    }

    internal HttpResponse DeleteAccount(HttpRequest req)
    {
      var str = Encoding.UTF8.GetString(req.Data);
      var parameters = JsonConvert.DeserializeObject<IDictionary<string, string>>(str);

      var token = parameters["token"];
      var passwordHash = parameters["passwordHash"];

      try
      {
        this.loginService.DeleteAccount(req.GetEndpoint(), token, passwordHash);
        return req.TextResponse("ok");
      } 
      catch (Exception e)
      {
        Request.HandleException(req, e);
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

    internal HttpResponse CrossDomainAccessConfig(HttpRequest req)
    {
      return new HttpResponse(req, 204, new Dictionary<string, string>
      {
        { "Access-Control-Allow-Methods", "POST" },
        { "Access-Control-Allow-Origin", this.crossOriginAllowedAddress() },
        { "Access-Control-Allow-Headers", "Accept, Accept-CH, Accept-Charset, Accept-Datetime, Accept-Encoding, Accept-Ext, Accept-Features, Accept-Language, Accept-Params, Accept-Ranges, Access-Control-Allow-Credentials, Access-Control-Allow-Headers, Access-Control-Allow-Methods, Access-Control-Allow-Origin, Access-Control-Expose-Headers, Access-Control-Max-Age, Access-Control-Request-Headers, Access-Control-Request-Method, Age, Allow, Alternates, Authentication-Info, Authorization, C-Ext, C-Man, C-Opt, C-PEP, C-PEP-Info, CONNECT, Cache-Control, Compliance, Connection, Content-Base, Content-Disposition, Content-Encoding, Content-ID, Content-Language, Content-Length, Content-Location, Content-MD5, Content-Range, Content-Script-Type, Content-Security-Policy, Content-Style-Type, Content-Transfer-Encoding, Content-Type, Content-Version, Cookie, Cost, DAV, DELETE, DNT, DPR, Date, Default-Style, Delta-Base, Depth, Derived-From, Destination, Differential-ID, Digest, ETag, Expect, Expires, Ext, From, GET, GetProfile, HEAD, HTTP-date, Host, IM, If, If-Match, If-Modified-Since, If-None-Match, If-Range, If-Unmodified-Since, Keep-Alive, Label, Last-Event-ID, Last-Modified, Link, Location, Lock-Token, MIME-Version, Man, Max-Forwards, Media-Range, Message-ID, Meter, Negotiate, Non-Compliance, OPTION, OPTIONS, OWS, Opt, Optional, Ordering-Type, Origin, Overwrite, P3P, PEP, PICS-Label, POST, PUT, Pep-Info, Permanent, Position, Pragma, ProfileObject, Protocol, Protocol-Query, Protocol-Request, Proxy-Authenticate, Proxy-Authentication-Info, Proxy-Authorization, Proxy-Features, Proxy-Instruction, Public, RWS, Range, Referer, Refresh, Resolution-Hint, Resolver-Location, Retry-After, Safe, Sec-Websocket-Extensions, Sec-Websocket-Key, Sec-Websocket-Origin, Sec-Websocket-Protocol, Sec-Websocket-Version, Security-Scheme, Server, Set-Cookie, Set-Cookie2, SetProfile, SoapAction, Status, Status-URI, Strict-Transport-Security, SubOK, Subst, Surrogate-Capability, Surrogate-Control, TCN, TE, TRACE, Timeout, Title, Trailer, Transfer-Encoding, UA-Color, UA-Media, UA-Pixels, UA-Resolution, UA-Windowpixels, URI, Upgrade, User-Agent, Variant-Vary, Vary, Version, Via, Viewport-Width, WWW-Authenticate, Want-Digest, Warning, Width, X-Content-Duration, X-Content-Security-Policy, X-Content-Type-Options, X-CustomHeader, X-DNSPrefetch-Control, X-Forwarded-For, X-Forwarded-Port, X-Forwarded-Proto, X-Frame-Options, X-Modified, X-OTHER, X-PING, X-PINGOTHER, X-Powered-By, X-Requested-With" },
      });
    }

    private string crossOriginAllowedAddress()
    {
      return "http://" + this.config.CrossOriginAddress + (this.config.CrossOriginPort != 0 ? ":" + this.config.CrossOriginPort.ToString() : "");
    }

    private void throttleLoginAttempt(string sourceIp)
    {
      var previousAttempt = DateTime.MinValue;
      lock (this)
      {
        this.loginAttempts.TryGetValue(sourceIp, out previousAttempt);
      }

      var delta = DateTime.Now - previousAttempt;
      if (delta.Seconds < this.config.LoginAttemptThrottle)
      {
        Log.Information("Login attempt throttling - ip {ip} attempted login {seconds} s. before", sourceIp, delta.Seconds);
        Thread.Sleep(TimeSpan.FromSeconds(this.config.LoginAttemptThrottle - delta.Seconds));
      }

      lock(this)
      {
        this.loginAttempts[sourceIp] = DateTime.Now;
      }
    }

    private async Task<bool> validateInvisibleRecaptcha(HttpRequest req, string token)
    {
      if (this.config.Recaptcha.Enabled)
      {
        var result = await this.recapchaService.Verify3Async(token, req.SourceIp, this.config.Recaptcha.InvisibleSecret);
        if (result.IsSuccess)
        {
          return true;
        }
        else
        {
          Log.Warning("Client {endpoint} failed to provide valid captcha: {success}, {score}", result.IsSuccess, result.Score);
          throw new CaptchaValidationFailedException();
        }
      } else
      {
        return true;
      }
    }

    private async Task<bool> validateRecaptcha(HttpRequest req, string token)
    {
      if (this.config.Recaptcha.Enabled)
      {
        if (await this.recapchaService.Verify2Async(token, req.SourceIp, this.config.Recaptcha.VisibleSecret))
        {
          return true;
        }
        else
        {
          Log.Warning("Client {endpoint} failed to provide valid captcha");
          throw new CaptchaValidationFailedException();
        }
      } 
      else
      {
        return true;
      }
    }
  }
}
