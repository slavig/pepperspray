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
  internal class BlacklistController
  {
    private BlacklistService blacklistService = DI.Get<BlacklistService>();

    internal BlacklistController(Server s)
    {
      s.StaticRoutes.Add(HttpMethod.POST, "/getignores", this.GetIgnores);
      s.StaticRoutes.Add(HttpMethod.POST, "/ignore", this.Ignore);
      s.StaticRoutes.Add(HttpMethod.POST, "/unignore", this.Unignore);
    }

    internal HttpResponse GetIgnores(HttpRequest req)
    {
      try
      {
        var id = Convert.ToUInt32(req.GetFormParameter("uid"));

        var ignoredCharacters = this.blacklistService.GetCharactersIgnoredByUser(req.GetBearerToken());
        var blacklist = ignoredCharacters.Select((ch) => new Dictionary<string, object>
        {
          { "id", ch.Id },
          { "n", ch.Name },
          { "s", ch.Sex },
        });

        var ignoredByCharacters = this.blacklistService.GetCharactersWhichIgnoreCharacter(req.GetBearerToken(), id);
        var reverseBlacklist = ignoredByCharacters.Select((ch) => new Dictionary<string, object>
        {
          { "id", ch.Id },
          { "n", ch.Name },
          { "s", ch.Sex },
        });

        return req.JsonResponse(new Dictionary<string, object>
        {
          { "ignoredBy", reverseBlacklist },
          { "blacklist", blacklist },
        });
      }
      catch (Exception e)
      {
        Request.HandleException(req, e);

        if (e is LoginService.InvalidTokenException || e is ArgumentException)
        {
          return req.FailureResponse();
        }
        else
        {
          throw e;
        }
      }
    }

    internal HttpResponse Ignore(HttpRequest req)
    {
      try
      {
        var id = Convert.ToUInt32(req.GetFormParameter("iid"));
        this.blacklistService.IgnoreCharacter(req.GetBearerToken(), id);

        return req.TextResponse("ok");
      }
      catch (Exception e)
      {
        Request.HandleException(req, e);

        if (e is LoginService.InvalidTokenException || e is ArgumentException || e is BlacklistService.AlreadySubmittedException || e is FormatException)
        {
          return req.FailureResponse();
        }
        else
        {
          throw e;
        }
      }
    }

    internal HttpResponse Unignore(HttpRequest req)
    {
      try
      {
        var id = Convert.ToUInt32(req.GetFormParameter("iid"));
        this.blacklistService.UnignoreCharacter(req.GetBearerToken(), id);

        return req.TextResponse("ok");
      }
      catch (Exception e)
      {
        Request.HandleException(req, e);

        if (e is LoginService.InvalidTokenException || e is ArgumentException || e is BlacklistService.AlreadySubmittedException || e is FormatException)
        {
          return req.FailureResponse();
        }
        else
        {
          throw e;
        }
      }
    }
  }
}
