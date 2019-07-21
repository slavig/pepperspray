using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Unosquare.Labs.EmbedIO;
using Unosquare.Labs.EmbedIO.Constants;
using Unosquare.Labs.EmbedIO.Modules;

namespace pepperspray.ExternalServer
{
  public class MiscController: WebApiController
  {
    public MiscController(IHttpContext context) : base(context)
    {
    }

    [WebApiHandler(HttpVerbs.Post, "/news.php")]
    public async Task<bool> News()
    {
      var str = File.ReadAllText(ExternalServer.newsPath);
      await HttpContext.Response.StringResponseAsync(str);
      return true;
    }

    [WebApiHandler(HttpVerbs.Post, "/getmoney")]
    public async Task<bool> GetMoney()
    {
      await HttpContext.Response.StringResponseAsync("25170");
      return true;
    }
  }
}
