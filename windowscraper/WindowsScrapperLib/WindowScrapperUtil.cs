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
using System.Linq;
using System.Windows.Automation;
using System.Collections.Concurrent;
using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace WindowsScraper
{
    namespace Util
    {
        public enum IdType
        {
            RuntimeId,
            ProcessId
        };

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

        public class Win32
        {
            [DllImport("User32.Dll")]
            public static extern long SetCursorPos(int x, int y);

            [DllImport("User32.Dll")]
            public static extern bool ClientToScreen(IntPtr hWnd, ref POINT point);

            [StructLayout(LayoutKind.Sequential)]
            public struct POINT
            {
                public int x;
                public int y;
            }

            [DllImport("user32.dll", EntryPoint = "SystemParametersInfo")]
            public static extern bool SystemParametersInfo(uint uiAction, uint uiParam, ref uint pvParam, uint fWinIni);

            [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
            public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

            [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
            public static extern bool SetForegroundWindow(IntPtr hWnd);

            [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
            public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);

            public const uint MOUSEEVENTF_LEFTDOWN = 0x02;
            public const uint MOUSEEVENTF_LEFTUP = 0x04;
            public const uint MOUSEEVENTF_RIGHTDOWN = 0x08;
            public const uint MOUSEEVENTF_RIGHTUP = 0x10;
            public const uint MOUSEEVENTF_ABSOLUTE = 0x8000;
            public const uint MOUSEEVENTF_MOVE = 0x0001;

        }

        class StructureChangeArg
        {
            public AutomationElement sender;
            public StructureChangedEventArgs e;
            public StructureChangeArg(object obj, StructureChangedEventArgs arg)
            {
                sender = (AutomationElement)obj;
                e = arg;
            }
        }

        class RepeatedRequest
        {
            public string runtimeId;
            public DateTime arrivalTime;
            public RepeatedRequest(string runtimeId, DateTime arrivalTime)
            {
                this.runtimeId = runtimeId;
                this.arrivalTime = arrivalTime;
            }
        }

        class UIAction
        {

            static log4net.ILog log = log4net.LogManager.GetLogger("UIAction");
            public static void PerformDefaultAction(AutomationElement element)
            {
                if (element.TryGetCurrentPattern(InvokePattern.Pattern, out object invokePattern))
                {
                    ((InvokePattern)invokePattern).Invoke();
                }
                else if (element.TryGetCurrentPattern(TogglePattern.Pattern, out object togglePattern))
                {
                    ((TogglePattern)togglePattern).Toggle();
                }
                else if (element.TryGetCurrentPattern(SelectionItemPattern.Pattern, out object selectionPattern))
                {
                    ((SelectionItemPattern)selectionPattern).Select();
                }
                else if (element.TryGetCurrentPattern(ExpandCollapsePatternIdentifiers.Pattern, out object expandCollapsePattern))
                {
                    try
                    {
                        ((ExpandCollapsePattern)expandCollapsePattern).Expand();
                    }
                    catch (Exception ex)
                    {
                        log.Error(ex.ToString());
                    }
                }
            }

            public static void PerformToggleAction(AutomationElement element)
            {
                if (element.TryGetCurrentPattern(TogglePattern.Pattern, out object togglePattern))
                {
                    ((TogglePattern)togglePattern).Toggle();
                }
            }

            public static void PerformSelectionAction(AutomationElement element)
            {
                if (element.TryGetCurrentPattern(SelectionItemPattern.Pattern, out object selectionPattern))
                {
                    ((SelectionItemPattern)selectionPattern).Select();
                }
            }

            public static void PerformExpandAction(AutomationElement element)
            {
                log.InfoFormat("Expand {0}/{1}", element.GetRuntimeId()[0], element.GetRuntimeId()[1]);
                if (element.TryGetCurrentPattern(ExpandCollapsePatternIdentifiers.Pattern, out object expandCollapsePattern))
                {
                    try
                    {
                        ((ExpandCollapsePattern)expandCollapsePattern).Expand();
                    }
                    catch (Exception ex)
                    {
                        log.Error(ex.ToString());
                    }
                }
            }

            public static void PerformExpandAndSelectAction(string runtimeId, List<string[]> menuList)
            {
                log.DebugFormat("Executing ExpandAndSelectAction {0}", runtimeId);
                AutomationElement parentElement = SinterUtil.GetAutomationElementFromId(runtimeId, IdType.RuntimeId);

                if (parentElement.TryGetCurrentPattern(ExpandCollapsePatternIdentifiers.Pattern, out object expandCollapsePattern))
                {
                    log.InfoFormat("ExpandAndSelect: expanding {0}", parentElement.Current.Name);
                    try
                    {
                        ((ExpandCollapsePattern)expandCollapsePattern).Expand();
                    }
                    catch (Exception e)
                    {
                        log.Error(e.ToString());
                    }
                }

                foreach (string[] menu in menuList)
                {
                    Condition propCondition = new PropertyCondition(AutomationElement.NameProperty, menu[0], PropertyConditionFlags.IgnoreCase);
                    AutomationElement targetElement = parentElement.FindFirst(TreeScope.Subtree, propCondition);
                    if (targetElement != null)
                    {
                        PerformDefaultAction(targetElement);
                    }
                    else
                    {
                        log.ErrorFormat("targetElement {0} not found", menu[0]);
                        break;
                    }
                }
            }

            public static void PerformCollapseAction(AutomationElement element)
            {
                if (element.TryGetCurrentPattern(ExpandCollapsePatternIdentifiers.Pattern, out object expandCollapsePattern))
                {
                    ((ExpandCollapsePattern)expandCollapsePattern).Collapse();
                }
            }

            public static void PerformCloseAction(AutomationElement element)
            {
                if (element.TryGetCurrentPattern(WindowPattern.Pattern, out object windowPattern))
                {
                    ((WindowPattern)windowPattern).Close();
                }
            }

        }

        class SinterUtil
        {
            static log4net.ILog log = log4net.LogManager.GetLogger("SinterUtil");
            public static Dictionary<string, string> UIAutomationToARIA =
               new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase)
                   {
                {"button","button"},
                {"checkbox", "checkbox"},
                {"combobox","combobox"},
                {"customcontrol","customcontrol"},
                {"document","document"},
                {"datagrid","grid"},
                {"dataitem","gridcell"},
                {"dialog","dialog"},
                {"edit", "document"},
                {"editbox", "document"},
                {"group","group"},
                {"header", "header"},
                {"hyperlink","link"},
                {"image","img"},
                {"list","list"},
                {"listitem","listitem"},
                {"menu","menu"},
                {"menubar","menubar"},
                {"menuitem","menuitem"},
                {"page","document"},
                {"pane","pane"},
                {"progressbar","progressbar"},
                {"radiobutton","menuitemradio"},
                {"scrollbar","scrollbar"},
                {"separator","separator"},
                {"slider","slider"},
                {"spinner","spinbutton"},
                {"splitbutton","splitbutton"},
                {"statusbar","status"},
                {"tabitem","tab"},
                {"tab","tablist"},
                {"text","textbox"},
                {"thumb", "thumb"},
                {"titlebar", "titlebar"},
                {"toolbar","toolbar"},
                {"tooltip","tooltip"},
                {"tree","tree"},
                {"treeitem","treeitem"},
                {"window", "application"}
                   };

            public static Dictionary<ulong, string> StateDictionary = new Dictionary<ulong, string>()
            {
                {0x2, "STATE_SYSTEM_SELECTED"},
                {0x4, "STATE_SYSTEM_FOCUSED"},
                {0x8, "STATE_SYSTEM_PRESSED"},
                {0x10, "STATE_SYSTEM_CHECKED"},
                {0x20, "STATE_SYSTEM_MIXED"},
                {0x40, "STATE_SYSTEM_READONLY"},
                {0x80, "STATE_SYSTEM_HOTTRACKED"},
                {0x100, "STATE_SYSTEM_DEFAULT"},
                {0x200, "STATE_SYSTEM_EXPANDED"},
                {0x400, "STATE_SYSTEM_COLLAPSED"},
                {0x800, "STATE_SYSTEM_BUSY"},
                {0x1000, "STATE_SYSTEM_FLOATING"},
                {0x2000, "STATE_SYSTEM_MARQUEED"},
                {0x4000, "STATE_SYSTEM_ANIMATED"},
                {0x8000, "STATE_SYSTEM_INVISIBLE"},
                {0x10000, "STATE_SYSTEM_OFFSCREEN"},
                {0x80000, "STATE_SYSTEM_SELFVOICING"},
                {0x400000, "STATE_SYSTEM_LINKED"},
                {0x800000, "STATE_SYSTEM_TRAVERSED"},
                {0x4000000, "STATE_SYSTEM_ALERT_LOW"},
                {0x8000000, "STATE_SYSTEM_ALERT_MEDIUM"},
                {0x10000000, "STATE_SYSTEM_ALERT_HIGH"},
                {0x20000000, "STATE_SYSTEM_PROTECTED"},
                {0x40000000, "STATE_SYSTEM_HASPOPUP"},
                {0x7fffffff, "STATE_SYSTEM_VALID"}
            };

            public static Dictionary<ulong, string> ActionsDictionary = new Dictionary<ulong, string>()
            {
                {0x20000, "STATE_SYSTEM_SIZEABLE"},
                {0x40000, "STATE_SYSTEM_MOVEABLE"},
                {0x100000, "STATE_SYSTEM_FOCUSABLE"},
                {0x200000, "STATE_SYSTEM_SELECTABLE"},
                {0x1000000, "STATE_SYSTEM_MULTISELECTABLE"},
                {0x2000000, "STATE_SYSTEM_EXTSELECTABLE"},
            };

            public static string GetStateString(ulong state)
            {
                string stateString = "";
                foreach (KeyValuePair<ulong, string> entry in StateDictionary)
                {
                    if ((state & entry.Key) != 0)
                    {
                        stateString += entry.Value + ", ";
                    }
                }
                return stateString;
            }

            public static string GetActionString(ulong state)
            {
                string actionString = "";
                foreach (KeyValuePair<ulong, string> entry in ActionsDictionary)
                {
                    if ((state & entry.Key) != 0)
                    {
                        actionString += entry.Value + ", ";
                    }
                }
                return actionString;
            }

            public static string SerializedRuntimeId(int[] runtimeIds)
            {
                string runtimeIdStr = "";
                if (runtimeIds != null)
                {
                    foreach (int i in runtimeIds)
                        runtimeIdStr += i + "/";
                }
                return runtimeIdStr;
            }

            public static string GetRuntimeId(AutomationElement element, bool useCached = false, bool computeSyntheticID = true)
            {
                if (element == null)
                {
                    return null;
                }

                string runtimeIdStr = null;
                try
                {
                    if (computeSyntheticID)
                    {
                        if (element.Current.ControlType == ControlType.List)
                        {
                            System.Windows.Rect rect = element.Current.BoundingRectangle;
                            runtimeIdStr = string.Format("{0}/{1}/{2}/{3}", element.Current.ClassName, element.Current.Name, "" + rect.X, "" + rect.Y);
                            return runtimeIdStr;
                        }
                        /* if (element.Current.ControlType == ControlType.Button || element.Current.ControlType == ControlType.RadioButton)
                                     {
                                         runtimeIdStr = string.Format("{0}/{1}/{2}", element.Current.ClassName, element.Current.Name, element.Current.ControlType);
                                         return runtimeIdStr;
                                     }*/
                    }
                    // regular case
                    int[] runtimeids = useCached ?
                            (int[])element.GetCachedPropertyValue(AutomationElement.RuntimeIdProperty) :
                            (int[])element.GetCurrentPropertyValue(AutomationElement.RuntimeIdProperty);

                    if (runtimeids != null)
                    {
                        foreach (int i in runtimeids)
                            runtimeIdStr += i + "/";
                    }
                    return runtimeIdStr;

                }
                catch (Exception ex)
                {
                    log.ErrorFormat("Exception in getting runtime_id {0}", ex.Message);
                }

                return runtimeIdStr;
            }

            static public AutomationElement GetAutomationElementFromId(String id, IdType processOrRuntimeId)
            {
                AutomationElement element = null;

                if (processOrRuntimeId == IdType.RuntimeId)
                {
                    List<int> runtimeId = new List<int>();
                    foreach (String s in id.Split('/'))
                    {
                        if (Int32.TryParse(s, out int converted))
                            runtimeId.Add(converted);
                    }

                    PropertyCondition findCondition = new PropertyCondition(AutomationElement.RuntimeIdProperty, runtimeId.ToArray());
                    element = AutomationElement.RootElement.FindFirst(TreeScope.Descendants, findCondition);
                    return element;
                }

                if (processOrRuntimeId == IdType.ProcessId)
                {
                    if (Int32.TryParse(id, out int processId))
                    {
                        PropertyCondition processIdProperty = new PropertyCondition(AutomationElement.ProcessIdProperty, processId);
                        PropertyCondition windowProperty = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window);
                        AndCondition findCondition = new AndCondition(processIdProperty, windowProperty);

                        element = AutomationElement.RootElement.FindFirst(TreeScope.Children, findCondition);
                        return element;
                    }
                }

                return null;
            }

            public static void ScreenSize(out int width, out int height)
            {
                System.Windows.Rect screen_rect =
                  (System.Windows.Rect)AutomationElement.RootElement.GetCurrentPropertyValue(AutomationElement.BoundingRectangleProperty);

                width = (int)screen_rect.Width;
                height = (int)screen_rect.Height;
            }

            public static double GetCurrentTime()
            {
                TimeSpan span = DateTime.Now.Subtract(new DateTime(1970, 1, 1, 0, 0, 0));
                return span.TotalMilliseconds;
            }

            public static string GetTimeStamp()
            {
                TimeSpan span = DateTime.Now.Subtract(new DateTime(1970, 1, 1, 0, 0, 0));
                return "" + span.TotalSeconds;
            }
        }

        public class TimerLog
        {
            public static Stopwatch stopwatch = new Stopwatch();
        }

        /* class VersionInfo
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
         }*/



        /*class AutomationElementDictionary
        {
          private ConcurrentDictionary<string, VersionInfo> currentElements = null;
          private AutomationElement applicationRoot = null;
          private TreeWalker treeWalker = null;
          private volatile bool stopRequested = false;
          private Thread managerThread = null;

          private int sleepTimeMs = 5000;

          public AutomationElementDictionary(ref TreeWalker tw, ref AutomationElement root)
          {
            treeWalker = tw;
            applicationRoot = root;
            currentElements = new ConcurrentDictionary<string, VersionInfo>();
          }

          public bool Add(string key, VersionInfo value)
          {
            return currentElements.TryAdd(key, value);
          }

          public bool Get(string key, out VersionInfo value)
          {
            return currentElements.TryGetValue(key, out value);
          }
          public bool ContainsKey(string key)
          {
            return currentElements.ContainsKey(key);
          }

          public int count()
          {
            return currentElements.Count;
          }

          public void StartManagerThread()
          {
            if (managerThread == null)
            {
              managerThread = new Thread(new ThreadStart(this.RecycleDictionary));
              managerThread.Start();
            }
          }

          public void StopManagerThread()
          {
            stopRequested = true;
            managerThread.Join();
          }

          public void FindStaleElements(ref List<string> keys, AutomationElement parent)
          {
            try
            {
              string runtimeId = SinterUtil.GetRuntimeId(parent, true);
              if (runtimeId != null)
              {
                keys.Remove(runtimeId);
              }
              AutomationElement child = treeWalker.GetFirstChild(parent);
              while (child != null)
              {
                FindStaleElements(ref keys, child);
                child = treeWalker.GetNextSibling(child);
              }
            }
            catch
            {
              //System.log.Info("@IgnoreExistingElements: " + ex.Message);
            }
          }

          public void DeleteStaleElements(ref List<string> keys)
          {
            foreach (string k in keys)
            {
              VersionInfo value;
              currentElements.TryRemove(k, out value);
            }
          }

          public void RecycleDictionary()
          {
            int count_before, count_after;
            while (!stopRequested)
            {
              List<string> keys = currentElements.Keys.ToList();
              count_before = keys.Count;

              FindStaleElements(ref keys, applicationRoot);
              count_after = keys.Count;

              DeleteStaleElements(ref keys);
              //System.log.Info("#keys:: before:{0}, stale:{1}, after:{2}", count_before, count_after, currentElements.Count);

              Thread.Sleep(sleepTimeMs);
            }
          }
        }*/


    }
}
