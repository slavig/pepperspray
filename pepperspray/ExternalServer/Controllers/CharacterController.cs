using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

using pepperspray.Utils;
using pepperspray.SharedServices;
using WatsonWebserver;

namespace pepperspray.ExternalServer.Controllers
{
  internal class CharacterController
  {
    private NameValidator nameValidator = DI.Auto<NameValidator>();

    internal CharacterController(Server s)
    {
      s.StaticRoutes.Add(HttpMethod.POST, "/checkname", this.CheckName);
      s.StaticRoutes.Add(HttpMethod.POST, "/createchar", this.CreateChar);
      s.StaticRoutes.Add(HttpMethod.POST, "/changechar", this.ChangeChar);
      s.StaticRoutes.Add(HttpMethod.POST, "/getdefaultchar", this.GetDefaultChar);
    }

    internal HttpResponse CheckName(HttpRequest req)
    {
      string name = ExternalServer.GetFormParameter(req, "name");
      if (name == null)
      {
        return ExternalServer.FailureResponse(req);
      }

      return new HttpResponse(req, 200, null, "text/plain", this.nameValidator.Validate(name) ? "ok" : "bad_name");
    }

    internal HttpResponse CreateChar(HttpRequest req)
    {
      string name = ExternalServer.GetFormParameter(req, "name");
      if (name == null)
      {
        return ExternalServer.FailureResponse(req);
      }

      if (!this.nameValidator.Validate(name))
      {
        return ExternalServer.FailureResponse(req);
      }

      return new HttpResponse(req, 200, null, "text/json", JsonConvert.SerializeObject(new Dictionary<string, string>
      {
        { "id", Hashing.Md5(name) },
        { "token", " " }
      }));
    }

    internal HttpResponse ChangeChar(HttpRequest req)
    {
      string name = ExternalServer.GetFormParameter(req, "newname");
      if (name == null)
      {
        return ExternalServer.FailureResponse(req);
      }

      if (!this.nameValidator.Validate(name))
      {
        return ExternalServer.FailureResponse(req);
      }

      return new HttpResponse(req, 200, null, "text/json", JsonConvert.SerializeObject(new Dictionary<string, string>
      {
        { "id", Hashing.Md5(name) },
        { "token", " " }
      }));
    }

    internal HttpResponse GetDefaultChar(HttpRequest req)
    {
      string sex = ExternalServer.GetFormParameter(req, "sex");
      if (sex == null)
      {
        return ExternalServer.FailureResponse(req);
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
        return ExternalServer.FailureResponse(req);
      }

      return new HttpResponse(req, 200, null, "text/plain", File.ReadAllText(Path.Combine(ExternalServer.characterPresetsDirectoryPath, path)));
    }
  }
}
