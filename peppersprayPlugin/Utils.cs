using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Security.Cryptography;
using System.Xml;

namespace peppersprayPlugin
{
  public class Utils
  {
    public static XmlDocument OpenXml(string path)
    {
      XmlDocument doc = new XmlDocument();
      if (System.IO.File.Exists(path))
      {
        doc.Load(path);
      }
      return doc;
    }

    public static void SaveXml(XmlDocument doc, string path)
    {
      doc.Save(path);
    }
  }
}
