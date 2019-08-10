using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace peppersprayPlugin
{
  public class Locator
  {
    public static Configuration Config = Configuration.Load();
    public static Locator Instance = new Locator();

    public static string Version = "0.5";
    public static string ProtocolVersion = "2";
  }
}
