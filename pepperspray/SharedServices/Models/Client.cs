using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Serilog;
using RSG;
using pepperspray.CIO;
using Newtonsoft.Json;

namespace pepperspray.SharedServices
{
  internal class Client
  {
    internal string ConnectionEndpoint;
    internal bool IsConnectionAlive => this.connection.IsAlive;
    internal string Token;
    internal Character LoggedCharacter;

    private CIOSocket connection;

    internal Client(CIOSocket connection)
    {
      this.connection = connection;
    }

    internal IMultiPromise<IEnumerable<object>> EventStream()
    {
      var promise = new MultiPromise<IEnumerable<object>>();
      this.connection.InputStream().SingleThen(bytes =>
      {
        var jsonString = Encoding.UTF8.GetString(bytes);
        var jsonObject = JsonConvert.DeserializeObject(jsonString + "");
        if (jsonObject is IEnumerable<object> == false)
        {
          throw new Exception("invalid request");
        }

        var ev = jsonObject as IEnumerable<object>;
        if (ev.Count() == 0)
        {
          throw new Exception("invalid request");
        }

        promise.SingleResolve(ev);
      })
      .Then(_ => promise.Resolve(null))
      .Catch(exception =>
      {
        promise.Reject(exception);
      });

      return promise;
    }

    internal IPromise<Nothing> Emit(params object[] arguments)
    {
      var str = JsonConvert.SerializeObject(arguments);
#if DEBUG
      Log.Verbose("Emitting to {endpoint} of {characterName}: json {json}", this.ConnectionEndpoint, this.LoggedCharacter != null ? this.LoggedCharacter.Name : null, str);
#endif
      var bytes = Encoding.UTF8.GetBytes(str);
      return this.connection.Write(bytes);
    }

    internal void Terminate()
    {
      this.connection.Shutdown();
    }
  }
}
