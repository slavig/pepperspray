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
  internal class DI
  {
    private static Dictionary<Type, object> registeredServices = new Dictionary<Type, object>();

    internal static T Get<T>() where T: class
    {
      return DI.registeredServices[typeof(T)] as T;
    }

    internal static T Auto<T>() where T: class, new()
    {
      if (!DI.registeredServices.ContainsKey(typeof(T)))
      {
        DI.registeredServices[typeof(T)] = new T();
      }

      return DI.Get<T>();
    }

    internal static void Register<T>(T service) where T : class {
      DI.registeredServices[typeof(T)] = service;
    }
  }
}
