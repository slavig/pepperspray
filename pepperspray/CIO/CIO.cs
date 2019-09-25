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
      internal Thread ManagedThread;
      private bool restart;

      internal CIOThread(string name, bool restart, Action action)
      {
        this.restart = restart;
        this.ManagedThread = new Thread(new ThreadStart(() =>
        {
          try
          {
            Utils.Logging.ConfigureExceptionHandler();
            action();
            this.Resolve(new Nothing());
          }
          catch (Exception e) {
            Log.Error("Thread {id} crashed with following exception: {ex}", this.ManagedThread.ManagedThreadId, e);
            this.Reject(e);
          }
        }));

        this.ManagedThread.Name = name;
      }

      internal CIOThread Start()
      {
        Log.Debug("Spawned thread {id}/{name}", this.ManagedThread.ManagedThreadId, this.ManagedThread.Name);

        this.ManagedThread.Start();
        return this;
      }

      public void Join()
      {
        Log.Debug("Joining thread {name}", this.ManagedThread.Name);
        this.ManagedThread.Join();
      }
    }

    public static CIOThread Spawn(string name, bool restart, Action action)
    {
      var thread = new CIOThread(name, restart, action);
      thread.Start();
      return thread;
    }

    public static CIOThread Spawn(string name, Action action)
    {
      var thread = new CIOThread(name, false, action);
      thread.Start();
      return thread;
    }
  }
}
