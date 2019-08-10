using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using WatsonWebserver;
using pepperspray.Utils;
using pepperspray.RestAPIServer.Services;
using pepperspray.SharedServices;

namespace pepperspray.RestAPIServer.Controllers
{
  internal class FriendsController
  {
    private CharacterService characterService = DI.Auto<CharacterService>();

    internal FriendsController(Server s)
    {
      s.StaticRoutes.Add(HttpMethod.POST, "/acceptfriend", this.AcceptFriend);
      s.StaticRoutes.Add(HttpMethod.POST, "/deletefriend", this.DeleteFriend);
      s.StaticRoutes.Add(HttpMethod.POST, "/loadfriends", this.LoadFriends);
    }

    internal HttpResponse AcceptFriend(HttpRequest req)
    {
      try
      {
        var payload = req.GetFormParameter("token");
        string[] components = payload.Split(',');
        if (components.Count() != 2)
        {
          throw new ArgumentException();
        }

        var charId = Convert.ToUInt32(components[0]);
        var friendId = Convert.ToUInt32(components[1]);

        this.characterService.AcceptFriendRequest(req.GetBearerToken(), charId, friendId);
        return req.TextResponse("ok");
      } 
      catch (FormatException)
      {
        return req.FailureResponse();
      }
      catch (ArgumentException)
      {
        return req.FailureResponse();
      }
      catch (CharacterService.NotAuthorizedException)
      {
        return req.FailureResponse();
      }
    }

    internal HttpResponse DeleteFriend(HttpRequest req)
    {
      try
      {
        var charId = Convert.ToUInt32(req.GetFormParameter("uid"));
        var friendId = Convert.ToUInt32(req.GetFormParameter("fid"));

        this.characterService.DeleteFriend(req.GetBearerToken(), charId, friendId);
        return req.TextResponse("ok");
      } 
      catch (FormatException)
      {
        return req.FailureResponse();
      }
      catch (ArgumentException)
      {
        return req.FailureResponse();
      }
      catch (CharacterService.NotAuthorizedException)
      {
        return req.FailureResponse();
      }
    }

    internal HttpResponse LoadFriends(HttpRequest req)
    {
      try
      {
        var charId = Convert.ToUInt32(req.GetFormParameter("id"));
        return req.TextResponse(this.characterService.GetFriends(req.GetBearerToken(), charId));
      } 
      catch (FormatException)
      {
        return req.FailureResponse();
      }
      catch (ArgumentException)
      {
        return req.FailureResponse();
      }
      catch (CharacterService.NotAuthorizedException)
      {
        return req.FailureResponse();
      }
    }
  }
}
