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

namespace pepperspray.RestAPIServer.Controllers
{
  internal class WorldController
  {
    private WorldStorage storage;

    internal WorldController(Server s, WorldStorage storage)
    {
      this.storage = storage;

      s.StaticRoutes.Add(HttpMethod.POST, "/getworld2.php", this.GetWorld);
      s.StaticRoutes.Add(HttpMethod.POST, "/uploadworld2.php", this.UploadWorld);
    }

    internal HttpResponse GetWorld(HttpRequest req)
    {
      string uid;
      try
      {
        uid = req.GetFormParameter("uid");
      } catch (ArgumentException)
      {
        Log.Error("Client {ip}:{port} failed to provide uid for GetWorld", req.SourceIp, req.SourcePort);
        return req.FailureResponse();
      }

      Log.Information("Client {ip}:{port} requesting to load world {uid}", req.SourceIp, req.SourcePort, uid);
      byte[] worldData = this.storage.GetWorldData(uid);

      Log.Information("Sending world {uid} to client {ip}:{port}", uid, req.SourceIp, req.SourcePort, uid);
      return new HttpResponse(req, 200, null, "text/plain", worldData);
    }

    internal HttpResponse UploadWorld(HttpRequest req)
    {
      var header = req.ContentType;
      var parser = new MultipartFormDataParser(new MemoryStream(req.Data), this.getMultipartBoundary(header), Encoding.UTF8);
      var uid = parser.HasParameter("uid") ? parser.GetParameterValue("uid") : null;
      if (uid == null)
      {
        Log.Error("Client {ip}:{port} failed to provide uid for UploadWorld", req.SourceIp, req.SourcePort);
        return req.FailureResponse();
      }

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

    private string getMultipartBoundary(string contentType)
    {
      var boundaryPrefix = "multipart/form-data; boundary=\"";
      return contentType.Substring(boundaryPrefix.Count(), contentType.Count() - boundaryPrefix.Count() - 1);
    }
  }
}
