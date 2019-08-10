using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pepperspray.Utils
{
  internal class ErrorException: Exception
  {
    internal string PlayerMessage;

    internal ErrorException(string name, string playerMessage): base(name)
    {
      this.PlayerMessage = playerMessage;
    }
  }
}
