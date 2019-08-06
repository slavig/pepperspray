using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace peppersprayPlugin
{
  public class LoginService
  {
    public string Login(string username, string passworldHash)
    {
      if (username.Trim().Equals(""))
      {
        return "answer=fail";
      }

      var response = "answer=ok";

      int i = 1;
      foreach (var item in Locator.CharacterService.Characters)
      {
        response += String.Format("\r\nid{0}={1}", i, this.md5Hash(item.Name));
        response += String.Format("\r\nname{0}={1}", i, item.Name);
        response += String.Format("\r\nsex{0}={1}", i, item.Sex);
        response += String.Format("\r\ndata{0}={1}", i, item.Data);

        i++;
      }

      response += String.Format("\r\ntoken=0");

      return response;
    }

    public string md5Hash(string input)
    {
      MD5 md5 = System.Security.Cryptography.MD5.Create();
      byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
      byte[] hash = md5.ComputeHash(inputBytes);
      StringBuilder sb = new StringBuilder();
      for (int i = 0; i < hash.Length; i++)
      {
        sb.Append(hash[i].ToString("x2"));
      }

      return sb.ToString();
    }
  }
}
