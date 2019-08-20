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
  internal class GiftController
  {
    private GiftsService giftService = DI.Get<GiftsService>();

    internal GiftController(Server s)
    {
      s.StaticRoutes.Add(HttpMethod.POST, "/getgifts", this.GetGifts);
      s.StaticRoutes.Add(HttpMethod.POST, "/sendgift", this.SendGift);
      s.StaticRoutes.Add(HttpMethod.POST, "/deletegift", this.DeleteGift);
    }

    internal HttpResponse GetGifts(HttpRequest req)
    {
      try
      {
        var id = Convert.ToUInt32(req.GetFormParameter("uid"));
        var offset = Convert.ToUInt32(req.GetFormParameter("page"));
        var gifts = this.giftService.GetGifts(id, offset);
        return req.JsonResponse(gifts);
      }
      catch (Exception e)
      {
        Request.HandleException(req, e);

        if (e is CharacterService.NotFoundException || e is FormatException)
        {
          return req.FailureResponse();
        }
        else
        {
          throw e;
        }
      }
    }

    internal HttpResponse SendGift(HttpRequest req)
    {
      try
      {
        var token = req.GetBearerToken();
        var senderId = Convert.ToUInt32(req.GetFormParameter("from"));
        var recepientId = Convert.ToUInt32(req.GetFormParameter("to"));
        var giftIdentifier = req.GetFormParameter("gift");
        var message = req.GetFormParameter("msg");

        this.giftService.SendGift(token, senderId, recepientId, giftIdentifier, message);
        return req.TextResponse("ok");
      } 
      catch (GiftsService.NotEnoughCurrencyException)
      {
        return req.TextResponse("money");
      }
      catch (Exception e)
      {
        Request.HandleException(req, e);

        if (e is CharacterService.NotAuthorizedException || e is CharacterService.NotFoundException || e is FormatException)
        {
          return req.FailureResponse();
        }
        else
        {
          throw e;
        }
      }
    }

    internal HttpResponse DeleteGift(HttpRequest req)
    {
      try
      {
        var token = req.GetBearerToken();
        var id = Convert.ToUInt32(req.GetFormParameter("uid"));
        var gid = req.GetFormParameter("gid");

        this.giftService.DeleteGift(token, id, gid);
        return req.TextResponse("ok");
      } 
      catch (Exception e)
      {
        Request.HandleException(req, e);

        if (e is CharacterService.NotAuthorizedException || e is CharacterService.NotFoundException || e is FormatException)
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
