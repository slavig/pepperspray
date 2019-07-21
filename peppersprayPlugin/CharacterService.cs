using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Security.Cryptography;
using System.Xml;

namespace peppersprayPlugin
{
  public class CharacterService
  {
    public class Character: IEquatable<Character>
    {
      public string Name;
      public string Sex;
      public string Data;

      public override bool Equals(object obj)
      {
        if (obj is Character)
        {
          return this.Equals(obj as Character);
        } else
        {
          return base.Equals(obj);
        }
      }

      public bool Equals(Character other)
      {
        return other.Name == this.Name;
      }
    }

    public List<Character> Characters;

    public static CharacterService Load()
    {
      var charactersDoc = Utils.OpenXml(CharacterService.charactersPath());
      var characters = new List<Character>();
      foreach (XmlNode node in charactersDoc.SelectNodes("characters/character"))
      {
        var name = node.Attributes["name"].Value;
        var sex = node.Attributes["sex"].Value;
        var data = node.InnerText;
        characters.Add(new Character
        {
          Name = name,
          Sex = sex,
          Data = data
        });
      }

      return new CharacterService
      {
        Characters = characters
      };
    }

    public void PushCharacter(Character character)
    {
      var doc = new XmlDocument();
      var node = doc.CreateElement("characters");
      doc.AppendChild(node);

      if (this.Characters.Contains(character))
      {
        this.Characters.Remove(character);
      }
      this.Characters.Insert(0, character);

      int count = 0;
      foreach (var item in this.Characters)
      {
        count += 1;
        if (count > 3)
        {
          break;
        }

        var element = doc.CreateElement("character");
        element.SetAttribute("name", item.Name);
        element.SetAttribute("sex", item.Sex);
        element.InnerText = item.Data;
        node.AppendChild(element);
      }

      Utils.SaveXml(doc, CharacterService.charactersPath());
    }


    private static string charactersPath()
    {
      return ".\\peppersprayPlugin\\characters.xml";
    }
  }
}
