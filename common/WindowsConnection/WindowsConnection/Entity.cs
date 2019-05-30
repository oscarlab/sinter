/* Copyright (C) 2014--2018 Stony Brook University
   Copyright (C) 2016--2018 The University of North Carolina at Chapel Hill

   This file is part of the Sinter Remote Desktop System.

   Sinter is dual-licensed, available under a commercial license or
   for free subject to the LGPL.

   Sinter is free software: you can redistribute it and/or modify it
   under the terms of the GNU Lesser General Public License as published by
   the Free Software Foundation, either version 3 of the License, or
   (at your option) any later version.  Sinter is distributed in the
   hope that it will be useful, but WITHOUT ANY WARRANTY; without even
   the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR
   PURPOSE.  See the GNU Lesser General Public License for more details.  You
   should have received a copy of the GNU Lesser General Public License along
   with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

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
    public class Entity : IEquatable<Entity>
    {

        public Entity()
        {
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
        [XmlArrayItem("word", typeof(Word), IsNullable = false)]
        public List<Word> words { get; set; }

        [XmlArray("children")]
        [XmlArrayItem("entity", typeof(Entity), IsNullable = false)]
        public List<Entity> Children { get; set; }

        [XmlIgnore]
        public VersionInfo versionInfo { get; set; }

        public bool Equals(Entity other)
        {
            return ChildCount == other.ChildCount
                    && Height == other.Height && Left == other.Left
                    && Name == other.Name && Process == other.Process
                    && RawType == other.RawType && States == other.States
                    && Top == other.Top && Type == other.Type
                    && UniqueID == other.UniqueID && Value == other.Value
                    && Width == other.Width && words == other.words;
        }
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

        [XmlArray("target_id_list")]
        [XmlArrayItem("string", typeof(string[]), IsNullable = false)]
        public List<string[]> TargetIdList { get; set; }

    [XmlAttribute("data1")]
    public string Data1 { get; set; }

    [XmlAttribute("data2")]
    public string Data2 { get; set; }

    [XmlAttribute("data3")]
    public string Data3 { get; set; }

    [XmlAttribute("keypress")]
    public char KeyPress { get; set; }

    public override string ToString()
    {
      return string.Format("<tid:{0}, tidList: {1}, data1:{2}, data2:{3}, data3:{4}>", TargetId, TargetIdList, Data1, Data2, Data3);
    }
  }
    [Flags]
    public enum Version
    {
        None = 0x0,
        Init = 0x1 << 0,
        Updated = 0x1 << 1,
        Expanded = 0x1 << 2,
        Collapsed = 0x1 << 3,
        Other = 0x1 << 4,
    }

    public class VersionInfo
    {
        public string runtimeID { get; set; }
        public Version version { get; set; }
        public string Hash { get; set; }

        public VersionInfo()
        {
            this.runtimeID = null;
            this.version = Version.Init;
        }
        public VersionInfo(string runtimeID, Version version = Version.Init)
        {
            this.runtimeID = runtimeID;
            this.version = version;
        }
    }

}
