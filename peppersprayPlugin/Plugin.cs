using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace peppersprayPlugin
{
  public class Locator
  {
    public static Configuration Config = Configuration.LoadConfiguration();
    public static LoginService LoginService = new LoginService();
    public static Locator Instance = new Locator();

    public static void ReloadConfig()
    {
      Locator.Config = Configuration.LoadConfiguration();
    }
  }
}
