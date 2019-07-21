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
    public static CharacterService CharacterService = CharacterService.Load();
    public static LoginService LoginService = new LoginService();

    public static Locator Instance = new Locator();

    public static void ReloadCharacters()
    {
      Locator.CharacterService = CharacterService.Load();
    }
  }
}
