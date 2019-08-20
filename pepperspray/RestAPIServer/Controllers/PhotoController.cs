using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

using Serilog;
using Newtonsoft.Json;
using HttpMultipartParser;
using WatsonWebserver;
using pepperspray.Utils;
using pepperspray.RestAPIServer.Services;
using pepperspray.RestAPIServer.Storage;
using pepperspray.SharedServices;
using pepperspray.LoginServer;

namespace pepperspray.RestAPIServer.Controllers
{
  internal class PhotoController
  {
    private Configuration config = DI.Get<Configuration>();
    private CharacterService characterService = DI.Get<CharacterService>();
    private PhotoService photoService = DI.Get<PhotoService>();
    private PhotoStorage storage = DI.Get<PhotoStorage>();
    private LoginServerListener loginServer = DI.Get<LoginServerListener>();

    internal PhotoController(Server s)
    {
      s.StaticRoutes.Add(HttpMethod.POST, "/upload2.php", this.Upload);
      s.StaticRoutes.Add(HttpMethod.POST, "/setavatar", this.SetAvatar);
      s.StaticRoutes.Add(HttpMethod.POST, "/deletephoto", this.DeletePhoto);
      s.DynamicRoutes.Add(HttpMethod.GET, new Regex("^/photos/(.*?)\\.jpg$"), this.DownloadPhoto);
    }

    internal HttpResponse DownloadPhoto(HttpRequest req)
    {
      if (this.config.StaticsRedirection.Enabled)
      {
        return req.RedirectionResponse(this.config.StaticsRedirection.Host + req.RawUrlWithoutQuery);
      }
      else
      {
        var prefix = "/photos/";
        var suffix = ".jpg";
        var imageFile = req.RawUrlWithoutQuery.Substring(prefix.Length, req.RawUrlWithoutQuery.Length - prefix.Length - suffix.Length);
        var filePath = Path.Combine("peppersprayData", "photos", imageFile + ".jpg");

        if (!File.Exists(filePath))
        {
          return req.FailureResponse();
        }

        return new HttpResponse(req, 200, null, "image/jpg", File.ReadAllBytes(filePath));
      }
    }

    internal HttpResponse Upload(HttpRequest req)
    {
      uint uid = 0;
      try
      {
        var header = req.ContentType;
        var parser = new MultipartFormDataParser(new MemoryStream(req.Data), req.GetMultipartBoundary(), Encoding.UTF8);
        uid = Convert.ToUInt32(parser.GetParameterValue("uid"));
        var token = parser.GetParameterValue("token");
        var slot = parser.GetParameterValue("slot");
        this.characterService.FindAndAuthorize(token, Convert.ToUInt32(uid));

        var slotId = Convert.ToUInt32(slot);
        if (slotId > this.config.PlayerPhotoSlots)
        {
          Log.Warning("Client {endpoint} failed to upload photo: slot out of range ({slot})!", req.GetEndpoint(), slot);
          return req.TextResponse("limit=");
        }

        if (parser.Files.Count() < 1)
        {
          Log.Warning("Client {ip}:{port} failed to provide file for Photo upload", req.SourceIp, req.SourcePort);
          return req.FailureResponse();
        }

        var file = parser.Files[0];
        if (file.Data.Length > this.storage.SizeLimit)
        {
          Log.Warning("Client {endpoint} failed to upload photo: over size limit {size}", req.GetEndpoint(), file.Data.Length);
          return req.TextResponse("limit=" + this.storage.SizeLimit);
        }
        else
        {
          Log.Information("Uploading photo for {uid}, slot {slot} from client {ip}:{port}", uid, slot, req.SourceIp, req.SourcePort);
          var hash = this.storage.Save(file.Data);
          this.photoService.SetPhoto(token, uid, slot, hash);
          return req.TextResponse("ok=" + hash);
        }
      }
      catch (Exception e)
      {
        Request.HandleException(req, e, false);
        if (e is PhotoStorage.OversizeException)
        {
          return req.TextResponse("limit=" + this.storage.SizeLimit);
        } else if (e is CharacterService.NotAuthorizedException || e is CharacterService.NotFoundException || e is FormatException)
        {
          return req.FailureResponse();
        }
        else
        {
          return req.TextResponse("limit=1" + this.storage.SizeLimit);
        }
      }
    }

    internal HttpResponse DeletePhoto(HttpRequest req)
    {
      try
      {
        var token = req.GetBearerToken();
        var id = Convert.ToUInt32(req.GetFormParameter("uid"));
        var slotIdentifier = req.GetFormParameter("slot");

        var character = this.characterService.FindAndAuthorize(token, id);
        var slot = this.photoService.GetPhoto(id, slotIdentifier);

        Log.Debug("Client {endpoint} deleting photo of {id} at {slot}", req.GetEndpoint(), id, slotIdentifier);
        this.photoService.DeletePhoto(character, slot);
        this.storage.Delete(slot.Hash);
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

    internal HttpResponse SetAvatar(HttpRequest req)
    {
      try
      {
        var token = req.GetBearerToken();
        var id = Convert.ToUInt32(req.GetFormParameter("uid"));
        var slot = req.GetFormParameter("slot");

        Log.Debug("Client {endpoint} setting avatar of {id} to {slot}", req.GetEndpoint(), req.GetEndpoint(), id, slot);
        this.photoService.SetAvatar(token, id, slot);
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
