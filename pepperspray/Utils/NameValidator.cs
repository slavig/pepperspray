using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace pepperspray.Utils
{
  internal class NameValidator
  {
    internal string ServerName = "";

    private Regex nameRegex = new Regex("^\\w{3,32}$");

    internal bool Validate(string name)
    {
      return !name.Equals(this.ServerName) && this.nameRegex.IsMatch(name);
    }
  }
}
