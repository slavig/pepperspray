using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace pepperspray.SharedServices
{
  internal class NameValidator
  {
    internal string ServerName = "";

    private Regex nameRegex = new Regex("^\\w{3,32}$");
    private static List<string> reservedNames = new List<string>
    {
      "Admin", "Server", "Support", "pepperspray"
    };

    internal bool Validate(string name)
    {
      var trimmedName = name.Trim();
      return !trimmedName.Equals(this.ServerName) && !NameValidator.reservedNames.Contains(trimmedName) && this.nameRegex.IsMatch(name);
    }
  }
}
