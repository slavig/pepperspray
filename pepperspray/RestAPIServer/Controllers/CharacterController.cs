using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

using Serilog;
using Newtonsoft.Json;
using WatsonWebserver;
using pepperspray.Utils;
using pepperspray.RestAPIServer.Services;
using pepperspray.SharedServices;

namespace pepperspray.RestAPIServer.Controllers
{
  internal class CharacterController
  {
    private CharacterService characterService = DI.Get<CharacterService>();

    internal CharacterController(Server s)
    {
      s.StaticRoutes.Add(HttpMethod.POST, "/checkname", this.CheckName);
      s.StaticRoutes.Add(HttpMethod.POST, "/createchar", this.CreateChar);
      s.StaticRoutes.Add(HttpMethod.POST, "/changechar", this.ChangeChar);
      s.StaticRoutes.Add(HttpMethod.POST, "/savechar", this.SaveChar);
      s.StaticRoutes.Add(HttpMethod.POST, "/deletechar", this.DeleteChar);

      s.StaticRoutes.Add(HttpMethod.POST, "/getprofile", this.GetProfile);
      s.StaticRoutes.Add(HttpMethod.POST, "/saveprofile", this.SaveProfile);
      s.StaticRoutes.Add(HttpMethod.POST, "/wedding", this.Wedding);
      s.StaticRoutes.Add(HttpMethod.POST, "/divorce", this.Divorce);

      s.StaticRoutes.Add(HttpMethod.POST, "/getdefaultchar", this.GetDefaultChar);
      s.StaticRoutes.Add(HttpMethod.POST, "/getbotchar", this.GetBotChar);
    }

    internal HttpResponse CheckName(HttpRequest req)
    {
      string name, auth;
      try
      {
        name = req.GetFormParameter("name");
        auth = req.GetBearerToken();
      }
      catch (ArgumentException)
      {
        return req.FailureResponse();
      }

      try
      {
        this.characterService.CheckName(name);

        return req.TextResponse("ok");
      }
      catch (CharacterService.InvalidNameException)
      {
        return req.TextResponse("bad_name");
      }
      catch (CharacterService.NameTakenException)
      {
        return req.TextResponse("taken_name");
      }
    }

    internal HttpResponse CreateChar(HttpRequest req)
    {
      try
      {
        string name = req.GetFormParameter("name");
        string sex = req.GetFormParameter("sex");
        string token = req.GetBearerToken();

        this.characterService.CheckName(name);
        var character = this.characterService.CreateCharacter(name, sex, token);
        return req.JsonResponse(new Dictionary<string, object> {
          { "id", character.Id },
          { "token", token },
        });
      }
      catch (ArgumentException)
      {
        return req.FailureResponse();
      }
      catch (CharacterService.InvalidNameException)
      {
        return req.JsonResponse("error2");
      }
      catch (CharacterService.NameTakenException)
      {
        return req.JsonResponse("error3");
      }
    }

    internal HttpResponse ChangeChar(HttpRequest req)
    {
      try
      {
        var name = req.GetFormParameter("newname");
        var sex = req.GetFormParameter("newsex");

        var id = Convert.ToUInt32(req.GetFormParameter("uid"));
        var token = req.GetBearerToken();

        this.characterService.UpdateCharacter(id, token, name, sex);
        return req.JsonResponse(new Dictionary<string, object> { { "id", id }, { "token", token } });
      }
      catch (CharacterService.InvalidNameException)
      {
        return req.JsonResponse("error2");
      }
      catch (CharacterService.NameTakenException)
      {
        return req.JsonResponse("error3");
      }
      catch (Exception e)
      {
        Request.HandleException(req, e);
        if (e is FormatException || e is ArgumentException || e is CharacterService.NotAuthorizedException)
        {
          return req.FailureResponse();
        }
        else
        {
          throw e;
        }
      }
    }

    internal HttpResponse SaveChar(HttpRequest req)
    {
      try
      {
        var uid = Convert.ToUInt32(req.GetFormParameter("id"));
        string token = req.GetBearerToken();
        string data = req.GetFormParameter("data");

        this.characterService.UpdateCharacterAppearance(token, uid, data);
        return req.TextResponse("ok");
      }
      catch (Exception e)
      {
        Request.HandleException(req, e);
        if (e is FormatException || e is ArgumentException || e is CharacterService.NotAuthorizedException || e is CharacterService.NotFoundException)
        {
          return req.FailureResponse();
        }
        else
        {
          throw e;
        }
      }
    }

    internal HttpResponse DeleteChar(HttpRequest req)
    {
      try
      {
        var uid = Convert.ToUInt32(req.GetFormParameter("id"));
        var name = req.GetFormParameter("name");
        var token = req.GetBearerToken();

        this.characterService.DeleteCharacter(token, uid, name);
        return req.JsonResponse(new Dictionary<string, string> { { "token", token } });
      }
      catch (Exception e)
      {
        Request.HandleException(req, e);
        if (e is FormatException || e is ArgumentException || e is CharacterService.NotAuthorizedException || e is CharacterService.NotFoundException)
        {
          return req.FailureResponse();
        }
        else
        {
          throw e;
        }
      }
    }

    internal HttpResponse GetProfile(HttpRequest req)
    {
      try
      {
        var uid = Convert.ToUInt32(req.GetFormParameter("id"));

        return req.TextResponse(this.characterService.GetCharacterProfile(uid));
      }
      catch (Exception e) {
        Request.HandleException(req, e);
        if (e is FormatException || e is ArgumentException || e is CharacterService.NotFoundException)
        {
          return req.FailureResponse();
        }
        else
        {
          throw e;
        }
      }
    }

    internal HttpResponse SaveProfile(HttpRequest req)
    {
      try
      {
        var uid = Convert.ToUInt32(req.GetFormParameter("id"));
        var data = req.GetFormParameter("profile");
        var token = req.GetBearerToken();

        this.characterService.UpdateCharacterProfile(token, uid, data);
        return req.TextResponse("ok");
      }
      catch (Exception e)
      {
        Request.HandleException(req, e);
        if (e is ArgumentException || e is CharacterService.NotAuthorizedException || e is CharacterService.NotFoundException || e is FormatException)
        {
          return req.FailureResponse();
        }
        else
        {
          throw e;
        }
      }
    }

    internal HttpResponse Wedding(HttpRequest req)
    {
      try
      {
        var id = Convert.ToUInt32(req.GetFormParameter("uid"));
        var wid = Convert.ToUInt32(req.GetFormParameter("wid"));
        var token = req.GetBearerToken();

        this.characterService.SetCharacterSpouse(token, id, wid);
        return req.TextResponse("ok");
      }
      catch (Exception e)
      {
        Request.HandleException(req, e);
        if (e is ArgumentException || e is CharacterService.NotAuthorizedException || e is CharacterService.NotFoundException || e is FormatException)
        {
          return req.FailureResponse();
        }
        else
        {
          throw e;
        }
      }
    }

    internal HttpResponse Divorce(HttpRequest req)
    {
      try
      {
        var id = Convert.ToUInt32(req.GetFormParameter("uid"));
        var token = req.GetBearerToken();

        this.characterService.UnsetCharacterSpouse(token, id);
        return req.TextResponse("ok");
      }
      catch (Exception e)
      {
        Request.HandleException(req, e);
        if (e is ArgumentException || e is CharacterService.NotAuthorizedException || e is CharacterService.NotFoundException || e is FormatException)
        {
          return req.FailureResponse();
        }
        else
        {
          throw e;
        }
      }
    }

    internal HttpResponse GetDefaultChar(HttpRequest req)
    {
      string sex;
      try
      {
        sex = req.GetFormParameter("sex");
      }
      catch (ArgumentException)
      {
        return req.FailureResponse();
      }

      string data = this.characterService.GetDefaultAppearance(sex);
      if (data != null)
      {
        return req.TextResponse(data);
      } else
      {
        return req.FailureResponse();
      }
    }

    internal HttpResponse GetBotChar(HttpRequest req)
    {
      string uid;
      try
      {
        uid = req.GetFormParameter("uid");
      }
      catch (ArgumentException)
      {
        return req.FailureResponse();
      }


      string sex = null;
      if (uid.Equals("1"))
      {
        sex = "m";
      }
      else
      {
        sex = "f";
      }

      try
      {
        return req.TextResponse(this.characterService.GetBotAppearance(sex));
      }
      catch (ArgumentException)
      {
        return req.FailureResponse();
      }
    }
  }
}
