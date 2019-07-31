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
using Sintering.PlatformSpecificInfo;

namespace Sintering
{

    [Serializable()]
    [XmlRoot("sinter")]
    public class Sinter
    {
        [XmlElement("header")]
        public Header HeaderNode { get; set; }

        // single entity
        [XmlElement("entity")]
        public Entity EntityNode { get; set; }

        // multiple entities
        [XmlArray("entities")]
        [XmlArrayItem("entity", typeof(Entity), IsNullable = false)]
        public List<Entity> EntityNodes { get; set; }
    }

    [XmlRoot("word")]
    public class Word
    {
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

        public Word()
        {
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

        [XmlElement("detailed_entity", IsNullable = false)] //contain platform-specific data
        public DetailedEntity DetailedEntity { get; set; }

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
        public List<Word> Words { get; set; }

        [XmlArray("children")]
        [XmlArrayItem("entity", typeof(Entity), IsNullable = false)]
        public List<Entity> Children { get; set; }

        [XmlIgnore]
        public VersionInfo VersionInfo { get; set; }

        /*
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
        */

        public bool Equals(Entity other)
        {
            if (other == null)
                return false;

            if (!string.IsNullOrEmpty(UniqueID) &&
                !string.IsNullOrEmpty(other.UniqueID) &&
                UniqueID == other.UniqueID)
                return true;

            return false;
        }

        public override int GetHashCode()
        {
            if (!string.IsNullOrEmpty(UniqueID))
                return UniqueID.GetHashCode();
            return GetHashCode();
        }


        public void RemoveSubtree()
        {
            if (Children == null)
                return;

            int count = Children.Count - 1;
            while (count >= 0)
            {
                Entity subtree = Children[count];
                subtree.RemoveSubtree();

                Children.RemoveAt(count);
                //subtree = null;
                count--;
            }

            Children = null;
        }        
    }

    [XmlRoot("header")]
    public class Header
    {
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
    public class Screen
    {
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

    #region more, platform-specific entity info
    namespace PlatformSpecificInfo
    {
        [XmlRoot("DetailedEntity")]
        public class DetailedEntity : IEquatable<DetailedEntity>
        {
            public DetailedEntity()
            {
                ScreenTop = 0;
                ScreenLeft = 0;
            }

            public DetailedEntity(string id) : this()
            {
                IDProperty = id;
            }

            // a composite primary id
            private CompoundID cid;

            [XmlIgnore]
            public CompoundID CID
            {
                get
                {
                    if (cid == null)
                        cid = new CompoundID(this);
                    return cid;
                }
                set
                {
                    cid = value;
                    RuntimeIdProperty = cid.RuntimeIdProperty;
                    AutomationIdProperty = cid.AutomationIdProperty;
                    NativeWindowHandleProperty = cid.NativeWindowHandleProperty;
                    ClassNameProperty = cid.ClassNameProperty;
                    NameProperty = cid.NameProperty;
                    BoundingRectangleProperty = cid.BoundingRectangleProperty;
                    ControlTypeProperty = cid.ControlTypeProperty;
                }
            }

            public bool Equals(DetailedEntity other)
            {

                if (other == null)                
                    return false;

                return CID == other.CID;
            }

            #region Extended Properties
            [XmlIgnore]
            public Entity Parent { get; set; }

            [XmlIgnore]
            public double ScreenTop { get; set; }

            [XmlIgnore]
            public double ScreenLeft { get; set; }

            [XmlAttribute("ID")]
            public string IDProperty { get; set; }

            [XmlAttribute("Name")]
            public string NameProperty { get; set; }

            [XmlAttribute("ControlType")]
            public string ControlTypeProperty { get; set; }

            [XmlAttribute("RuntimeId")]
            public string RuntimeIdProperty { get; set; }

            [XmlAttribute("NativeWindowHandle")]
            public string NativeWindowHandleProperty { get; set; }

            [XmlAttribute("AutomationId")]
            public string AutomationIdProperty { get; set; }

            [XmlAttribute("BoundingRectangle")]
            public string BoundingRectangleProperty { get; set; }

            [XmlAttribute("AcceleratorKey")]
            public string AcceleratorKeyProperty { get; set; }

            [XmlAttribute("AccessKey")]
            public string AccessKeyProperty { get; set; }

            [XmlAttribute("ClassName")]
            public string ClassNameProperty { get; set; }

            [XmlAttribute("ClickablePointProperty")]
            public string ClickablePointProperty { get; set; }

            [XmlAttribute("Culture")]
            public string CultureProperty { get; set; }

            [XmlAttribute("FrameworkId")]
            public string FrameworkIdProperty { get; set; }

            [XmlAttribute("HasKeyboardFocus")]
            public string HasKeyboardFocusProperty { get; set; }

            [XmlAttribute("HelpText")]
            public string HelpTextProperty { get; set; }

            [XmlAttribute("IsContentElement")]
            public string IsContentElementProperty { get; set; }

            [XmlAttribute("IsControlElement")]
            public string IsControlElementProperty { get; set; }

            [XmlAttribute("IsEnabled")]
            public string IsEnabledProperty { get; set; }

            [XmlAttribute("IsKeyboardFocusable")]
            public string IsKeyboardFocusableProperty { get; set; }

            [XmlAttribute("IsOffscreen")]
            public string IsOffscreenProperty { get; set; }

            [XmlAttribute("IsPassword")]
            public string IsPasswordProperty { get; set; }

            [XmlAttribute("IsRequiredForForm")]
            public string IsRequiredForFormProperty { get; set; }

            [XmlAttribute("ItemStatus")]
            public string ItemStatusProperty { get; set; }

            [XmlAttribute("ItemType")]
            public string ItemTypeProperty { get; set; }

            [XmlAttribute("LocalizedControlType")]
            public string LocalizedControlTypeProperty { get; set; }

            [XmlAttribute("LabeledBy")]
            public string LabeledByProperty { get; set; }

            [XmlAttribute("Orientation")]
            public string OrientationProperty { get; set; }

            [XmlAttribute("ProcessId")]
            public string ProcessIdProperty { get; set; }


            #region IsPatternAvailable
            [XmlAttribute("IsDockPatternAvailable")]
            public string IsDockPatternAvailableProperty { get; set; }

            [XmlAttribute("IsExpandCollapsePatternAvailable")]
            public string IsExpandCollapsePatternAvailableProperty { get; set; }

            [XmlAttribute("IsGridItemPatternAvailable")]
            public string IsGridItemPatternAvailableProperty { get; set; }

            [XmlAttribute("IsGridPatternAvailable")]
            public string IsGridPatternAvailableProperty { get; set; }

            [XmlAttribute("IsInvokePatternAvailable")]
            public string IsInvokePatternAvailableProperty { get; set; }

            [XmlAttribute("IsItemContainerPatternAvailable")]
            public string IsItemContainerPatternAvailableProperty { get; set; }

            [XmlAttribute("IsMultipleViewPatternAvailable")]
            public string IsMultipleViewPatternAvailableProperty { get; set; }

            [XmlAttribute("IsRangeValuePatternAvailable")]
            public string IsRangeValuePatternAvailableProperty { get; set; }

            [XmlAttribute("IsScrollItemPatternAvailable")]
            public string IsScrollItemPatternAvailableProperty { get; set; }

            [XmlAttribute("IsScrollPatternAvailable")]
            public string IsScrollPatternAvailableProperty { get; set; }

            [XmlAttribute("IsSelectionItemPatternAvailable")]
            public string IsSelectionItemPatternAvailableProperty { get; set; }

            [XmlAttribute("IsSelectionPatternAvailable")]
            public string IsSelectionPatternAvailableProperty { get; set; }

            [XmlAttribute("IsSynchronizedInputPatternAvailable")]
            public string IsSynchronizedInputPatternAvailableProperty { get; set; }

            [XmlAttribute("IsTableItemPatternAvailable")]
            public string IsTableItemPatternAvailableProperty { get; set; }

            [XmlAttribute("IsTablePatternAvailable")]
            public string IsTablePatternAvailableProperty { get; set; }

            [XmlAttribute("IsTextPatternAvailable")]
            public string IsTextPatternAvailableProperty { get; set; }

            [XmlAttribute("IsTogglePatternAvailable")]
            public string IsTogglePatternAvailableProperty { get; set; }

            [XmlAttribute("IsTransformPatternAvailable")]
            public string IsTransformPatternAvailableProperty { get; set; }

            [XmlAttribute("IsValuePatternAvailable")]
            public string IsValuePatternAvailableProperty { get; set; }

            [XmlAttribute("IsVirtualizedItemPatternAvailable")]
            public string IsVirtualizedItemPatternAvailableProperty { get; set; }

            [XmlAttribute("IsWindowPatternAvailable")]
            public string IsWindowPatternAvailableProperty { get; set; }

            [XmlAttribute("IsLegacyIAccessiblePatternAvailable")]
            public string IsLegacyIAccessiblePatternAvailableProperty { get; set; }

            #endregion

            #region Patterns        
            [XmlElement("DockPattern")]
            public DockPattern DockPatternNode { get; set; }
            [XmlElement("ExpandCollapsePattern")]
            public ExpandCollapsePattern ExpandCollapsePatternNode { get; set; }
            [XmlElement("GridItemPattern")]
            public GridItemPattern GridItemPatternNode { get; set; }
            [XmlElement("GridPattern")]
            public GridPattern GridPatternNode { get; set; }
            [XmlElement("MultipleViewPattern")]
            public MultipleViewPattern MultipleViewPatternNode { get; set; }
            [XmlElement("RangeValuePattern")]
            public RangeValuePattern RangeValuePatternNode { get; set; }
            [XmlElement("ScrollItemPattern")]
            public ScrollItemPattern ScrollItemPatternNode { get; set; }
            [XmlElement("ScrollPattern")]
            public ScrollPattern ScrollPatternNode { get; set; }
            [XmlElement("SelectionItemPattern")]
            public SelectionItemPattern SelectionItemPatternNode { get; set; }
            [XmlElement("SelectionPattern")]
            public SelectionPattern SelectionPatternNode { get; set; }
            [XmlElement("TableItemPattern")]
            public TableItemPattern TableItemPatternNode { get; set; }
            [XmlElement("TablePattern")]
            public TablePattern TablePatternNode { get; set; }
            [XmlElement("TogglePattern")]
            public TogglePattern TogglePatternNode { get; set; }
            [XmlElement("TransformPattern")]
            public TransformPattern TransformPatternNode { get; set; }
            [XmlElement("ValuePattern")]
            public ValuePattern ValuePatternNode { get; set; }
            [XmlElement("WindowPattern")]
            public WindowPattern WindowPatternNode { get; set; }
            [XmlElement("InvokePattern")]
            public InvokePattern InvokePatternNode { get; set; }
            [XmlElement("TextPattern")]
            public TextPattern TextPatternNode { get; set; }
            [XmlElement("LegacyIAccessiblePattern")]
            public LegacyIAccessiblePattern LegacyIAccessiblePatternNode { get; set; }

            #endregion
            #endregion


            public string[] PropertyValues()
            {
                return new string[] { IDProperty, AcceleratorKeyProperty, AccessKeyProperty, AutomationIdProperty, BoundingRectangleProperty, ClassNameProperty, ClickablePointProperty, ControlTypeProperty, CultureProperty, FrameworkIdProperty, HasKeyboardFocusProperty, HelpTextProperty, IsContentElementProperty, IsControlElementProperty, IsEnabledProperty, IsKeyboardFocusableProperty, IsOffscreenProperty, IsPasswordProperty, IsRequiredForFormProperty, ItemStatusProperty, ItemTypeProperty, LocalizedControlTypeProperty, LabeledByProperty, NameProperty, NativeWindowHandleProperty, OrientationProperty, ProcessIdProperty, RuntimeIdProperty, IsDockPatternAvailableProperty, IsExpandCollapsePatternAvailableProperty, IsGridItemPatternAvailableProperty, IsGridPatternAvailableProperty, IsInvokePatternAvailableProperty, IsItemContainerPatternAvailableProperty, IsMultipleViewPatternAvailableProperty, IsRangeValuePatternAvailableProperty, IsScrollItemPatternAvailableProperty, IsScrollPatternAvailableProperty, IsSelectionItemPatternAvailableProperty, IsSelectionPatternAvailableProperty, IsSynchronizedInputPatternAvailableProperty, IsTableItemPatternAvailableProperty, IsTablePatternAvailableProperty, IsTextPatternAvailableProperty, IsTogglePatternAvailableProperty, IsTransformPatternAvailableProperty, IsValuePatternAvailableProperty, IsVirtualizedItemPatternAvailableProperty, IsLegacyIAccessiblePatternAvailableProperty };
            }

            public static string[] PropertyNames()
            {
                return new String[] {
                "IDProperty",
                "AcceleratorKeyProperty",
                "AccessKeyProperty",
                "AutomationIdProperty",
                "BoundingRectangleProperty",
                "NameProperty",
                "ClassNameProperty",
                "ClickablePointProperty",
                "CultureProperty",
                "FrameworkIdProperty",
                "HasKeyboardFocusProperty",
                "HelpTextProperty",
                "IsContentElementProperty",
                "IsControlElementProperty",
                "IsEnabledProperty",
                "IsKeyboardFocusableProperty",
                "IsOffscreenProperty",
                "IsPasswordProperty",
                "IsRequiredForFormProperty",
                "ItemStatusProperty",
                "ItemTypeProperty",
                "LabeledByProperty",
                "NativeWindowHandleProperty",
                "OrientationProperty",
                "ProcessIdProperty",
                "RuntimeIdProperty",
                "IsDockPatternAvailableProperty",
                "IsExpandCollapsePatternAvailableProperty",
                "IsGridItemPatternAvailableProperty",
                "IsGridPatternAvailableProperty",
                "IsInvokePatternAvailableProperty",
                "IsItemContainerPatternAvailableProperty",
                "IsMultipleViewPatternAvailableProperty",
                "IsRangeValuePatternAvailableProperty",
                "IsScrollItemPatternAvailableProperty",
                "IsScrollPatternAvailableProperty",
                "IsSelectionItemPatternAvailableProperty",
                "IsSelectionPatternAvailableProperty",
                "IsSynchronizedInputPatternAvailableProperty",
                "IsTableItemPatternAvailableProperty",
                "IsTablePatternAvailableProperty",
                "IsTextPatternAvailableProperty",
                "IsTogglePatternAvailableProperty",
                "IsTransformPatternAvailableProperty",
                "IsValuePatternAvailableProperty",
                "IsVirtualizedItemPatternAvailableProperty",
                "ControlTypeProperty",
                "LocalizedControlTypeProperty",
                "IsLegacyIAccessiblePatternAvailableProperty"
                };
            }

        }

        [XmlRoot("CompoundID")]
        public class CompoundID
        {
            public enum Index
            {
                IdRuntimeIdProperty = 0,
                IdAutomationIdProperty,
                IdNativeWindowHandleProperty,
                IdClassNameProperty,
                IdNameProperty,
                IdBoundingRectangleProperty,
                IdControlTypeProperty,
                Count
            }

            [XmlIgnore]
            public string[] ids;

            [XmlAttribute("UseNameAsId")]
            public bool UseNameAsId { get; set; }

            [XmlAttribute("RuntimeIdProperty")]
            public string RuntimeIdProperty
            {
                get
                {
                    return ids[(int)Index.IdRuntimeIdProperty];
                }
                set
                {
                    ids[(int)Index.IdRuntimeIdProperty] = value;
                }
            }

            [XmlAttribute("AutomationIdProperty")]
            public string AutomationIdProperty
            {
                get
                {
                    return ids[(int)Index.IdAutomationIdProperty];
                }
                set
                {
                    ids[(int)Index.IdAutomationIdProperty] = value;
                }
            }

            [XmlAttribute("NativeWindowHandleProperty")]
            public string NativeWindowHandleProperty
            {
                get
                {
                    return ids[(int)Index.IdNativeWindowHandleProperty];
                }
                set
                {
                    ids[(int)Index.IdNativeWindowHandleProperty] = value;
                }
            }

            [XmlAttribute("ClassNameProperty")]
            public string ClassNameProperty
            {
                get
                {
                    return ids[(int)Index.IdClassNameProperty];
                }
                set
                {
                    ids[(int)Index.IdClassNameProperty] = value;
                }
            }

            [XmlAttribute("NameProperty")]
            public string NameProperty
            {
                get
                {
                    return ids[(int)Index.IdNameProperty];
                }
                set
                {
                    ids[(int)Index.IdNameProperty] = value;
                }
            }

            [XmlAttribute("BoundingRectangleProperty")]
            public string BoundingRectangleProperty
            {
                get
                {
                    return ids[(int)Index.IdBoundingRectangleProperty];
                }
                set
                {
                    ids[(int)Index.IdBoundingRectangleProperty] = value;
                }
            }

            [XmlAttribute("ControlTypeProperty")]
            public string ControlTypeProperty
            {
                get
                {
                    return ids[(int)Index.IdControlTypeProperty];
                }
                set
                {
                    ids[(int)Index.IdControlTypeProperty] = value;
                }
            }

            public CompoundID()
            {
                ids = new string[(int)Index.Count];
            }

            public CompoundID(string RuntimeIdProperty,
                            string AutomationIdProperty,
                            string NativeWindowHandleProperty,
                            string ClassNameProperty,
                            string NameProperty,
                            string BoundingRectangleProperty,
                            string ControlTypeProperty) : this()
            {
                UseNameAsId = ClassNameProperty != "Edit";
                ids[(int)Index.IdRuntimeIdProperty] = RuntimeIdProperty;
                ids[(int)Index.IdAutomationIdProperty] = AutomationIdProperty;
                ids[(int)Index.IdNativeWindowHandleProperty] = NativeWindowHandleProperty;
                ids[(int)Index.IdClassNameProperty] = ClassNameProperty;
                ids[(int)Index.IdNameProperty] = UseNameAsId ? NameProperty : "";
                ids[(int)Index.IdBoundingRectangleProperty] = BoundingRectangleProperty; //string.Join(",", BoundingRectangleProperty.Split(',').Take(2));
                ids[(int)Index.IdControlTypeProperty] = ControlTypeProperty;
            }

            public CompoundID(DetailedEntity entity) :
                this(entity.RuntimeIdProperty,
                    entity.AutomationIdProperty,
                    entity.NativeWindowHandleProperty,
                    entity.ClassNameProperty,
                    entity.NameProperty,
                    entity.BoundingRectangleProperty,
                    entity.ControlTypeProperty)
            {
                //empty
            }

            public override string ToString()
            {
                return string.Format("<RuntimeId:{0} AutomationId:{1} WindowHandle:{2} Class:{3} Name:{4} Rect:{5} Type:{6}>",
                    RuntimeIdProperty,
                    AutomationIdProperty,
                    NativeWindowHandleProperty,
                    ClassNameProperty,
                    NameProperty,
                    BoundingRectangleProperty,
                    ControlTypeProperty);                
            }

            public override bool Equals(object obj)
            {
                CompoundID other = obj as CompoundID;
                if (other == null) return false;

                if (!string.IsNullOrEmpty(RuntimeIdProperty) &&
                    !string.IsNullOrEmpty(other.RuntimeIdProperty) &&
                    RuntimeIdProperty != other.RuntimeIdProperty)
                    return false;

                if (!string.IsNullOrEmpty(AutomationIdProperty) &&
                    !string.IsNullOrEmpty(other.AutomationIdProperty) &&
                    AutomationIdProperty != other.AutomationIdProperty)
                    return false;

                if (!string.IsNullOrEmpty(NativeWindowHandleProperty) &&
                    !string.IsNullOrEmpty(other.NativeWindowHandleProperty) &&
                    NativeWindowHandleProperty != other.NativeWindowHandleProperty)
                    return false;

                if (!string.IsNullOrEmpty(ClassNameProperty) &&
                    !string.IsNullOrEmpty(other.ClassNameProperty) &&
                    ClassNameProperty != other.ClassNameProperty)
                    return false;

                if (!string.IsNullOrEmpty(NameProperty) &&
                    !string.IsNullOrEmpty(other.NameProperty) &&
                    NameProperty != other.NameProperty)
                    return false;

                if (!string.IsNullOrEmpty(BoundingRectangleProperty) &&
                    !string.IsNullOrEmpty(other.BoundingRectangleProperty) &&
                    BoundingRectangleProperty != other.BoundingRectangleProperty)
                    return false;

                if (!string.IsNullOrEmpty(ControlTypeProperty) &&
                    !string.IsNullOrEmpty(other.ControlTypeProperty) &&
                    ControlTypeProperty != other.ControlTypeProperty)
                    return false;

                return true;
            }

            public override int GetHashCode()
            {
                // ToString().GetHashCode()
                return base.GetHashCode();
            }

        }

        [XmlRoot("DockPattern")]
        public class DockPattern
        {
            [XmlAttribute("DockPositionProperty")]
            public string DockPositionProperty { get; set; }

            [XmlAttribute("Pattern")]
            public string Pattern { get; set; }

            public DockPattern() { }
        }

        [XmlRoot("ExpandCollapsePattern")]
        public class ExpandCollapsePattern
        {
            [XmlAttribute("ExpandCollapseStateProperty")]
            public string ExpandCollapseStateProperty { get; set; }

            [XmlAttribute("Pattern")]
            public string Pattern { get; set; }

            public ExpandCollapsePattern() { }
        }

        [XmlRoot("GridItemPattern")]
        public class GridItemPattern
        {
            [XmlAttribute("ColumnProperty")]
            public string ColumnProperty { get; set; }

            [XmlAttribute("ColumnSpanProperty")]
            public string ColumnSpanProperty { get; set; }

            [XmlAttribute("ContainingGridProperty")]
            public string ContainingGridProperty { get; set; }

            [XmlAttribute("RowProperty")]
            public string RowProperty { get; set; }

            [XmlAttribute("RowSpanProperty")]
            public string RowSpanProperty { get; set; }

            [XmlAttribute("Pattern")]
            public string Pattern { get; set; }

            public GridItemPattern() { }
        }

        [XmlRoot("GridPattern")]
        public class GridPattern
        {
            [XmlAttribute("ColumnCountProperty")]
            public string ColumnCountProperty { get; set; }

            [XmlAttribute("RowCountProperty")]
            public string RowCountProperty { get; set; }

            [XmlAttribute("Pattern")]
            public string Pattern { get; set; }

            public GridPattern() { }
        }

        [XmlRoot("MultipleViewPattern")]
        public class MultipleViewPattern
        {
            [XmlAttribute("CurrentViewProperty")]
            public string CurrentViewProperty { get; set; }

            [XmlAttribute("SupportedViewsProperty")]
            public string SupportedViewsProperty { get; set; }

            [XmlAttribute("Pattern")]
            public string Pattern { get; set; }

            public MultipleViewPattern() { }
        }

        [XmlRoot("RangeValuePattern")]
        public class RangeValuePattern
        {
            [XmlAttribute("IsReadOnlyProperty")]
            public string IsReadOnlyProperty { get; set; }

            [XmlAttribute("LargeChangeProperty")]
            public string LargeChangeProperty { get; set; }

            [XmlAttribute("MaximumProperty")]
            public string MaximumProperty { get; set; }

            [XmlAttribute("MinimumProperty")]
            public string MinimumProperty { get; set; }

            [XmlAttribute("SmallChangeProperty")]
            public string SmallChangeProperty { get; set; }

            [XmlAttribute("ValueProperty")]
            public string ValueProperty { get; set; }

            [XmlAttribute("Pattern")]
            public string Pattern { get; set; }

            public RangeValuePattern() { }
        }

        [XmlRoot("ScrollItemPattern")]
        public class ScrollItemPattern
        {
            [XmlAttribute("Pattern")]
            public string Pattern { get; set; }

            public ScrollItemPattern() { }
        }

        [XmlRoot("ScrollPattern")]
        public class ScrollPattern
        {
            [XmlAttribute("HorizontallyScrollableProperty")]
            public string HorizontallyScrollableProperty { get; set; }

            [XmlAttribute("HorizontalScrollPercentProperty")]
            public string HorizontalScrollPercentProperty { get; set; }

            [XmlAttribute("HorizontalViewSizeProperty")]
            public string HorizontalViewSizeProperty { get; set; }

            [XmlAttribute("NoScroll")]
            public string NoScroll { get; set; }

            [XmlAttribute("VerticallyScrollableProperty")]
            public string VerticallyScrollableProperty { get; set; }

            [XmlAttribute("VerticalScrollPercentProperty")]
            public string VerticalScrollPercentProperty { get; set; }

            [XmlAttribute("VerticalViewSizeProperty")]
            public string VerticalViewSizeProperty { get; set; }

            [XmlAttribute("Pattern")]
            public string Pattern { get; set; }

            public ScrollPattern() { }
        }


        [XmlRoot("SelectionItemPattern")]
        public class SelectionItemPattern
        {
            [XmlAttribute("IsSelectedProperty")]
            public string IsSelectedProperty { get; set; }

            [XmlAttribute("SelectionContainerProperty")]
            public string SelectionContainerProperty { get; set; }

            [XmlAttribute("Pattern")]
            public string Pattern { get; set; }

            public SelectionItemPattern() { }
        }

        [XmlRoot("SelectionPattern")]
        public class SelectionPattern
        {
            [XmlAttribute("CanSelectMultipleProperty")]
            public string CanSelectMultipleProperty { get; set; }

            [XmlAttribute("IsSelectionRequiredProperty")]
            public string IsSelectionRequiredProperty { get; set; }

            [XmlAttribute("SelectionProperty")]
            public string SelectionProperty { get; set; }

            [XmlAttribute("Pattern")]
            public string Pattern { get; set; }

            public SelectionPattern() { }
        }

        [XmlRoot("TableItemPattern")]
        public class TableItemPattern
        {
            [XmlAttribute("ColumnHeaderItemsProperty")]
            public string ColumnHeaderItemsProperty { get; set; }

            [XmlAttribute("RowHeaderItemsProperty")]
            public string RowHeaderItemsProperty { get; set; }

            [XmlAttribute("Pattern")]
            public string Pattern { get; set; }

            public TableItemPattern() { }
        }

        [XmlRoot("TablePattern")]
        public class TablePattern
        {
            [XmlAttribute("ColumnHeadersProperty")]
            public string ColumnHeadersProperty { get; set; }

            [XmlAttribute("RowHeadersProperty")]
            public string RowHeadersProperty { get; set; }

            [XmlAttribute("RowOrColumnMajorProperty")]
            public string RowOrColumnMajorProperty { get; set; }

            [XmlAttribute("Pattern")]
            public string Pattern { get; set; }

            public TablePattern() { }
        }

        [XmlRoot("TogglePattern")]
        public class TogglePattern
        {
            [XmlAttribute("ToggleStateProperty")]
            public string ToggleStateProperty { get; set; }

            [XmlAttribute("Pattern")]
            public string Pattern { get; set; }

            public TogglePattern() { }
        }

        [XmlRoot("TransformPattern")]
        public class TransformPattern
        {
            [XmlAttribute("CanMoveProperty")]
            public string CanMoveProperty { get; set; }

            [XmlAttribute("CanResizeProperty")]
            public string CanResizeProperty { get; set; }

            [XmlAttribute("CanRotateProperty")]
            public string CanRotateProperty { get; set; }

            [XmlAttribute("Pattern")]
            public string Pattern { get; set; }

            public TransformPattern() { }
        }

        [XmlRoot("ValuePattern")]
        public class ValuePattern
        {
            [XmlAttribute("IsReadOnlyProperty")]
            public string IsReadOnlyProperty { get; set; }

            [XmlAttribute("ValueProperty")]
            public string ValueProperty { get; set; }

            [XmlAttribute("Pattern")]
            public string Pattern { get; set; }

            public ValuePattern() { }
        }

        [XmlRoot("WindowPattern")]
        public class WindowPattern
        {
            [XmlAttribute("CanMaximizeProperty")]
            public string CanMaximizeProperty { get; set; }

            [XmlAttribute("CanMinimizeProperty")]
            public string CanMinimizeProperty { get; set; }

            [XmlAttribute("IsModalProperty")]
            public string IsModalProperty { get; set; }

            [XmlAttribute("IsTopmostProperty")]
            public string IsTopmostProperty { get; set; }

            [XmlAttribute("WindowInteractionStateProperty")]
            public string WindowInteractionStateProperty { get; set; }

            [XmlAttribute("WindowVisualStateProperty")]
            public string WindowVisualStateProperty { get; set; }


            [XmlAttribute("Pattern")]
            public string Pattern { get; set; }

            public WindowPattern() { }
        }

        [XmlRoot("InvokePattern")]
        public class InvokePattern
        {
            [XmlAttribute("Pattern")]
            public string Pattern { get; set; }

            public InvokePattern() { }
        }

        [XmlRoot("TextPattern")]
        public class TextPattern
        {
            [XmlAttribute("DocumentRange")]
            public string DocumentRange { get; set; }

            [XmlAttribute("SupportedTextSelection")]
            public string SupportedTextSelection { get; set; }

            [XmlAttribute("AnimationStyleAttribute")]
            public string AnimationStyleAttribute { get; set; }

            [XmlAttribute("BackgroundColorAttribute")]
            public string BackgroundColorAttribute { get; set; }

            [XmlAttribute("BulletStyleAttribute")]
            public string BulletStyleAttribute { get; set; }

            [XmlAttribute("CapStyleAttribute")]
            public string CapStyleAttribute { get; set; }

            [XmlAttribute("CultureAttribute")]
            public string CultureAttribute { get; set; }

            [XmlAttribute("FontNameAttribute")]
            public string FontNameAttribute { get; set; }

            [XmlAttribute("FontSizeAttribute")]
            public string FontSizeAttribute { get; set; }

            [XmlAttribute("FontWeightAttribute")]
            public string FontWeightAttribute { get; set; }

            [XmlAttribute("ForegroundColorAttribute")]
            public string ForegroundColorAttribute { get; set; }

            [XmlAttribute("HorizontalTextAlignmentAttribute")]
            public string HorizontalTextAlignmentAttribute { get; set; }

            [XmlAttribute("IndentationFirstLineAttribute")]
            public string IndentationFirstLineAttribute { get; set; }

            [XmlAttribute("IndentationLeadingAttribute")]
            public string IndentationLeadingAttribute { get; set; }

            [XmlAttribute("IndentationTrailingAttribute")]
            public string IndentationTrailingAttribute { get; set; }

            [XmlAttribute("IsHiddenAttribute")]
            public string IsHiddenAttribute { get; set; }

            [XmlAttribute("IsItalicAttribute")]
            public string IsItalicAttribute { get; set; }

            [XmlAttribute("IsReadOnlyAttribute")]
            public string IsReadOnlyAttribute { get; set; }

            [XmlAttribute("IsSubscriptAttribute")]
            public string IsSubscriptAttribute { get; set; }

            [XmlAttribute("IsSuperscriptAttribute")]
            public string IsSuperscriptAttribute { get; set; }

            [XmlAttribute("MarginBottomAttribute")]
            public string MarginBottomAttribute { get; set; }

            [XmlAttribute("MarginLeadingAttribute")]
            public string MarginLeadingAttribute { get; set; }

            [XmlAttribute("MarginTopAttribute")]
            public string MarginTopAttribute { get; set; }

            [XmlAttribute("MarginTrailingAttribute")]
            public string MarginTrailingAttribute { get; set; }

            [XmlAttribute("MixedAttributeValue")]
            public string MixedAttributeValue { get; set; }

            [XmlAttribute("OutlineStylesAttribute")]
            public string OutlineStylesAttribute { get; set; }

            [XmlAttribute("OverlineColorAttribute")]
            public string OverlineColorAttribute { get; set; }

            [XmlAttribute("OverlineStyleAttribute")]
            public string OverlineStyleAttribute { get; set; }

            [XmlAttribute("StrikethroughColorAttribute")]
            public string StrikethroughColorAttribute { get; set; }

            [XmlAttribute("StrikethroughStyleAttribute")]
            public string StrikethroughStyleAttribute { get; set; }

            [XmlAttribute("TabsAttribute")]
            public string TabsAttribute { get; set; }

            [XmlAttribute("TextFlowDirectionsAttribute")]
            public string TextFlowDirectionsAttribute { get; set; }

            [XmlAttribute("UnderlineColorAttribute")]
            public string UnderlineColorAttribute { get; set; }

            [XmlAttribute("UnderlineStyleAttribute")]
            public string UnderlineStyleAttribute { get; set; }

            [XmlAttribute("Pattern")]
            public string Pattern { get; set; }

            public TextPattern() { }
        }

        [XmlRoot("LegacyIAccessiblePattern")]
        public class LegacyIAccessiblePattern
        {

            [XmlAttribute("ChildId")]
            public string ChildId { get; set; }

            [XmlAttribute("Name")]
            public string Name { get; set; }

            [XmlAttribute("DefaultAction")]
            public string DefaultAction { get; set; }

            [XmlAttribute("Description")]
            public string Description { get; set; }

            [XmlAttribute("Help")]
            public string Help { get; set; }

            [XmlAttribute("KeyboardShortcut")]
            public string KeyboardShortcut { get; set; }

            [XmlAttribute("Role")]
            public string Role { get; set; }

            [XmlAttribute("State")]
            public string CultureAttribute { get; set; }

            [XmlAttribute("Value")]
            public string FontNameAttribute { get; set; }
            
            [XmlAttribute("Pattern")]
            public string Pattern { get; set; }

            public LegacyIAccessiblePattern() { }
        }
    }
    #endregion
}
