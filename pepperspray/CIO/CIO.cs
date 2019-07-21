using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using RSG;

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
        action();
        this.Resolve(null);
        }));

        this.t.Name = name;
      }

      internal CIOThread Start()
      {
        this.t.Start();
        return this;
      }

      public void Join()
      {
        Debug.Assert(Thread.CurrentThread.IsBackground);
        this.t.Join();
      }
    }

    public static CIOThread Spawn(string name, Action action)
    {
      return new CIOThread(name, action).Start();
    }

    private static Thread createThread(Action action)
    {
      return new Thread(new ThreadStart(action));
    }
  }
}
