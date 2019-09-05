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
using System.Windows.Automation;
using System.Diagnostics;
using System.Windows.Automation.Text;
using System.Threading;
using System.Windows;
//using UIAComWrapperInternal;
using System.Collections.Concurrent;
using System.Windows.Forms;
using Sintering;
using LinqAndTrie;

using System.Linq;
using WindowsScraper.Util;

namespace WindowsScraper
{

    public class WindowsScraper : IWinCommands
    {
        private Thread ThreadFocusThrottler = null;
        private ConcurrentStack<RepeatedRequest> _repeatedRequestStack = new ConcurrentStack<RepeatedRequest>();
        BlockingCollection<RepeatedRequest> repeatedRequestStack = null;

        public TreeWalker treeWalker = null;

        public AutomationElement applicationRootElement = null;
        private int requestedProcessId = 0;
        private string requestedProcessRuntimeId;
        private ConcurrentDictionary<string, bool> desktopDictionary = new ConcurrentDictionary<string, bool>();

        private AutomationElement lastActiveMenu = null;

        private Trie<int[], Entity, int> automationElementTrie = null;

        Condition listItemCondition;

        private static readonly log4net.ILog log = Logger.Create();

        TimeSpan DEFAULT_WAIT_TIME = new TimeSpan(0, 0, 0, 0, 5);

        Dictionary<string, int> serviceCodes;
        Dictionary<int, string> serviceCodesRev;
      	Dictionary<int, string> sendKeysCodes;

        private string passcode;
        public bool bPasscodeVerified { get; private set; }

        public WindowsScraper(string passcode)
        {
            this.passcode = passcode;
            this.bPasscodeVerified = false;

            // construct DOM tree walker except this application
            Condition condition1 = new PropertyCondition(AutomationElement.ProcessIdProperty, Process.GetCurrentProcess().Id);
            Condition condition2 = new AndCondition(new Condition[] { Automation.RawViewCondition, new NotCondition(condition1) });
            treeWalker = new TreeWalker(condition2);

            // initialize some utiity stacks
            repeatedRequestStack = new BlockingCollection<RepeatedRequest>(_repeatedRequestStack);

            // initialize list-condition for faster list serialization
            listItemCondition = new OrCondition(
              new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Header),
              new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.ListItem)
            );

            // loading the service_code dictionary
            Dictionary<string, object> serviceCodesTemp = Config.getConfig("service_code");
            if (serviceCodesTemp != null)
            {
                serviceCodes = serviceCodesTemp.ToDictionary(pair => pair.Key, pair => (int)pair.Value);
                serviceCodesRev = serviceCodes.ToDictionary(pair => pair.Value, pair => pair.Key);
            }
            else
            {
                Console.WriteLine("Unable to load service_code dictionary");
            }

            Dictionary<string, object> keyCodesTemp = Config.getConfig("send_key_codes");

        	  if (keyCodesTemp != null)
        	  {
            		sendKeysCodes = keyCodesTemp.ToDictionary(pair => Int32.Parse(pair.Key), pair => (string)pair.Value);
        	  }
        }

        public void LogTime(string arg)
        {
            TimerLog.stopwatch.Stop();
            string logStr = String.Format("\t{0}  update detection time {1}", arg,
              TimerLog.stopwatch.ElapsedMilliseconds);
            log.Info(logStr);
            Console.WriteLine(logStr);
        }

        private static bool bDesktopHookAdded = false;

        public bool ScreenReaderFlag
        {
            get
            {
                int SPI_GETSCREENREADER = 70;
                uint isRunning = 0;
                Win32.SystemParametersInfo((uint)SPI_GETSCREENREADER, 0, ref isRunning, 0);
                return isRunning > 0 ? true : false;
            }

            set
            {
                //SPI_SETSCREENREADER 0x0047
                //SPI_SETSCREENREADER=71
                int SPI_SETSCREENREADER = 71;
                int SPIF_UPDATEINIFILE = 1;
                int SPIF_SENDCHANGE = 2;
                uint isRunning = 0;

                if (value)
                {
                    Win32.SystemParametersInfo((uint)SPI_SETSCREENREADER,
                      1,
                      ref isRunning,
                      (uint)(SPIF_UPDATEINIFILE | SPIF_SENDCHANGE)
                    );
                }
            }
        }



        // register hooks
        public void RegisterDesktopHooks()
        {
            // register scraper as screen-reader
            ScreenReaderFlag = true;


            if (!bDesktopHookAdded)
            {
                bDesktopHookAdded = true;
                // structureChanged
                // Automation.AddStructureChangedEventHandler(AutomationElement.RootElement, TreeScope.Children, new StructureChangedEventHandler(OnStructureChanged));
                // focusChanged

                Automation.AddAutomationFocusChangedEventHandler(new AutomationFocusChangedEventHandler(OnFocusChanged));

                // windowOpened, windowClosed
                //Automation.AddAutomationEventHandler(WindowPatternIdentifiers.WindowOpenedEvent , AutomationElement.RootElement , TreeScope.Subtree , new AutomationEventHandler(OnWindowOpened));

                Automation.AddAutomationEventHandler(WindowPatternIdentifiers.WindowClosedEvent, AutomationElement.RootElement, TreeScope.Subtree, new AutomationEventHandler(OnWindowClosed));

                // menuOpened, menuClosed
                Automation.AddAutomationEventHandler(AutomationElement.MenuOpenedEvent, AutomationElement.RootElement, System.Windows.Automation.TreeScope.Subtree, new AutomationEventHandler(OnMenuOpened));
                // Automation.AddAutomationEventHandler(AutomationElement.MenuClosedEvent, AutomationElement.RootElement, System.Windows.Automation.TreeScope.Subtree, new AutomationEventHandler(OnMenuClosed));

                Automation.AddAutomationPropertyChangedEventHandler(AutomationElement.RootElement, TreeScope.Children, new AutomationPropertyChangedEventHandler(OnPropertyChangeGlobal), new AutomationProperty[] { AutomationElement.BoundingRectangleProperty,
                    });

            }
        }



        private bool IsCached(AutomationElement element)
        {
            string uniqueId = SinterUtil.GetRuntimeId(element, true);
            if (uniqueId == null)
            {
                return false;
            }
            //TODO
            return automationElementTrie.ContainsKey(element.GetRuntimeId());
            //return automationElementDictionary.ContainsKey(uniqueId);
        }

        private AutomationElement GetAnchorElementFromCache(AutomationElement element)
        {
            AutomationElement parent = element;
            try
            {
                bool cached = false;
                do
                {
                    parent = treeWalker.GetParent(parent);
                    cached = IsCached(parent);
                } while (parent != null && !cached);
                return parent;
            }
            catch (Exception)
            {
                // System.Console.WriteLine("In GetAnchorElement: " + exception.Message);
            }

            return null;
        }

        #region UIAutomation Action
        public void executeSetProcessToForeground(string pid)
        {
            AutomationElement element = SinterUtil.GetAutomationElementFromId(pid, IdType.ProcessId);
            if (null == element)
            {
                return;
            }

            int SHOW_DEFAULT = 10;
            Win32.ShowWindow((IntPtr)element.Current.NativeWindowHandle, SHOW_DEFAULT);
            Win32.SetForegroundWindow((IntPtr)element.Current.NativeWindowHandle);
        }

        public void SetFocus(string runtimeId)
        {
            try
            {
                AutomationElement element = SinterUtil.GetAutomationElementFromId(runtimeId, IdType.RuntimeId);
                if (element != null)
                {
                    Console.WriteLine("Focus {0}", element);
                    element.SetFocus();
                    if (element.TryGetCurrentPattern(SelectionItemPatternIdentifiers.Pattern, out object selectionPattern))
                    {
                        ((SelectionItemPattern)selectionPattern).Select();
                        //Console.WriteLine("Done setting focus {0}", runtimeId);
                    }
                }
            }
            catch
            {
                Console.WriteLine("problem with SetFocus {0}", runtimeId);
            }
        }
        public void executeSetText(string runtimeId, string text = "")
        {
            try
            {
                AutomationElement element = SinterUtil.GetAutomationElementFromId(runtimeId, IdType.RuntimeId);
                if (element != null && element.Current.IsEnabled && element.Current.IsKeyboardFocusable)
                {
                    //element.set
                    Object valuePattern;
                    if (element.TryGetCurrentPattern(ValuePattern.Pattern, out valuePattern))
                    {
                        ((ValuePattern)valuePattern).SetValue(text);
                    }
                    else
                    {   //send keystrokes
                        // Set focus for input functionality and begin.
                        element.SetFocus();

                        // Pause before sending keyboard input.
                        Thread.Sleep(100);
                        SendKeys.SendWait("^{HOME}");   // Move to start of control
                        SendKeys.SendWait("^+{END}");   // Select everything
                        SendKeys.SendWait("{DEL}");     // Delete selection
                        SendKeys.SendWait(text);
                    }
                }
            }
            catch
            {
                Console.WriteLine("problem with SetText {0}", runtimeId);
            }
        }

        public void executeAppendText(string runtimeId, string text = "")
        {
            try
            {
                AutomationElement element = SinterUtil.GetAutomationElementFromId(runtimeId, IdType.RuntimeId);
                if (element != null && element.Current.IsEnabled && element.Current.IsKeyboardFocusable)
                {
                    SendKeys.SendWait(text);
                }
            }
            catch
            {
                Console.WriteLine("problem with SetText {0}", runtimeId);
            }
        }

        public void executeDeltaFocus(string runtimeId, string hash)
        {
            try
            {
                AutomationElement element = SinterUtil.GetAutomationElementFromId(runtimeId, IdType.RuntimeId);
                Console.WriteLine("Execute Focus {0}", element);
                if (element != null && element.Current.ControlType != ControlType.Window)
                {
                    element.SetFocus();
                    if (element.TryGetCurrentPattern(SelectionItemPatternIdentifiers.Pattern, out object selectionPattern))
                        ((SelectionItemPattern)selectionPattern).Select();

                    if (element.Current.Name != hash)
                    {
                        // send the up to-date info
                        Sinter xmlDoc = new Sinter
                        {
                            HeaderNode = MsgUtil.BuildHeader(serviceCodes["delta"], serviceCodes["delta_subtree_replace"]),
                            EntityNode = UIAElement2EntityRecursive(element),
                        };

                        connection.SendMessage(xmlDoc);
                    }
                }
            }
            catch
            {
                Console.WriteLine("problem with DeltaFocus {0}", runtimeId);
            }
        }


        /* remove unused code */
        /*
        private void RegisterStructureChangedNotification(AutomationElement element)
        {
            if (element != null)
            {
                Automation.AddStructureChangedEventHandler(element, TreeScope.Subtree, new StructureChangedEventHandler(OnStructureChangedLocal));
                Console.WriteLine("successfully registered structureChangeNotification");
            }
            else
            {
                Console.WriteLine("registration of structureChangeNotification failed");
            }
        }
        */

        public void executeCaretMoveOrSelection(string runtimeId, uint location, uint length)
        {
            AutomationElement element = SinterUtil.GetAutomationElementFromId(runtimeId, IdType.RuntimeId);
            if (element == null)
            {
                Console.WriteLine("Unable to get Automation Element");
                return;
            }
            TextPattern textPattern = element.GetCurrentPattern(TextPattern.Pattern) as TextPattern;
            if (textPattern == null)
            {
                Console.WriteLine("Unable to get TextPattern");
                return;
            }
            if (textPattern.SupportedTextSelection == SupportedTextSelection.None)
            {
                Console.WriteLine("No SupportedTextSelection");
                return;
            }

            // get entire range
            TextPatternRange range1 = textPattern.DocumentRange.Clone();
            //move the beginning of range to 'location'
            range1.MoveEndpointByUnit(TextPatternRangeEndpoint.Start, TextUnit.Character, (int)location);
            TextPatternRange range2 = range1.Clone();
            // move the ending of range2 to the beginning of range1
            range2.MoveEndpointByRange(TextPatternRangeEndpoint.End, range1, TextPatternRangeEndpoint.Start);
            // now begining and end both point to same location, so move it by 'length' amount
            range2.MoveEndpointByUnit(TextPatternRangeEndpoint.End, TextUnit.Character, (int)length);
            range2.Select();

        }


        #endregion//

        #region Event or Property Handlers
        private void OnWindowOpened(object obj, AutomationEventArgs e)
        {
            AutomationElement element = (AutomationElement)obj;
            if (element.Current.ProcessId == requestedProcessId)
            {
                System.Console.WriteLine("Window Opened " + SinterUtil.GetRuntimeId(element) + " " + element.Current.Name + " " + element.Current.LocalizedControlType);
                desktopDictionary.TryAdd(SinterUtil.GetRuntimeId(element), true);

                // @todo: fix synchronization between this one and structure changed callback
                /*
                XmlDocument xmlDoc = create_xml(element, (int)SERVICE_CODES.COMMAND_UPDATE);
                if (xmlDoc == null)
                {
                    return;
                }
                xmlDoc.Save("Window_opened" + GetTimeStamp() + ".xml");
                */
            }
        }

        private void OnWindowOpenedLocal(object obj, AutomationEventArgs e)
        {
            AutomationElement element = (AutomationElement)obj;
            if (element.Current.ProcessId == requestedProcessId)
            {
                Console.WriteLine("Window Opened " + SinterUtil.GetRuntimeId(element) + " " + element.Current.Name + " " + element.Current.LocalizedControlType);

                // send an LS_WINDOW msg as if it were a new window
                Sinter sinter = new Sinter
                {
                    HeaderNode = MsgUtil.BuildHeader(serviceCodes["ls_l_res"], serviceCodes["ls_l_res_dialog"]),
                    EntityNode = UIAElement2EntityRecursive(element),
                };

                connection.SendMessage(sinter);
            }
        }

        private void OnWindowClosedLocal(object obj, AutomationEventArgs _e)
        {
            WindowClosedEventArgs e = _e as WindowClosedEventArgs;
            int[] runtimeId = e.GetRuntimeId();
            string stringRuntimeId = SinterUtil.SerializedRuntimeId(e.GetRuntimeId());

            if (automationElementTrie.ContainsKey(runtimeId))
            {
                //DeltaForClose(stringRuntimeId);
                DeltaForClose(null); // targetID = 'null' will make client close all windows of the processID. 
                                     // this is different from OnWindowClosed(), which close only specific targetID.
                Console.WriteLine("Window Closed Locally " + runtimeId);
            }
        }

        //Every windows under AutomationElement.RootElement will trigger this event. 
        private void OnWindowClosed(object obj, AutomationEventArgs _e)
        {
            WindowClosedEventArgs e = _e as WindowClosedEventArgs;
            int[] runtimeId = e.GetRuntimeId();
            string stringRuntimeId = SinterUtil.SerializedRuntimeId(e.GetRuntimeId());



            if (automationElementTrie.ContainsKey(runtimeId))
            {
                if (automationElementTrie.TryGetValue(runtimeId, out Entity entity))
                {
                    if (entity.Type == "Pane")
                    {
                        Console.WriteLine(entity);
                        foreach (Entity child in entity.Children)
                        {
                            Console.WriteLine("Window Closed Globally: " + child.Type);
                            DeltaForClose(child.UniqueID);
                        }
                    }
                    else
                    {
                        Console.WriteLine("Window Closed Globally: " + entity.Type);
                        DeltaForClose(stringRuntimeId);
                    }
                }
            }
        }

        private void OnMenuOpened(object obj, AutomationEventArgs e)
        {
            Console.WriteLine("Opened Menu");
            AutomationElement element = (AutomationElement)obj;
            Sinter xmlDoc = null;
            try
            {
                if (element.Current.ProcessId == requestedProcessId)
                {
                    desktopDictionary.TryAdd(SinterUtil.GetRuntimeId(element), true);

                    Header header = null;
                    if (element.Current.Name == "Context")
                    {
                        header = MsgUtil.BuildHeader(serviceCodes["delta"], serviceCodes["delta_subtree_context_menu"]);
                        Console.WriteLine("OnContextMenuOpened " + element.Current.Name + " " + element.Current.LocalizedControlType);
                    }
                    else if (element.Current.LocalizedControlType == "menu" && lastActiveMenu != null)
                    {
                        // Menu was expanded, lets send it as a child of last active menu
                        string target_id = SinterUtil.GetRuntimeId(lastActiveMenu, true);
                        header = MsgUtil.BuildHeader(serviceCodes["delta"], serviceCodes["delta_subtree_menu"], target_id);
                    }
                    else
                    {
                        // string target_id = SinterUtil.GetRuntimeId(lastActiveMenu, true);
                        header = MsgUtil.BuildHeader(serviceCodes["delta"], serviceCodes["delta_subtree_menu"]);
                    }

                    //send xml
                    if (header != null)
                    {
                        xmlDoc = new Sinter
                        {
                            HeaderNode = header,
                            EntityNode = UIAElement2EntityRecursive(element),
                        };

                        connection.SendMessage(xmlDoc);
                    }
                }
            }
            catch
            {
                Console.WriteLine("Problem in MenuOpened");
            }
        }

        private void OnMenuClosed(object obj, AutomationEventArgs e)
        {
            AutomationElement element = (AutomationElement)obj;
            string rid = SinterUtil.GetRuntimeId(element, true);
            /*
            bool outValue;
            if (rid != null && desktopDictionary.TryRemove(rid, out outValue)) {
              System.Console.WriteLine("OnMenuClosed " + rid);
              //DeltaForClose(rid, "child_removed");
            }
            */
        }
        private void OnFocusChanged(object src, AutomationFocusChangedEventArgs e)
        {
        /*    AutomationElement element = (AutomationElement)src;
            if (element.Current.ProcessId != requestedProcessId)
            {
                return;
            } */
            /*
          if (element.Current.ControlType == ControlType.Text) {
              DeltaGeneric(element);
          }*/
        }

        private void OnPropertyChange(object sender, AutomationPropertyChangedEventArgs e)
        {
            AutomationElement element = (AutomationElement)sender;

            //Console.WriteLine("On Property Change {0}", e.Property.ProgrammaticName);

            // Property: IsOffScreen
            if (e.Property == AutomationElement.IsOffscreenProperty)
            {
                // TO DO: later
            }
            // Property: BoundingRectangle
            else if (e.Property == AutomationElement.BoundingRectangleProperty)
            {
                Console.WriteLine("Properties {0} {1} {2}", element.Current.ControlType.ProgrammaticName, element.Current.Name, e.Property.ProgrammaticName);
                //Console.WriteLine("New Value Width {0} Height {1} X {2} Y {3} {4}", element.Current.BoundingRectangle.Width, element.Current.BoundingRectangle.Height, element.Current.BoundingRectangle.X, element.Current.BoundingRectangle.Y, e.NewValue);
                // Subproperty: List
                if (element.Current.ControlType == ControlType.List)
                {
                    DeltaSpecialList(element);
                } //  Subproperty: ComboBox
                  //else if (element.Current.ControlType == ControlType.ComboBox) {
                  //Console.WriteLine("box: {0} {1} ", element.Current.ControlType.ProgrammaticName, element.Current.Name);
                  //DeltaComboBox(element);
                  //} //  Subproperty: Breadcrumb , Toolbar
                  //else if (element.Current.ControlType == ControlType.Window) {
                  //  DeltaGenericWindow(element);
                  //}
                else if (element.Current.ControlType == ControlType.ToolBar)
                {
                    Console.WriteLine("EXECUTING");
                    DeltaGeneric(element);
                }
                else if (element.Current.ControlType == ControlType.Window)
                {
                    DeltaGenericWindowSize(element);
                }
                else
                {
                    // not required
                }
            }
            // Property: ExpandCollapseState
            else if (e.Property == ExpandCollapsePattern.ExpandCollapseStateProperty)
            {
                // if menu triggered expandCollapse event then ignore
                /*
                if(element.Current.ControlType == ControlType.Menu ||
                   element.Current.ControlType == ControlType.MenuItem ||
                   element.Current.ControlType == ControlType.MenuBar) {
                    return;
                }
                */
                DeltaSpecialTree(element, (ExpandCollapseState)e.NewValue);
            }
            // Property: ValueChanged
            else if (e.Property == ValuePattern.ValueProperty)
            {
                Console.WriteLine("valueChanged {0} {1}", element.Current.Name, e.NewValue);
                DeltaGenericHash(element);
                //MarkUpdateRequired(element);
            }//update required
            else if (e.Property == RangeValuePattern.ValueProperty)
            {
                Console.WriteLine("RangeValuePattern value {0} {1}", element.Current.Name, e.NewValue);
                DeltaGenericHash(element, e.NewValue.ToString());
            }
            else if (e.Property == AutomationElement.NameProperty &&
                (element.Current.ControlType == ControlType.Text ||
                element.Current.ControlType == ControlType.Button))
            {
                //Console.WriteLine("NameChanged {0} {1}", element.Current.Name, e.NewValue);
                DeltaGenericHash(element);
            }
            if (e.Property == SelectionItemPattern.IsSelectedProperty)
            {
                DeltaGeneric(element);
            }
            else
            {
                // not required now
            }
        }

        private void OnPropertyChangeGlobal(object sender, AutomationPropertyChangedEventArgs e)
        {
            AutomationElement element = (AutomationElement)sender;
            // Console.WriteLine("Property Change Global");

            // Property: BoundingRectangle
            // Console.WriteLine("Global Box, {0}", element.Current.ControlType.ProgrammaticName);
            // Console.WriteLine("Properties {0} {1} {2} {3}", element.Current.ControlType, e.Property, element.Current.Name, e.ToString());
            if (e.Property == AutomationElement.BoundingRectangleProperty)
            {
                // Subproperty: List
              /*  if (element.Current.ControlType == ControlType.List)
                {
                    Console.WriteLine("PropertyChange: Delta");
                    DeltaSpecialList(element);

                } */ //  Subproperty: ComboBox
            }
        }

        private Object stackLock = new Object();

        private void HandlerFocusThrottler()
        {
            RepeatedRequest focus, top;
            while (true)
            {
                focus = repeatedRequestStack.Take();
                SetFocus(focus.runtimeId);
                // check if the stack has more recent data, otherwise clear old stuffs
                if (_repeatedRequestStack.TryPeek(out top)
                    && (top.arrivalTime <= focus.arrivalTime))
                {
                    while (repeatedRequestStack.Count > 0)
                        repeatedRequestStack.TryTake(out top);
                }
            }
        }

        private void OnStructureChanged(object sender, StructureChangedEventArgs e)
        {
            //if (e.StructureChangeType != StructureChangeType.ChildAdded &&
            //  e.StructureChangeType != StructureChangeType.ChildrenBulkAdded) {
            //return;
            //}

            //Console.WriteLine("Structure Changed Global");
            AutomationElement element = (AutomationElement)sender;
            Console.WriteLine("Struct changed global {0}", element.Current.ControlType.ProgrammaticName);

            //if (element.Current.ControlType == ControlType.List) {
            // DeltaList(element);
            //}
            if (element.Current.ClassName == "Auto-Suggest Dropdown")
            {
                Console.WriteLine("Auto-Suggest Dropdown created {0} {1}", element.Current.ControlType.ProgrammaticName, e.StructureChangeType);
                DeltaSpecialList(element);
            }
        }

        
        private void OnStructureChangedLocal(object sender, StructureChangedEventArgs e)
        {
            if (e.StructureChangeType != StructureChangeType.ChildAdded)
            {
                return;
            }

            Console.WriteLine("OnStructureChangedLocal");
            AutomationElement element = (AutomationElement)sender;
            AutomationElementCollection elementCollection = element.FindAll(TreeScope.Children, Condition.TrueCondition);

            // Console.WriteLine("Local Structure {0}", element.Current.ControlType);
            //Console.WriteLine("my {0} {1}", element.Current.ControlType.ProgrammaticName, element.Current.Name);

            if (element.Current.ControlType == ControlType.SplitButton)
            {
                DeltaGenericAnchor(element);
            }
            if (element.Current.ControlType == ControlType.ProgressBar)
            {
                DeltaGenericImmediate(element);
            }

            if (element.Current.Name != "View")
            {
                if (element.Current.LocalizedControlType != "text") //already sent the msg 511 (delta_prop_change_value)
                {
                    DeltaGeneric(element);
                }
            }

        }

        private void StructureChangeHandler()
        {
            Console.Write("Structure Change Handler");
            /*
            XmlDocument xmlDoc = null;
            VersionInfo vInfo;

            while (true)
            {
                xmlDoc = null;
                try
                {
                    StructureChangeArg arg = structureChangeQueue.Take();
                    AutomationElement element = arg.sender;
                    StructureChangedEventArgs e = arg.e;

                    if (element != null && element.Current.ControlType == ControlType.List)
                    {
                        if (!IsCached(element)) continue;
                        string syntheticID = SinterUtil.GetRuntimeId(element, true);
                        string trueID = SinterUtil.GetRuntimeId(element, true, false);
                        if (automationElementDictionary.Get(syntheticID, out vInfo))
                        {
                            if (vInfo != null && vInfo.runtimeID != trueID)
                            {   // list-item has been recreated
                                vInfo.runtimeID = trueID;
                                vInfo.version = VERSION.UPDATED;
                                xmlDoc = create_xml(element, SERVICE_CODES.COMMAND_UPDATE, "child_replaced");
                            }
                        }
                    }
                }
                catch
                {
                }

                //send XML
                if (xmlDoc != null)
                {
                    //xmlDoc.Save(GetTimeStamp() + ".xml");
                    sendMessage(xmlDoc);
                }
            }
             */
        }
        #endregion


        //Stopwatch uiStopwatch = new Stopwatch();

        private void MarkUpdateRequired(AutomationElement element)
        {
            // find anchor node
            AutomationElement anchor = element;
            if (!IsCached(element))
            {
                anchor = GetAnchorElementFromCache(element);
            }
            // mark update required flag
            if (anchor != null)
            {
                VersionInfo vInfo;
                Entity entity;
                int[] id = element.GetRuntimeId();
                if (automationElementTrie.TryGetValue(id, out entity))
                {
                    vInfo = entity.versionInfo;
                    if (vInfo != null)
                        vInfo.version = Sintering.Version.Updated;
                }
            }
        }

        //  sends subtree rooted at element or its anchor if not found in cache
        private void DeltaGeneric(AutomationElement element)
        {
            // find anchor node
            AutomationElement anchor = element;
            if (anchor.Current.ControlType == ControlType.Window && !anchor.Current.LocalizedControlType.Equals("Dialog"))
                return;

            //if (anchor.Current.ControlType != ControlType.Pane)
              //  return;

            if (!IsCached(element))
                anchor = GetAnchorElementFromCache(element);

            //always sends update
            if (anchor != null)
            {
                Console.WriteLine("DeltaGeneric");
                SinterUtil.ScreenSize(out int width, out int height);

                Header header = MsgUtil.BuildHeader(serviceCodes["delta"], serviceCodes["delta_subtree_replace"]);
                header.ParamsInfo = new Params
                {
                    Data1 = width.ToString(),
                    Data2 = height.ToString(),
                };
                Sinter sinter = new Sinter
                {
                    HeaderNode = header,
                    EntityNode = UIAElement2EntityRecursive(anchor),
                };

                //Console.WriteLine("Chillins");
                //Console.WriteLine("{0} {1}", sinter.EntityNode.Type, sinter.EntityNode.Name);
                PrintChildrenNodes(sinter.EntityNode);

                // send
                connection.SendMessage(sinter);
            }
        }

        private void PrintChildrenNodes(Entity node)
        {
            if (node.Children != null)
            {
                foreach (Entity child_entity in node.Children)
                {
                    Console.WriteLine("PrintChildrenNodes {0} {1}", child_entity.Type, child_entity.Name);
                    PrintChildrenNodes(child_entity);
                }
            }
        }

        private void DeltaGenericHash(AutomationElement element, string hash = "")
        {
            Console.WriteLine("DeltaGenericHash");
            if (string.IsNullOrEmpty(hash))
            {
                object value = element.GetCurrentPropertyValue(ValuePattern.ValueProperty, true);
                if (value == AutomationElement.NotSupported)
                    hash = element.Current.Name;
                else
                    hash = value as string;
            }

            if (!IsCached(element))
                return;

            string runtimeId = SinterUtil.GetRuntimeId(element);
            int[] id = element.GetRuntimeId();
            VersionInfo vInfo;
            Entity entity;
            if (automationElementTrie.TryGetValue(id, out entity))
            {
                vInfo = entity.versionInfo;
                if (vInfo != null && (vInfo.version == Sintering.Version.Init || vInfo.Hash != hash))
                {
                    int subCode = 0;
                    if (element.Current.ControlType == ControlType.Text)
                    {
                        Console.WriteLine("delta_prop_change_value");
                        subCode = serviceCodes["delta_prop_change_value"];
                    }
                    else
                        subCode = serviceCodes["delta_prop_change_name"];

                    // build sinter message
                    Sinter sinter = new Sinter
                    {
                        HeaderNode = MsgUtil.BuildHeader(serviceCodes["delta"], subCode, runtimeId, vInfo.Hash, hash),
                    };

                    // update the value
                    vInfo.version = Sintering.Version.Updated;
                    vInfo.Hash = hash;

                    //send delta
                    Console.WriteLine("Sending from DeltaGenericHash");
                    connection.SendMessage(sinter);
                }
            }
        }

        //  sends subtree rooted at element or its anchor if not found in cache
        private void DeltaGenericWindow(AutomationElement element)
        {
            if (IsCached(element))
            {
                string id = SinterUtil.GetRuntimeId(element);
                int[] runtimeId = element.GetRuntimeId();
                Entity entity;
                VersionInfo vInfo;

                if ((id == requestedProcessRuntimeId) && automationElementTrie.TryGetValue(runtimeId, out entity))
                {
                    vInfo = entity.versionInfo;
                    if (vInfo.version == Sintering.Version.Updated)
                    {
                        Sinter sinter = new Sinter
                        {
                            HeaderNode = MsgUtil.BuildHeader(serviceCodes["delta"], serviceCodes["delta_subtree_dialog"]),
                            EntityNode = UIAElement2EntityRecursive(element),
                        };

                        vInfo.version = Sintering.Version.None;

                        // send
                        Console.WriteLine("Sending new Window");
                        connection.SendMessage(sinter);
                    }
                }
            }
        }

        private void DeltaGenericWindowSize(AutomationElement element)
        {
            Console.WriteLine("Generic Window");
            if (IsCached(element))
            {
                string id = SinterUtil.GetRuntimeId(element);
                int[] runtimeId = element.GetRuntimeId();
                Entity entity;
                VersionInfo vInfo;

                if ((id == requestedProcessRuntimeId) && automationElementTrie.TryGetValue(runtimeId, out entity))
                {
                    Console.WriteLine("Got Id");
                    vInfo = entity.versionInfo;
                    //if (vInfo.version == Sintering.Version.Updated)
                    //{
                        Sinter sinter = new Sinter
                        {
                            HeaderNode = MsgUtil.BuildHeader(serviceCodes["delta"], serviceCodes["delta_prop_change_value"]),
                            EntityNode = UIAElement2EntityRecursive(element),
                        };

                        //vInfo.version = Sintering.Version.None;

                        // send
                        Console.WriteLine("Sending new Window");
                        if ((sinter.EntityNode.States & States.DISABLED) != 0)
                        {
                            return; //'DISABLE' states confuses proxy. if it is disabled, scraper no need to send size change anyway 
                        }
                        connection.SendMessage(sinter);
                    //}
                }
            }
        }

        // sends subtree rooted at element's anchor (or parent)
        private void DeltaGenericAnchor(AutomationElement element)
        {
            //always sends update on anchor
            if (element.Current.ControlType == ControlType.Window)
                return;

            AutomationElement anchor = GetAnchorElementFromCache(element);
            if (anchor == null)
                return;

            int[] id = element.GetRuntimeId();
            Entity entity;
            VersionInfo vInfo;
            if (automationElementTrie.TryGetValue(id, out entity))
            {
                vInfo = entity.versionInfo;
                if (vInfo != null && vInfo.Hash != anchor.Current.Name)
                {
                    vInfo.version = Sintering.Version.Updated;
                    vInfo.Hash = anchor.Current.Name;

                    // generate sinter message
                    Console.WriteLine("DeltaGenericAnchor");
                    Sinter sinter = new Sinter
                    {
                        HeaderNode = MsgUtil.BuildHeader(serviceCodes["delta"], serviceCodes["delta_subtree_replace"]),
                        EntityNode = UIAElement2EntityRecursive(anchor),
                    };

                    //send
                    connection.SendMessage(sinter);
                }
            }
        }

        // sends subtree rooted at 'element' if it exists in cache
        private void DeltaGenericImmediate(AutomationElement element)
        {
            if (IsCached(element))
            {
                //don't look for anchor

                // generate sinter message
                Sinter sinter = new Sinter
                {
                    HeaderNode = MsgUtil.BuildHeader(serviceCodes["delta"], serviceCodes["delta_subtree_update"]),
                    EntityNode = UIAElement2EntityRecursive(element),
                };

                //send
                connection.SendMessage(sinter);
            }
        }

        // special delta method that infers 'List' from BoundingBox notification
        private void DeltaSpecialList(AutomationElement element)
        {
            Header header = null;

            Console.WriteLine("DeltaSpecialList");
            if (IsCached(element))
            {
                // if the list is sent before
                string syntheticId = SinterUtil.GetRuntimeId(element, true);
                string trueId = SinterUtil.GetRuntimeId(element, true, false);
                /*if (automationElementDictionary.Get(syntheticId, out VersionInfo vInfo))
                {
                    if (vInfo != null && vInfo.runtimeID != trueId)
                    {
                        // list-item has been recreated
                        vInfo.runtimeID = trueId;
                        vInfo.version = Util.Version.Updated;
                        // build header
                        header = MsgUtil.BuildHeader(serviceCodes["delta"], serviceCodes["delta_subtree_replace"]);
                    }
                }*/
            }
            else // not in cache
            {
                // list item created at the Desktop-level
                Console.WriteLine("DeltaSpecialList: Not in Cache");
                AutomationElement anchor = GetAnchorElementFromCache(element);
                if (anchor != null)
                {
                    // build header
                    Console.WriteLine("DeltaSpecialList: No anchor");
                    header = MsgUtil.BuildHeader(serviceCodes["delta"], serviceCodes["delta_subtree_add"]);
                }
                else
                {
                    if (element.Current.ProcessId == requestedProcessId)
                    {
                        Console.WriteLine("DeltaSpecialList: Process Id");
                        anchor = AutomationElement.FocusedElement;
                        string anchorId = SinterUtil.GetRuntimeId(anchor);
                        // build header
                        header = MsgUtil.BuildHeader(serviceCodes["delta"], serviceCodes["delta_subtree_add"], anchorId);
                    }
                }
            }

            //send delta
            if (header != null)
            {
                Sinter sinter = new Sinter
                {
                    HeaderNode = header,
                    EntityNode = UIAElement2EntityRecursive(element),
                };

                connection.SendMessage(sinter);
            }
        }

        // special delta method to send combobox
        private void DeltaComboBox(AutomationElement element)
        {
            if (!IsCached(element))
            {
                AutomationElement parent = GetAnchorElementFromCache(element);
                if (parent != null)
                {
                    // generate sinter message
                    Sinter sinter = new Sinter
                    {
                        HeaderNode = MsgUtil.BuildHeader(serviceCodes["delta"], serviceCodes["delta_subtree_keep"]),
                        EntityNode = UIAElement2EntityRecursive(parent),
                    };

                    //send
                    connection.SendMessage(sinter);
                }
            }
        }

        // special delta method to send Tree update
        private void DeltaSpecialTree(AutomationElement element, ExpandCollapseState newValue)
        {
            int[] runtimeId = element.GetRuntimeId();

            if (automationElementTrie.TryGetValue(runtimeId, out Entity entity))
            {
                VersionInfo vInfo = entity.versionInfo;
                if (vInfo == null) return;
                if (newValue == ExpandCollapseState.Collapsed)
                {
                    vInfo.version |= Sintering.Version.Collapsed;
                    return;
                }

                if (newValue == ExpandCollapseState.Expanded)
                {
                    bool alreadySent = (((vInfo.version & Sintering.Version.Expanded) == Sintering.Version.Expanded) ||
                                        ((vInfo.version & Sintering.Version.Collapsed) == Sintering.Version.Collapsed));
                    vInfo.version |= Sintering.Version.Expanded;

                    if (alreadySent && IsTree(element))
                        return;
                }
            }

            Sinter sinter = new Sinter
            {
                HeaderNode = MsgUtil.BuildHeader(serviceCodes["delta"], serviceCodes["delta_subtree_expand"]),
                EntityNode = UIAElement2EntityRecursive(element),
            };

            //send delta
            connection.SendMessage(sinter);
        }

        private void DeltaForClose(string rid)
        {
            Sinter sinter = new Sinter
            {
                HeaderNode = MsgUtil.BuildHeader(serviceCodes["event"], serviceCodes["event_closed"], rid),
            };

            connection.SendMessage(sinter);
        }

        #region XML Processing

        PropertyCondition textPatternAvailable = new PropertyCondition(AutomationElement.IsTextPatternAvailableProperty, true);

        private bool HasCollection(AutomationElement element)
        {
            return (element.Current.ControlType == ControlType.Tree ||
                    element.Current.ControlType == ControlType.List);
        }

        private bool IsTree(AutomationElement element)
        {
            return (element.Current.ControlType == ControlType.Tree ||
                    element.Current.ControlType == ControlType.TreeItem);
        }

        public void SerializeDocumentElement(AutomationElement element, Entity parentXMLNode)
        {
            // For MS Word: Document, Edit, Page xx - all points to same contents so we will have duplicates,
            // so checking at document level to have single copy

            // Set up the conditions for finding the text control.
            //PropertyCondition documentControl = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Document);
            //PropertyCondition textPatternAvailable = new PropertyCondition(AutomationElement.IsTextPatternAvailableProperty, true);
            //AndCondition findControl = new AndCondition(documentControl, textPatternAvailable);
            //AutomationElement targetDocument = element.FindFirst(TreeScope.Element, findControl);

            AutomationElement targetDocument = element.FindFirst(TreeScope.Element, textPatternAvailable);
            if (targetDocument != null)
            {
                // Get required control patterns
                TextPattern targetTextPattern = targetDocument.GetCurrentPattern(TextPattern.Pattern) as TextPattern;
                if (targetTextPattern != null)
                {

                    if (parentXMLNode.words == null)
                    {
                        parentXMLNode.words = new List<Word>();
                    }

                    TextPatternRange textRange = targetTextPattern.DocumentRange;
                    textRange.ExpandToEnclosingUnit(TextUnit.Word);
                    do
                    {
                        Word wd = new Word();
                        wd.text = textRange.GetText(-1);

                        if (wd.text == "\r")
                        {
                            // it's a newline
                            wd.newline = "1";
                        }

                        Object fontName = textRange.GetAttributeValue(TextPattern.FontNameAttribute);
                        if (null != fontName && fontName != AutomationElement.NotSupported)
                        {
                            wd.font_name = fontName.ToString();
                        }

                        // 400 Normal, 700 Bold
                        Object fontWeight = textRange.GetAttributeValue(TextPattern.FontWeightAttribute);
                        if (null != fontWeight && fontWeight != AutomationElement.NotSupported)
                        {
                            wd.bold = fontWeight.ToString() == "700" ? "1" : "0";
                        }

                        Object fontSize = textRange.GetAttributeValue(TextPattern.FontSizeAttribute);
                        if (null != fontSize && fontSize != AutomationElement.NotSupported)
                        {
                            wd.font_size = fontSize.ToString();
                        }

                        bool italic = (bool)textRange.GetAttributeValue(TextPattern.IsItalicAttribute);
                        if (italic)
                        {
                            wd.italic = "1";
                        }

                        Object underline = textRange.GetAttributeValue(TextPattern.UnderlineStyleAttribute);
                        if (null != underline && underline != AutomationElement.NotSupported)
                        {
                            string uline = underline.ToString();
                            wd.underline = uline == "None" ? "0" : "1";
                        }

                        parentXMLNode.words.Add(wd);

                    } while (textRange.MoveEndpointByUnit(TextPatternRangeEndpoint.End, TextUnit.Word, 1) == 1 &&
                             textRange.MoveEndpointByUnit(TextPatternRangeEndpoint.Start, TextUnit.Word, 1) == 1);
                }
            }
        }

        public Entity AutomationElement2EntityLite(AutomationElement element)
        {
            AutomationElement.AutomationElementInformation current = element.Current;
            Entity entity = new Entity()
            {
                Name = current.Name,
                Process = "" + current.ProcessId,
            };
            return entity;
        }

        public Entity UIAElement2EntitySingle(AutomationElement element)
        {
            if (element == null)
                return null;

            AutomationElement.AutomationElementInformation current = element.Current;

            Console.WriteLine("Form Entity for {0}/{1}/{2}", element.Current.ControlType.ProgrammaticName, element.Current.ClassName, element.Current.Name);

            String uniqueId;

           /* if (element.Current.ControlType == ControlType.Button || element.Current.ControlType == ControlType.RadioButton)
            {
                uniqueId = SinterUtil.GetRuntimeId(element, true, true);
            } else
            {*/
                uniqueId = SinterUtil.GetRuntimeId(element, true);
           // }

            int[] runtimeId = element.GetRuntimeId();
            if (uniqueId == null)
                return null;

            //dimension
            Rect rect = current.BoundingRectangle;

            Entity entity = new Entity
            {
                UniqueID = uniqueId,
                Name = current.Name,
                Left = (int)rect.X,
                Top = (int)rect.Y,
                Height = (int)rect.Height,
                Width = (int)rect.Width,
            };

            //cache IDs
            if (automationElementTrie != null && current.ControlType != ControlType.ListItem)
            {
                if (current.ControlType == ControlType.List)
                {
                    entity.versionInfo = new VersionInfo(SinterUtil.GetRuntimeId(element, true, false));
                    automationElementTrie.Add(runtimeId, entity);
                }
                else
                {
                    entity.versionInfo = new VersionInfo();
                    automationElementTrie.Add(runtimeId, entity);
                }
            }

            // type
            //if (current.ControlType == ControlType.Pane)
            // entity.Type = current.ClassName;//.ToLower();
            //else //sizeof("ControlType.") = 11
            entity.Type = current.ControlType.ProgrammaticName.Substring(12);//.ToLower();
            
            if (current.ControlType == ControlType.Window && current.LocalizedControlType.Equals("Dialog"))
            {
                entity.Type = current.LocalizedControlType.ToString();
            }
            

            if (current.ControlType == ControlType.Pane && current.ClassName.Equals("SysDateTimePick32"))
            {
                entity.Type = "DateTimePicker";
            }

            // name, value
            //entity.Value = "" + element.GetCurrentPropertyValue(LegacyIAccessiblePattern.ValueProperty);

            #region child count -- not setting it for the sake of performance
            /*
             entity.ChildCount = 0;
            entity.ChildCount = element.FindAll(TreeScope.Children, Condition.TrueCondition).Count;
            if (entity.ChildCount > 0)
                entity.Children = new List<Entity>(xmlDoc.ChildCount);
            */
            #endregion

            // states
            //int states = (int)element.GetCurrentPropertyValue(LegacyIAccessiblePattern.StateProperty);
            uint states = 0;
            #region code for getting States manually
            if (current.IsOffscreen)
                states |= States.OFFSCREEN;
            if (!current.IsEnabled)
                states |= States.DISABLED;
            if (current.HasKeyboardFocus)
                states |= States.FOCUSED;
            if (current.IsKeyboardFocusable)
                states |= States.FOCUSABLE;

            object pattern;

            // expand/collapse
            if (current.ControlType == ControlType.TreeItem ||
              current.ControlType == ControlType.MenuItem ||
              current.ControlType == ControlType.ComboBox)
            {
                if (element.TryGetCurrentPattern(ExpandCollapsePattern.Pattern, out pattern))
                {
                    if (((ExpandCollapsePattern)pattern).Current.ExpandCollapseState == ExpandCollapseState.Expanded)
                        states |= States.EXPANDED;
                    if (((ExpandCollapsePattern)pattern).Current.ExpandCollapseState == ExpandCollapseState.Collapsed)
                        states |= States.COLLAPSED;
                }
            }

            // selected, selectable
            if (current.ControlType == ControlType.TreeItem ||
              current.ControlType == ControlType.ListItem)
            {
                if (element.TryGetCurrentPattern(SelectionItemPattern.Pattern, out pattern))
                {
                    if (((SelectionItemPattern)pattern).Current.IsSelected)
                        states |= States.SELECTED;
                    states |= States.SELECTABLE;
                }
            }

            
            /* //Debug
           AutomationPattern[] patterns = element.GetSupportedPatterns();
           Console.WriteLine("name {0} {1}", current.Name, current.ControlType.ProgrammaticName);
           foreach (AutomationPattern p in patterns) {
             Console.WriteLine("\t{0}", p.ProgrammaticName);
           }
           */
            // checked
            if (element.TryGetCurrentPattern(TogglePattern.Pattern, out pattern))
            {
                if (((TogglePattern)pattern).Current.ToggleState == ToggleState.On)
                    states |= States.CHECKED;
            }
            if (current.ControlType == ControlType.RadioButton &&
                (bool)element.GetCurrentPropertyValue(AutomationElement.IsSelectionItemPatternAvailableProperty))
            { // for others
                if (element.TryGetCurrentPattern(SelectionItemPattern.Pattern, out pattern))
                {
                    if (((SelectionItemPattern)pattern).Current.IsSelected)
                    {
                        states |= States.SELECTED;
                    }
                }
            }
            else if (current.ControlType == ControlType.Spinner)
            {
                object rangeValue = element.GetCurrentPropertyValue(RangeValuePattern.ValueProperty, true);
                if (rangeValue != AutomationElement.NotSupported)
                {
                    entity.Value = rangeValue.ToString();
                }
            }

            //xmlDoc.Value = "";
            pattern = element.GetCurrentPropertyValue(ValuePattern.ValueProperty, true);
            if (AutomationElement.NotSupported != pattern)
            {
                entity.Value = pattern as string;
            }
            else
            {
                if (current.ControlType == ControlType.Text)
                {
                    entity.Value = entity.Name;
                    if (current.AccessKey != "")
                    {
                        entity.Value = current.AccessKey;
                        states |= States.LINKED;
                    }
                }
            }

            //override default value, state
            if (current.ControlType == ControlType.MenuItem ||
                current.ControlType == ControlType.Menu)
            {
                entity.Value = current.AcceleratorKey;
            }
            /*
          //special treatment for menu, alter the 'value' field
          if ((bool) element.GetCurrentPropertyValue(AutomationElement.IsValuePatternAvailableProperty)) { // for others
            if (element.TryGetCurrentPattern(ValuePattern.Pattern, out pattern)) {
              xmlDoc.Value = ((ValuePattern)pattern).Current.Value;
              if (((ValuePattern)pattern).Current.IsReadOnly) {
                  states |= States.READONLY;
              }
            }
          } */

            entity.States = (uint)states;

            #endregion

            // check for rich text-edit element
            if (current.ControlType == ControlType.Document)
                SerializeDocumentElement(element, entity);

            return entity;
        }

        public Entity UIAElement2EntityRecursive(AutomationElement element)
        {
            if (element == null)
                return null;

            Entity entity = UIAElement2EntitySingle(element);

            //children DFS
            if (entity != null)
            {
                if (HasCollection(element))
                    AppendAllChildren(entity, element);
                else
                    AppendChildren(entity, element);
            }
            return entity;
        }

        public Entity SerializeListOrTreeItem(AutomationElement element)
        {
            Entity xmlDoc = new Entity();
            AutomationElement.AutomationElementInformation current = element.Current;
            //unique_id
            string uniqueId = SinterUtil.GetRuntimeId(element, true);
            if (uniqueId == null)
                return null;
            xmlDoc.UniqueID = uniqueId;

            xmlDoc.Type = current.ControlType.ProgrammaticName.Substring(12).ToLower();

            // name, value
            xmlDoc.Name = current.Name;
            //xmlDoc.Value = "" + element.GetCurrentPropertyValue(LegacyIAccessiblePattern.ValueProperty);

            //dimension
            Rect rect = (Rect)current.BoundingRectangle;
            xmlDoc.Left = (int)rect.X;
            xmlDoc.Top = (int)rect.Y;
            xmlDoc.Height = (int)rect.Height;
            xmlDoc.Width = (int)rect.Width;

            // states
            //int states = (int)element.GetCurrentPropertyValue(LegacyIAccessiblePattern.StateProperty);
            uint states = 0;
            #region code for getting States manually
            object pattern;
            if (current.ControlType == ControlType.TreeItem)
            {
                if (element.TryGetCurrentPattern(ExpandCollapsePattern.Pattern, out pattern))
                {
                    if (((ExpandCollapsePattern)pattern).Current.ExpandCollapseState == ExpandCollapseState.Expanded)
                        states |= States.EXPANDED;
                    if (((ExpandCollapsePattern)pattern).Current.ExpandCollapseState == ExpandCollapseState.Collapsed)
                        states |= States.COLLAPSED;
                }
            }

            // selected, selectable
            if (element.TryGetCurrentPattern(SelectionItemPattern.Pattern, out pattern))
            {
                if (((SelectionItemPattern)pattern).Current.IsSelected)
                    states |= States.SELECTED;
                states |= States.SELECTABLE;
            }
            xmlDoc.States = (uint)states;
            #endregion

            #region child count
            AutomationElementCollection children = element.FindAll(TreeScope.Children, Condition.TrueCondition);
            AutomationElement child = null;
            Entity childEntity = null;
            for (int i = 0; i < children.Count; i++)
            {
                child = children[i];
                if (child.Current.ControlType == ControlType.Image)
                {
                    continue;
                }
                childEntity = new Entity();
                { //populate basic information
                    childEntity.UniqueID = SinterUtil.GetRuntimeId(child, true);
                    childEntity.Name = child.Current.Name;
                    childEntity.Type = child.Current.ControlType.ProgrammaticName.Substring(12).ToLower();
                    pattern = child.GetCurrentPropertyValue(ValuePattern.ValueProperty, true);
                    if (AutomationElement.NotSupported != pattern)
                    {
                        childEntity.Value = pattern as string;
                    }
                }
                if (xmlDoc.Children == null)
                {
                    xmlDoc.Children = new List<Entity>();
                }
                xmlDoc.Children.Add(childEntity);
            }
            if (xmlDoc.Children != null)
            {
                xmlDoc.ChildCount = xmlDoc.Children.Count;
            }
            else
            {
                xmlDoc.ChildCount = 0;
            }
            #endregion

            //log.Info(String.Format("\t\t {0}  --> {1}", xmlDoc.Name.Length > 15 ? xmlDoc.Name.Substring(0, 15) : xmlDoc.Name.PadLeft(15), uiStopwatch.ElapsedMilliseconds));
            //Console.WriteLine("\t\t {0}  --> {1}", xmlDoc.Name.Length > 15 ? xmlDoc.Name.Substring(0, 15): xmlDoc.Name.PadLeft(15), uiStopwatch.ElapsedMilliseconds);
            return xmlDoc;
        }

        public void AppendChildren(Entity parentNode, AutomationElement parent)
        {
            try
            {
                AutomationElement childElement = treeWalker.GetFirstChild(parent);
                while (childElement != null)
                {
                    try
                    {
                        Entity childEntity = UIAElement2EntitySingle(childElement);
                        if (childEntity != null)
                        {
                            if (parentNode.Children == null)
                            {
                                parentNode.Children = new List<Entity>();
                            }
                            parentNode.Children.Add(childEntity);
                            if (childElement.Current.ControlType == ControlType.Image)
                            { // if image then replace it to group
                                childEntity.Type = ControlType.Group.ProgrammaticName.Substring(12).ToLower();
                            }
                            AppendChildren(childEntity, childElement);
                        }
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine("In AppendChildren: " + exception.Message);
                    }
                    childElement = treeWalker.GetNextSibling(childElement);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("In AppendChildren " + ex.Message);
            }
        }

        public void AppendAllChildren(Entity parentEntity, AutomationElement parentUI)
        {
            // Find all children that match the specified conditions.
            AutomationElementCollection elementCollection = parentUI.FindAll(TreeScope.Children, Condition.TrueCondition);
            //AutomationElementCollection elementCollection = parentUI.FindAll(TreeScope.Children, listItemCondition);

            Entity childEntity = null;
            for (int i = 0; i < elementCollection.Count; i++)
            {
                childEntity = SerializeListOrTreeItem(elementCollection[i]);
                if (childEntity != null)
                {
                    if (parentEntity.Children == null)
                    {
                        parentEntity.Children = new List<Entity>();
                    }
                    parentEntity.Children.Add(childEntity);
                }
            }
        }

        #endregion

        //Stopwatch xmlStopwatch = new Stopwatch();
        Queue<AutomationElement> fringe = new Queue<AutomationElement>();

        #region IWinCommands inteface (actuator) implementation

        public ConnectionHandler connection { get; set; }

        public void execute_stop_scraping()
        {
            bDesktopHookAdded = false;
        }

        public void execute_verify_passcode(Sinter sinter)
        {
            /* handles verify_passcode_req */
            string clientPasscode = sinter.HeaderNode.ParamsInfo.Data1;
            //Console.WriteLine("client passcode: {0}", clientPasscode);

            bool result = false;
            if(clientPasscode == this.passcode)
            {
              result = true;
              Console.WriteLine("client passcode match.");
              this.bPasscodeVerified = true;
            }

            Header header = MsgUtil.BuildHeader(serviceCodes["verify_passcode"], serviceCodes["verify_passcode_res"]);
            header.ParamsInfo = new Params
            {
              Data1 = result.ToString(),
            };

            Sinter sintermsg = new Sinter()
            {
              HeaderNode = header,
            };

            connection.SendMessage(sintermsg);


            if(result == false)
            {
              Console.WriteLine("client passcode not match!");
              connection.StopConnectionHandling();
            }

        }

        public void execute_ls_req(Sinter _)
        {
            // demo: only fetch explorer app for now
            string[] supportedProcesses = { "Calculator", "calc1", "calc", "Notepad", "explorer", "WINWORD", "Word", "wordpad"};

            Dictionary<string, string> processes = new Dictionary<string, string>();
            foreach (string pname in supportedProcesses)
            {
                Process[] p = Process.GetProcessesByName(pname);
                if (p.Length > 0)
                    processes[p[0].Id.ToString()] = pname;
            }

            List<Entity> entityNodes = new List<Entity>();

            AutomationElement element = treeWalker.GetFirstChild(AutomationElement.RootElement);
            while (element != null)
            {
                if (element.Current.LocalizedControlType.Contains("window"))
                {
                    Entity node = AutomationElement2EntityLite(element);
                    if (node == null)
                        continue;

                    if (processes.ContainsKey(node.Process) || processes.ContainsValue(node.Name))
                    {
                        //windows 10 Calculator (metro app) windows pid is different from app pid and not a key in Dictionary processes. 
                        if (!(processes.ContainsValue(node.Name))) 
                        {
                            node.Name = String.Format("{0} --{1}", processes[node.Process], node.Name);
                        }
                        entityNodes.Add(node);
                    }
                }
                element = treeWalker.GetNextSibling(element);
            }

            // constructing sinter message
            Sinter sinter = new Sinter
            {
                HeaderNode = MsgUtil.BuildHeader(serviceCodes["ls_res"]),
                EntityNodes = entityNodes,
            };

            connection.SendMessage(sinter);
        }

        public void execute_ls_l_req(Sinter sinter)
        {
            string id = sinter.HeaderNode.Process;

            AutomationElement element = SinterUtil.GetAutomationElementFromId(id, IdType.ProcessId);
            if (element == null)
                return;

            applicationRootElement = element;
            requestedProcessId = element.Current.ProcessId;
            requestedProcessRuntimeId = SinterUtil.GetRuntimeId(element);

            // update instance in connection
            connection.RequestedProcessId = requestedProcessId;

            // register listeners
            Automation.AddStructureChangedEventHandler(element, TreeScope.Subtree, new StructureChangedEventHandler(OnStructureChangedLocal));

            //window_opened, closed event
            Automation.AddAutomationEventHandler(WindowPatternIdentifiers.WindowOpenedEvent, element, TreeScope.Subtree, new AutomationEventHandler(OnWindowOpenedLocal));

            Automation.AddAutomationEventHandler(WindowPatternIdentifiers.WindowClosedEvent, element, TreeScope.Subtree, new AutomationEventHandler(OnWindowClosedLocal));

            //propertyChanged listener
            Automation.AddAutomationPropertyChangedEventHandler(element, TreeScope.Subtree,
              new AutomationPropertyChangedEventHandler(OnPropertyChange),
                new AutomationProperty[] {
            ExpandCollapsePattern.ExpandCollapseStateProperty,
            AutomationElement.BoundingRectangleProperty,
            AutomationElement.NameProperty,
                    ValuePattern.ValueProperty,
                    SelectionItemPattern.IsSelectedProperty,
                    RangeValuePattern.ValueProperty,
                    /*AutomationElement.IsOffscreenProperty*/
                    /*AutomationElement.ControlTypeProperty*/
            });

            // initialize cache dictionary
            // automationElementDictionary = new AutomationElementDictionary(ref treeWalker, ref applicationRootElement);
            automationElementTrie = new Trie<int[], Entity, int>();

            // bring the current app window to foreground
            int SHOW_DEFAULT = 10;
            Win32.ShowWindow((IntPtr)element.Current.NativeWindowHandle, SHOW_DEFAULT);
            Win32.SetForegroundWindow((IntPtr)element.Current.NativeWindowHandle);

            // register global desktop hook
            RegisterDesktopHooks();
            Console.WriteLine("isScreenReader {0}", ScreenReaderFlag);

            //Sinter message generation
            SinterUtil.ScreenSize(out int width, out int height);
            Header header = MsgUtil.BuildHeader(serviceCodes["ls_l_res"]);
            header.ParamsInfo = new Params
            {
                Data1 = width.ToString(),
                Data2 = height.ToString(),
            };

            Sinter sinterRes = new Sinter
            {
                HeaderNode = header,
                EntityNode = UIAElement2EntityRecursive(element),
            };

            //start recycle element thread
            // automationElementDictionary.StartManagerThread();

            // start some threads
            ThreadFocusThrottler = new Thread(new ThreadStart(HandlerFocusThrottler));
            ThreadFocusThrottler.Start();

            // start structureChange thread
            //StructureChangedManagerThread = new Thread(new ThreadStart(this.StructureChangeHandler));
            //StructureChangedManagerThread.Start();

            connection.SendMessage(sinterRes);
        }

        public void execute_delta(Sinter sinter)
        {
            // To Do
        }

    public void execute_kbd(Sinter sinter)
    {
	  /*link: https://msdn.microsoft.com/en-us/library/system.windows.forms.sendkeys.send(v=vs.110).aspx*/
	  //Modifier values indicate if control, shift, or alt has been pressed
	  string runtimeId = sinter.HeaderNode.ParamsInfo.TargetId;
	  char key;
	  SetFocus(runtimeId);
	  //Allow time for focus to be moved to active application, probably can reduce from doing this every key press
	  Thread.Sleep(10);
	  key = sinter.HeaderNode.ParamsInfo.KeyPress;
            if (key != 0)
            {
                string keyPress = key.ToString();
                Console.WriteLine(key);
                SendKeys.SendWait(keyPress);
            }
            else{
                Console.WriteLine(sinter.HeaderNode.ParamsInfo.Data1);
                SendKeys.SendWait(sinter.HeaderNode.ParamsInfo.Data1);
            }
    }

        public void execute_mouse(Sinter sinter)
        {
            Console.WriteLine("received mouse message (code:{0} subCode:{1})", sinter.HeaderNode.ServiceCode, sinter.HeaderNode.SubCode);

            int subCode = sinter.HeaderNode.SubCode;

            if (subCode == serviceCodes["mouse_click_left"] || subCode == serviceCodes["mouse_click_right"])
            {
                int button = int.Parse(sinter.HeaderNode.ParamsInfo.Data1);
                uint x = uint.Parse(sinter.HeaderNode.ParamsInfo.Data2);
                uint y = uint.Parse(sinter.HeaderNode.ParamsInfo.Data3);

                uint mask = ((button == 1) ?
                            Win32.MOUSEEVENTF_RIGHTDOWN | Win32.MOUSEEVENTF_RIGHTUP :
                            Win32.MOUSEEVENTF_LEFTDOWN | Win32.MOUSEEVENTF_LEFTUP);

                Win32.SetCursorPos((int)x, (int)y);
                Win32.mouse_event(mask, x, y, 0, 0);

            }
            else if (subCode == serviceCodes["mouse_scroll_up"])
            {

            }
            else if (subCode == serviceCodes["mouse_scroll_down"])
            {

            }
            else if (subCode == serviceCodes["mouse_move"])
            {
                uint x = uint.Parse(sinter.HeaderNode.ParamsInfo.Data2);
                uint y = uint.Parse(sinter.HeaderNode.ParamsInfo.Data3);
                Win32.SetCursorPos((int)x, (int)y);
            }
            else { }
        }


        public void execute_event(Sinter sinter)
        {
            throw new NotImplementedException();
        }

        public void execute_ls_res(Sinter sinter)
        {
            throw new NotImplementedException();
        }

        public void execute_ls_l_res(Sinter sinter)
        {
            throw new NotImplementedException();
        }

        public void execute_listener(Sinter sinter)
        {
            string runtimeId = sinter.HeaderNode.ParamsInfo.TargetId;
            RepeatedRequest focus = new RepeatedRequest(runtimeId, DateTime.Now);
            repeatedRequestStack.Add(focus);
            //Console.WriteLine("items in repeated stack {0}", repeatedRequestStack.Count);
        }

        public void execute_action(Sinter sinter)
        {
            string runtimeId = "";
            string _serviceCode = "";
            string _subCode = "";
            AutomationElement element = null;

            if (sinter.HeaderNode.ParamsInfo != null)
            {
                Console.WriteLine("execute_action {0}", sinter.HeaderNode.ParamsInfo.TargetId);
                runtimeId = sinter.HeaderNode.ParamsInfo.TargetId;
            }
            else
                runtimeId = sinter.HeaderNode.Process;  //for example: action_close

            if (!serviceCodesRev.TryGetValue(sinter.HeaderNode.ServiceCode, out _serviceCode))
                return;

            // sanity check
            if (!_serviceCode.Equals("action"))
                return;

            serviceCodesRev.TryGetValue(sinter.HeaderNode.SubCode, out _subCode);
            if ((_subCode != "action_expand_and_select") && (_subCode != "action_foreground"))
            {
                // extract the automation element pointed by runtimeId
                try
                {
                    //Console.WriteLine("execute_action: Get RuntimeId = {0}", runtimeId);
                    if (runtimeId != null)
                    {
                        element = SinterUtil.GetAutomationElementFromId(runtimeId, IdType.RuntimeId);
                        if (element == null)
                            return;
                    }

                    int[] id = element.GetRuntimeId();
                    if (automationElementTrie.TryGetValue(id, out Entity entity))
                    {
                        entity.versionInfo.version = Sintering.Version.Updated;
                        // Console.WriteLine("Version from AutoElement Dict {0}", vInfo.runtimeID);
                    }
                }
                catch
                {
                    Console.WriteLine("problem with extracting {0}", runtimeId);
                }
            }

            if (serviceCodesRev.TryGetValue(sinter.HeaderNode.SubCode, out _subCode))
            {
                Console.WriteLine("execute_action: Got subCode = {0}", _subCode);
                switch (_subCode)
                {
                    case "action_default":
                        UIAction.PerformDefaultAction(element);
                        break;
                    case "action_toggle":
                        UIAction.PerformToggleAction(element);
                        break;
                    case "action_select":
                        UIAction.PerformSelectionAction(element);
                        break;
                    case "action_rename":
                        break;
                    case "action_expand":
                        UIAction.PerformExpandAction(element);
                        break;
                    case "action_collapse":
                        UIAction.PerformCollapseAction(element);
                        break;
                    case "action_close":
                        UIAction.PerformCloseAction(element);
                        break;
                    case "action_change_focus":
                        break;
                    case "action_change_focus_precise":
                        break;
                    case "action_set_text":
                        executeSetText(runtimeId, sinter.HeaderNode.ParamsInfo.Data1);
                        break;
                    case "action_append_text":
                        executeAppendText(runtimeId, sinter.HeaderNode.ParamsInfo.Data1);
                        break;
                    case "action_foreground":
                        // bring the current app window to foreground
                        executeSetProcessToForeground(sinter.HeaderNode.Process);
                        break;
                    case "action_expand_and_select":
                        Console.WriteLine("Case action_expand_and_select");
                        if(sinter.HeaderNode.ParamsInfo.TargetIdList != null)
                        {
                            //Console.WriteLine("{0}", sinter.HeaderNode.ParamsInfo.TargetId.GetType());
                            UIAction.PerformExpandAndSelectAction(sinter.HeaderNode.ParamsInfo.TargetId.ToString(), uint.Parse(sinter.HeaderNode.ParamsInfo.Data1), sinter.HeaderNode.ParamsInfo.TargetIdList);
                        }
                        break;
                    //case "structureChangeNotification":
                    //    RegisterStructureChangedNotification(element);
                    //    break;
                    default:
                        break;
                }
            }
        }


        #endregion

        #region Important Doc. links
        // https://msdn.microsoft.com/en-us/library/system.windows.automation.textpattern(v=vs.110).aspx
        // https://msdn.microsoft.com/en-us/library/system.windows.automation.textpattern.fontnameattribute(v=vs.110).aspx
        // https://blogs.msdn.microsoft.com/oldnewthing/20150216-00/?p=44673

        #endregion

    }
}
