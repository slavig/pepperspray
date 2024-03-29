﻿using RSG;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Serilog;

namespace pepperspray.CIO
{
  public interface IMultiPromise<A>: IPromise<A>
  {
    IMultiPromise<A> SingleThen(Action<A> func);
    IMultiPromise<B> Map<B>(Func<A, B> func);
    IMultiPromise<B> CompactMap<B>(Func<A, B> func);
    IMultiPromise<B> Into<B>(Func<MultiPromise<A>, IMultiPromise<B>> func);
    IPromise<I> Fold<I>(I initial, Func<I, A, I> func);
  }

  public class MultiPromise<T>: Promise<T>, IMultiPromise<T>
  {
    private Action<T> singleThenFunc;
    private Action<Exception> singleCatchFunc;

    public void SingleResolve(T item)
    {
      lock (this)
      {
        if (this.singleThenFunc != null)
        {
          try
          {
            this.singleThenFunc(item);
          }
          catch (Exception e)
          {
            Log.Debug("MultiPromise {promise} rejected due to unhandled exception: {exception}", this, e);
            this.Reject(e);
          }
        }
      }
    }

    public IMultiPromise<T> SingleThen(Action<T> func)
    {
      lock (this)
      {
        this.singleThenFunc = func;
        return this;
      }
    }

    public IMultiPromise<B> Into<B>(Func<MultiPromise<T>, IMultiPromise<B>> func)
    {
      return func(this);
    }

    public IMultiPromise<B> Map<B>(Func<T, B> map)
    {
      return new MappedPromise<T, B>(this, map);
    }

    public IMultiPromise<B> CompactMap<B>(Func<T, B> map)
    {
      return new MappedPromise<T, B>(this, map, true);
    }

    public IPromise<I> Fold<I>(I initial, Func<I, T, I> func)
    {
      return new FoldedPromise<T, I>(this, initial, func);
    }
  }

  public class MappedPromise<A, B>: MultiPromise<B>
  {
    private MultiPromise<A> promise;

    public MappedPromise(MultiPromise<A> promise, Func<A, B> functor, bool compact = false) 
    {
      this.promise = promise;
      this.promise.SingleThen(value =>
      {
        var processedItem = functor(value);
        if (!compact || processedItem != null)
        {
          this.SingleResolve(processedItem);
        }
      })
      .Then(value =>
      {
        this.Resolve(default(B));
      })
      .Catch(ex => this.Reject(ex));
    }
  }

  public class FoldedPromise<A, I>: Promise<I>
  {
    private MultiPromise<A> promise;
    private I value;

    public FoldedPromise(MultiPromise<A> promise, I initial, Func<I, A, I> functor) 
    {
      this.value = initial;
      this.promise = promise;
      this.promise
        .SingleThen(item => this.value = functor(this.value, item))
        .Then(value => this.Resolve(this.value), ex => this.Reject(ex));
    }
  }
}
