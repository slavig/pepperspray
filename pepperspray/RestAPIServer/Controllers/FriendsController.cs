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
      catch (Exception e)
      {
        Log.Debug("Client {endpoint} failed to accept friend: {exception}", req.GetEndpoint(), e);

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

        this.characterService.DeleteFriend(req.GetBearerToken(), charId, friendId);
        return req.TextResponse("ok");
      } 
      catch (Exception e)
      {
        Log.Debug("Client {endpoint} failed to delete friend: {exception}", req.GetEndpoint(), e);

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
        return req.TextResponse(this.characterService.GetFriends(req.GetBearerToken(), charId));
      } 
      catch (Exception e)
      {
        Log.Debug("Client {endpoint} failed to load friends: {exception}", req.GetEndpoint(), e);

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
