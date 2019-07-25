using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

using RSG;

namespace pepperspray.CIO
{
  public static class EPromise
  {
    public static T Join<T>(this IPromise<T> promise)
    {
      T result = default(T);
      var sync = new ManualResetEvent(false);
      promise.Done(res =>
      {
        result = res;
        sync.Set();
      });
      sync.WaitOne();
      return result;
    }

    public static IPromise<T> IntoResolved<T>(this Promise<T> promise, T value)
    {
      promise.Resolve(value);
      return promise;
    }
  }
}
