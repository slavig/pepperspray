using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using pepperspray.CoreServer.Game;
using pepperspray.CoreServer.Protocol;
using pepperspray.CoreServer.Shell;

namespace pepperspray.Utils
{
  internal class DI
  {
    private static Dictionary<Type, object> registeredServices = new Dictionary<Type, object>();

    internal static T Get<T>() where T: class
    {
      return DI.registeredServices[typeof(T)] as T;
    }

    internal static void Register<T>(T service) where T : class {
      DI.registeredServices[typeof(T)] = service;
    }

    internal static void Setup()
    {
      DI.Register(new Configuration(Path.Combine("peppersprayData", "configuration.xml")));
      DI.Register(new ActionsAuthenticator());
      DI.Register(new ShellDispatcher());
      DI.Register(new NameValidator());
    }
  }
}
