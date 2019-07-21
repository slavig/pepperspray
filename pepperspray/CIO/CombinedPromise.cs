using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RSG;

namespace pepperspray.CIO
{
  class CombinedPromise<A>: MultiPromise<A>
  {
    private List<IPromise> promises;

    public CombinedPromise(IEnumerable<IPromise<A>> gen)
    {
      var hitCount = 0;
      var promises = gen.ToList<IPromise<A>>();

      this.promises = promises.Select(promise =>
        promise.Then(value =>
        {
          this.SingleResolve(value);
          hitCount += 1;
          if (hitCount == promises.Count())
          {
            this.Resolve(value);
          }
        }, ex =>
        {
          hitCount += 1;
          if (hitCount == promises.Count())
          {
            this.Reject(ex);
          }
        })).ToList();
    }
  }
}
