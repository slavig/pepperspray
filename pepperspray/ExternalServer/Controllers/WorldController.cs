using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

using HttpMultipartParser;
using WatsonWebserver;

namespace pepperspray.ExternalServer.Controllers
{
  internal class WorldController
  {
    private byte[] defaultWorldCache;

    internal WorldController(Server s)
    {
      s.StaticRoutes.Add(HttpMethod.POST, "/getworld2.php", this.GetWorld);
      s.StaticRoutes.Add(HttpMethod.POST, "/uploadworld2.php", this.UploadWorld);
    }

    internal HttpResponse GetWorld(HttpRequest req)
    {
      string uid = ExternalServer.GetFormParameter(req, "uid");
      if (uid == null)
      {
        return ExternalServer.FailureResponse(req);
      }

      var path = this.worldPath(uid);
      byte[] worldData = null;

      if (!File.Exists(path))
      {
        if (this.defaultWorldCache == null)
        {
          this.defaultWorldCache = File.ReadAllBytes(this.defaultWorldPath());
        }

        worldData = this.defaultWorldCache;
      }
      else
      {
        worldData = File.ReadAllBytes(path);
      }

      return new HttpResponse(req, 200, null, "text/plain", worldData);
    }

    internal HttpResponse UploadWorld(HttpRequest req)
    {
      var header = req.ContentType;
      var parser = new MultipartFormDataParser(new MemoryStream(req.Data), this.getMultipartBoundary(header), Encoding.UTF8);
      var uid = parser.HasParameter("uid") ? parser.GetParameterValue("uid") : null;
      if (uid == null)
      {
        return ExternalServer.FailureResponse(req);
      }

      if (parser.Files.Count() < 1)
      {
        return ExternalServer.FailureResponse(req);
      }

      var file = parser.Files[0];
      using (var fileStream = File.OpenWrite(this.worldPath(uid)))
      {
        file.Data.Seek(0, SeekOrigin.Begin);
        file.Data.CopyTo(fileStream);
      }

      return new HttpResponse(req, 200, null);
    }

    private string getMultipartBoundary(string contentType)
    {
      var boundaryPrefix = "multipart/form-data; boundary=\"";
      return contentType.Substring(boundaryPrefix.Count(), contentType.Count() - boundaryPrefix.Count() - 1);
    }

    private string defaultWorldPath()
    {
      return ExternalServer.worldDirectoryPath + "\\default.world";
    }

    private string worldPath(string uid)
    {
      var identifier = Utils.Hashing.Md5(uid);
      return ExternalServer.worldDirectoryPath + "\\" + identifier + ".world";
    }
  }
}
