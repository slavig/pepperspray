using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using RSG;
using Serilog;

namespace pepperspray.CIO
{
  public class Nothing
  {
    public Nothing() { }
    public static IPromise<Nothing> Resolved()
    {
      return new Promise<Nothing>().IntoResolved(new Nothing());
    }
  }

  public class CIOReactor
  {
    public class CIOThread: Promise<Nothing>
    {
      private Thread t;
      internal CIOThread(string name, Action action)
      {
        this.t = new Thread(new ThreadStart(() =>
        {
          try
          {
            action();
          }
          catch (Exception) { }
          finally
          {
            this.Resolve(null);
          }
        }));

        this.t.Name = name;
      }

      internal CIOThread Start()
      {
        Log.Debug("Spawned thread {id}/{name}", this.t.ManagedThreadId, this.t.Name);

        this.t.Start();
        return this;
      }

      public void Join()
      {
        Log.Debug("Joining thread {name}", this.t.Name);
        this.t.Join();
      }
    }

    public static CIOThread Spawn(string name, Action action)
    {
      return new CIOThread(name, action).Start();
    }
  }
}
