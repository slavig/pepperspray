using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Newtonsoft.Json;
using WatsonWebserver;
using SpikeHttp = Spike.Network.Http;

namespace pepperspray.RestAPIServer.Services
{
  internal static class Request
  {
    internal static HttpResponse FailureResponse(this HttpRequest req)
    {
      return new HttpResponse(req, 400, null);
    }

    internal static HttpResponse TextResponse(this HttpRequest req, string text)
    {
      return new HttpResponse(req, 200, null, "text/plain; charset=utf-8", text);
    }

    internal static HttpResponse JsonResponse(this HttpRequest req, object data)
    {
      return new HttpResponse(req, 200, null, "application/json", JsonConvert.SerializeObject(data));
    }

    internal static HttpResponse RedirectionResponse(this HttpRequest req, string targetUrl)
    {
      return new HttpResponse(req, 301, new Dictionary<string, string> { { "Location", targetUrl } });
    }

    internal static string GetMultipartBoundary(this HttpRequest req)
    {
      var contentType = req.ContentType;
      var boundaryPrefix = "multipart/form-data; boundary=\"";
      return contentType.Substring(boundaryPrefix.Count(), contentType.Count() - boundaryPrefix.Count() - 1);
    }

    internal static string GetEndpoint(this HttpRequest req)
    {
      return String.Format("{0}:{1}", req.SourceIp, req.SourcePort);
    }

    internal static string GetFormParameter(this HttpRequest req, string key)
    {
      if (req.Data == null)
      {
        throw new ArgumentException();
      }

      var str = Encoding.UTF8.GetString(req.Data);
      foreach (string keypair in str.Split('&'))
      {
        string[] kv = keypair.Split('=');
        if (kv.Count() < 2)
        {
          continue;
        }

        if (kv[0].Equals(key))
        {
          return SpikeHttp.HttpUtility.UrlDecode(kv[1]);
        }
      }

      throw new ArgumentException();
    }

    internal static string GetBearerToken(this HttpRequest req)
    {
      string authorization = null;
      if (!req.Headers.TryGetValue("Authorization", out authorization))
      {
        throw new ArgumentException();
      }

      var prefix = "Bearer ";
      if (authorization.Length < prefix.Length)
      {
        throw new ArgumentException();
      }

      return authorization.Substring(prefix.Length);
    }
  }
}
