using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

using Serilog;
using HttpMultipartParser;
using WatsonWebserver;
using pepperspray.RestAPIServer.Services;
using pepperspray.RestAPIServer.Storage;
using pepperspray.SharedServices;

namespace pepperspray.RestAPIServer.Controllers
{
  internal class WorldController
  {
    private Configuration config = DI.Get<Configuration>();
    private CharacterService characterService = DI.Get<CharacterService>();

    private WorldStorage storage;

    internal WorldController(Server s, WorldStorage storage)
    {
      this.storage = storage;

      s.StaticRoutes.Add(HttpMethod.POST, "/getworld2.php", this.GetWorld);
      s.StaticRoutes.Add(HttpMethod.POST, "/uploadworld2.php", this.UploadWorld);
    }

    internal HttpResponse GetWorld(HttpRequest req)
    {
      try
      {
        var uid = req.GetFormParameter("uid");
        if (/* this.config.StaticsRedirection.Enabled */ false)
        {
          /*
           * Client doesn't work yet with redirected worlds.
           **/
          return req.RedirectionResponse(this.config.StaticsRedirection.Host + "/worlds/" + this.storage.WorldName(uid));
        }
        else
        {
          byte[] worldData = this.storage.GetWorldData(uid);
          return new HttpResponse(req, 200, null, "text/plain", worldData);
        }
      }
      catch (Exception e)
      {
        Request.HandleException(req, e);

        if (e is ArgumentException
          || e is FormatException)
        {
          return req.FailureResponse();
        }
        else
        {
          throw e;
        }
      }
    }

    internal HttpResponse UploadWorld(HttpRequest req)
    {
      try
      {
        var header = req.ContentType;
        var parser = new MultipartFormDataParser(new MemoryStream(req.Data), req.GetMultipartBoundary(), Encoding.UTF8);
        var uid = parser.GetParameterValue("uid");
        var token = parser.GetParameterValue("token");
        this.characterService.FindAndAuthorize(token, Convert.ToUInt32(uid));

        if (parser.Files.Count() < 1)
        {
          Log.Error("Client {ip}:{port} failed to provide file for UploadWorld", req.SourceIp, req.SourcePort);
          return req.FailureResponse();
        }

        var file = parser.Files[0];
        Log.Information("Uploading world {uid} from client {ip}:{port}", uid, req.SourceIp, req.SourcePort);
        this.storage.SaveWorldData(uid, file.Data);
        Log.Information("Successfully saved world {uid} from client {ip}:{port}", uid, req.SourceIp, req.SourcePort);
        return new HttpResponse(req, 200, null);
      } 
      catch (Exception e)
      {
        Request.HandleException(req, e, false);
        if (e is CharacterService.NotAuthorizedException || e is CharacterService.NotFoundException)
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
