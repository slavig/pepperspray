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
  internal class FriendsController
  {
    private FriendsService friendsService = DI.Get<FriendsService>();

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

        this.friendsService.AcceptFriendRequest(req.GetBearerToken(), charId, friendId);
        return req.TextResponse("ok");
      } 
      catch (Exception e)
      {
        Request.HandleException(req, e);

        if (e is FormatException
          || e is ArgumentException
          || e is CharacterService.NotAuthorizedException)
        {
          return req.FailureResponse();
        } else
        {
          throw e;
        }
      }
    }

    internal HttpResponse DeleteFriend(HttpRequest req)
    {
      try
      {
        var charId = Convert.ToUInt32(req.GetFormParameter("uid"));
        var friendId = Convert.ToUInt32(req.GetFormParameter("fid"));

        this.friendsService.DeleteFriend(req.GetBearerToken(), charId, friendId);
        return req.TextResponse("ok");
      } 
      catch (Exception e)
      {
        Request.HandleException(req, e);

        if (e is FormatException
          || e is ArgumentException
          || e is CharacterService.NotAuthorizedException)
        {
          return req.FailureResponse();
        }
        else
        {
          throw e;
        }
      }
    }

    internal HttpResponse LoadFriends(HttpRequest req)
    {
      try
      {
        var charId = Convert.ToUInt32(req.GetFormParameter("id"));
        return req.TextResponse(this.friendsService.GetFriends(req.GetBearerToken(), charId));
      } 
      catch (Exception e)
      {
        Request.HandleException(req, e);

        if (e is FormatException
          || e is ArgumentException
          || e is CharacterService.NotAuthorizedException)
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
