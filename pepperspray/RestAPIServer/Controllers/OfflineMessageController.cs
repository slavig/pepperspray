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
  internal class OfflineMessageController
  {
    private OfflineMessageService offlineMessageService = DI.Get<OfflineMessageService>();
    private CharacterService characterSerivce = DI.Get<CharacterService>();

    internal OfflineMessageController(Server s)
    {
      s.StaticRoutes.Add(HttpMethod.POST, "/offlineMsgCheck", this.OfflineMsgCheck);
      s.StaticRoutes.Add(HttpMethod.POST, "/offlineMsgGet", this.OfflineMsgGet);
    }

    internal HttpResponse OfflineMsgCheck(HttpRequest req)
    {
      try
      {
        var token = req.GetBearerToken();
        var result = new List<Dictionary<string, object>>();
        foreach (var pair in this.offlineMessageService.CheckMessages(token))
        {
          result.Add(new Dictionary<string, object>()
          {
            { "id", pair.Key },
            { "msg", pair.Value }
          });
        }

        return req.JsonResponse(result);
      }
      catch (Exception e)
      {
        Request.HandleException(req, e);

        if (e is CharacterService.NotFoundException || e is FormatException || e is LoginService.InvalidTokenException)
        {
          return req.FailureResponse();
        }
        else
        {
          throw e;
        }
      }
    }

    internal HttpResponse OfflineMsgGet(HttpRequest req)
    {
      try
      {
        var token = req.GetBearerToken();
        var id = Convert.ToUInt32(req.GetFormParameter("uid"));
        var messages = this.offlineMessageService.PopMessages(token, id);

        var result = new List<Dictionary<string, object>>();
        foreach (var message in messages)
        {
          try
          {
            var sender = this.characterSerivce.Find(message.SenderId);
            result.Add(new Dictionary<string, object>
            {
              { "from", new Dictionary<string, string>
                {
                  { "id", sender.Id.ToString() },
                  { "name", sender.Name }
                }
              },
              { "msg", message.Message }
            });
          }
          catch (CharacterService.NotFoundException)
          {
            continue;
          }
        }

        return req.JsonResponse(result);
      }
      catch (Exception e)
      {
        Request.HandleException(req, e);

        if (e is CharacterService.NotFoundException || e is FormatException || e is CharacterService.NotAuthorizedException)
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
