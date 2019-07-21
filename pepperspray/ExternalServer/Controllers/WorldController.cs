using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Unosquare.Labs.EmbedIO;
using Unosquare.Labs.EmbedIO.Constants;
using Unosquare.Labs.EmbedIO.Modules;
using Unosquare.Labs.EmbedIO.Core;
using HttpMultipartParser;
using System.Security.Cryptography;

namespace pepperspray.ExternalServer
{
  public class WorldController: WebApiController
  {
    public WorldController(IHttpContext context) : base(context)
    {

    }

    [WebApiHandler(HttpVerbs.Post, "/uploadworld2.php")]
    public async Task<bool> UploadWorld()
    {
      var parser = new MultipartFormDataParser(HttpContext.Request.InputStream, this.getMultipartBoundary(), Encoding.UTF8);
      var uid = parser.HasParameter("uid") ? parser.GetParameterValue("uid") : null;
      if (uid == null)
      {
        return false;
      }

      if (parser.Files.Count() < 1)
      {
        return false;
      }

      var file = parser.Files[0];
      using (var fileStream = File.OpenWrite(this.worldPath(uid)))
      {
        file.Data.Seek(0, SeekOrigin.Begin);
        file.Data.CopyTo(fileStream);
      }

      return true;
    }

    [WebApiHandler(HttpVerbs.Post, "/getworld2.php")]
    public async Task<bool> DownloadWorld()
    {
      var form = await HttpContext.RequestFormDataDictionaryAsync();
      string uid = form.ContainsKey("uid") ? form["uid"].ToString() : null;
      if (uid == null)
      {
        return false;
      }

      var path = this.worldPath(uid);
      if (!File.Exists(path))
      {
        path = this.defaultWorldPath();
      }
      using (var stream = File.OpenRead(path))
      {
        stream.CopyTo(HttpContext.Response.OutputStream);
      }
      return true;
    }

    private string getMultipartBoundary()
    {
      var contentType = HttpContext.Request.ContentType;
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
