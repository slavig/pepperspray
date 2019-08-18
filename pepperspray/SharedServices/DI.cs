using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using pepperspray.ChatServer.Game;
using pepperspray.ChatServer.Protocol;
using pepperspray.ChatServer.Shell;
using pepperspray.ChatServer.Services;

namespace pepperspray.SharedServices
{
  internal interface IDIService
  {
    void Inject();
  }

  internal class DI
  {
    private static DI sync = new DI();
    private static Dictionary<Type, object> registeredServices = new Dictionary<Type, object>();

    internal static T Get<T>() where T: class, IDIService, new()
    {
      lock (DI.sync)
      {
        if (!DI.registeredServices.ContainsKey(typeof(T)))
        {
          var instance = new T();
          DI.registeredServices[typeof(T)] = instance;

          instance.Inject();
          return instance;
        }
        else
        {
          return DI.registeredServices[typeof(T)] as T;
        }
      }
    }

    internal static void Register<T>(T service) where T : class, IDIService {
      lock (DI.sync)
      {
        DI.registeredServices[typeof(T)] = service;
        service.Inject();
      }
    }
  }
}
