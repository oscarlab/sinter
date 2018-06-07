using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Sintering {

  [Serializable()]
  [XmlRoot("sinter")]
  public class Sinter {
    [XmlElement("header")]
    public Header HeaderNode { get; set; }

    // single entity
    [XmlElement("entity")]
    public Entity EntityNode { get; set; }

    // multiple entities
    [XmlArray("entities")]
    [XmlArrayItem("entity" , typeof(Entity) , IsNullable = false)]
    public List<Entity> EntityNodes { get; set; }
  }

  [XmlRoot("word")]
  public class Word {
    [XmlAttribute("text")]
    public string text { get; set; }

    [XmlAttribute("font_name")]
    public string font_name { get; set; }

    [XmlAttribute("font_size")]
    public string font_size { get; set; }

    [XmlAttribute("bold")]
    public string bold { get; set; }

    [XmlAttribute("italic")]
    public string italic { get; set; }

    [XmlAttribute("underline")]
    public string underline { get; set; }

    [XmlAttribute("newline")]
    public string newline { get; set; }

    public Word() {
      text = "";
      font_name = "";
      font_size = "";
      bold = "0";
      italic = "0";
      underline = "0";
      newline = "0";
    }
  }

  [XmlRoot("entity")]
  public class Entity {

    public Entity() {
      ChildCount = 0;
    }

    [XmlAttribute("unique_id")]
    public string UniqueID { get; set; }

    [XmlAttribute("name")]
    public string Name { get; set; }

    [XmlAttribute("value")]
    public string Value { get; set; }

    [XmlAttribute("type")]
    public string Type { get; set; }

    [XmlAttribute("raw_type")]
    public string RawType { get; set; }
    
    [XmlAttribute("process_id")]
    public string Process { get; set; }

    [XmlAttribute("top")]
    public int Top { get; set; }

    [XmlAttribute("left")]
    public int Left { get; set; }

    [XmlAttribute("height")]
    public int Height { get; set; }

    [XmlAttribute("width")]
    public int Width { get; set; }

    [XmlAttribute("child_count")]
    public int ChildCount { get; set; }

    [XmlAttribute("states")]
    public uint States { get; set; }

    [XmlArray("words")]
    [XmlArrayItem("word" , typeof(Word) , IsNullable = false)]
    public List<Word> words { get; set; }

    [XmlArray("children")]
    [XmlArrayItem("entity" , typeof(Entity) , IsNullable = false)]
    public List<Entity> Children { get; set; }
  }

  [XmlRoot("header")]
  public class Header {
    [XmlAttribute("service_code")]
    public int ServiceCode { get; set; }

    [XmlAttribute("sub_code")]
    public int SubCode { get; set; }

    [XmlAttribute("timestamp")]
    public string Timestamp { get; set; }

    [XmlAttribute("process_id")]
    public string Process { get; set; }

    [XmlElement("params")] //, IsNullable = true
    public Params ParamsInfo { get; set; }
  }

  [XmlRoot("screen")]
  public class Screen {
    [XmlAttribute("screen_width")]
    public int ScreenWidth { get; set; }

    [XmlAttribute("screen_height")]
    public int ScreenHeight { get; set; }
  }

  [XmlRoot("params")]
  public class Params
  {
    [XmlAttribute("target_id")]
    public string TargetId { get; set; }

    [XmlAttribute("data1")]
    public string Data1 { get; set; }

    [XmlAttribute("data2")]
    public string Data2 { get; set; }

    [XmlAttribute("data3")]
    public string Data3 { get; set; }

    public override string ToString()
    {
      return string.Format("<tid:{0}, data1:{1}, data2:{2}, data3:{3}>", TargetId, Data1, Data2, Data3);
    }
  }
}
