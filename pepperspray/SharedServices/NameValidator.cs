using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace pepperspray.SharedServices
{
  internal class NameValidator: IDIService
  {
    internal string ServerName = "";

    private Regex nameRegex = new Regex("^[A-Za-z0-9_\\-]{3,32}$");
    private static List<string> reservedNames = new List<string>
    {
      "Admin", "Server", "Support", "pepperspray"
    };

    public void Inject()
    {

    }

    internal bool Validate(string name)
    {
      var trimmedName = name.Trim();
      return !trimmedName.Equals(this.ServerName) && !NameValidator.reservedNames.Contains(trimmedName) && this.nameRegex.IsMatch(name);
    }
  }
}
