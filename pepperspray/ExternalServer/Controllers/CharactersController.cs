using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

using Unosquare.Labs.EmbedIO;
using Unosquare.Labs.EmbedIO.Constants;
using Unosquare.Labs.EmbedIO.Modules;

namespace pepperspray.ExternalServer
{
  public class CharactersController: WebApiController
  {
    public CharactersController(IHttpContext context) : base(context)
    {
    }

    [WebApiHandler(HttpVerbs.Post, "/checkname")]
    public async Task<bool> CheckName()
    {
      await HttpContext.Response.StringResponseAsync("ok");
      return true;
    }

    [WebApiHandler(HttpVerbs.Post, "/createchar")]
    public async Task<bool> CreateChar()
    {
      var data = await HttpContext.RequestFormDataDictionaryAsync();
      var name = data.ContainsKey("name") ? data["name"].ToString() : null;

      await HttpContext.JsonResponseAsync(new Dictionary<string, string>
      {
        { "id", Utils.Hashing.Md5(name) },
        { "token", "" }
      });
      return true;
    }

    [WebApiHandler(HttpVerbs.Post, "/changechar")]
    public async Task<bool> ChangeChar()
    {
      var data = await HttpContext.RequestFormDataDictionaryAsync();
      var name = data.ContainsKey("newname") ? data["newname"].ToString() : null;

      await HttpContext.JsonResponseAsync(new Dictionary<string, string>
      {
        { "id", Utils.Hashing.Md5(name) },
        { "token", "" }
      });
      return true;
    }


    [WebApiHandler(HttpVerbs.Post, "/getdefaultchar")]
    public async Task<bool> GetDefaultChar()
    {
      var data = await HttpContext.RequestFormDataDictionaryAsync();
      var sex = data.ContainsKey("sex") ? data["sex"] : null;
      if (sex == null)
      {
        return false;
      }

      string path = null;
      if (sex.Equals("m"))
      {
        path = "defaultMale.base64";
      } else if (sex.Equals("f"))
      {
        path = "defaultFemale.base64";
      } else
      {
        return false;
      }

      var str = File.ReadAllText(ExternalServer.characterPresetsDirectoryPath + path);
      await HttpContext.Response.StringResponseAsync(str);
      return true;
    }
  }
}
