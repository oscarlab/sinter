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
using Sintering;
using System.Windows.Forms;
using System.Drawing;
using System.Reflection;
using LinqAndTrie;
using System.Collections.Concurrent;
using Gma.System.MouseKeyHook;

namespace WindowsProxy
{

    class TagInfo
    {
        String _id;
        Point _xy;

        public string Id
        {
            get { return _id; }
            set { _id = value; }
        }

        public Point XY
        {
            get { return _xy; }
            set { _xy = value; }
        }
        public TagInfo(string id, Point xy)
        {
            _id = id;
            _xy = xy;
        }
    };

    // keypress delegate
    public delegate void DelegateKeyPresses(string keys);

    public class WindowsProxy : IWinCommands
    {
        RootForm root;
        AppForm form;
        string RemoteCloseButtonUID; //UniqueID of remote close button
        string RemoteMinimizeButtonUID; //UniqueID of remote close button
        string RemoteZoomButtonUID; //UniqueID of remote close button
        FormWindowState prevWindowState; 

        Point rootPoint;
        Entity root_entity;

        static Dictionary<string, object> winControls;
        Type type;

        float height_ratio;
        float width_ratio;

        int window_count = 0;
        public int Window_Count
        {
            get
            {
                return window_count;
            }
            set
            {
                window_count = value;
            }
        }

        Dictionary<string, int> serviceCodes;
        Dictionary<int, string> serviceCodesRev;
        Dictionary<int, string> sendKeysCodes;

        public bool bPasscodeVerified { get; private set; }
        public Sinter baseXML { get; set; }

        Timer timer;
        ConcurrentDictionary<string, Control> hash = new ConcurrentDictionary<string, Control>();
        ConcurrentDictionary<string, Control> menuHash = new ConcurrentDictionary<string, Control>();
        ConcurrentDictionary<string, Entity> menuHashEntity = new ConcurrentDictionary<string, Entity>();
        ConcurrentDictionary<string, ToolStripMenuItem> menuHashMenuItem = new ConcurrentDictionary<string, ToolStripMenuItem>();
        ConcurrentDictionary<string, Entity> menuItemParent = new ConcurrentDictionary<string, Entity>();
        ConcurrentDictionary<string, Entity> comboBoxParent = new ConcurrentDictionary<string, Entity>();
        ConcurrentDictionary<string, Entity> comboBoxEntity = new ConcurrentDictionary<string, Entity>();

        Boolean mainWindowOpened = false; // May need to change to accomodate more windows, use the names to distinguish the windows

        public WindowsProxy(RootForm r)
        { // Form
            root = r;

            // loading the config dictionaries
            winControls = Config.getConfig("control_types_windows");

            Dictionary<string, object> serviceCodesTemp = Config.getConfig("service_code");

            if (serviceCodesTemp != null)
            {
                serviceCodes = serviceCodesTemp.ToDictionary(pair => pair.Key, pair => (int)pair.Value);

                serviceCodesRev = serviceCodes.ToDictionary(pair => pair.Value, pair => pair.Key);
            }

            hash = new ConcurrentDictionary<string, Control>();

            type = GetType();

            // timer to avoid sending mouse click multiple times
            timer = new Timer
            {
                Interval = 50,
                Enabled = false,
            };

            timer.Tick += Timer_Tick;

            this.bPasscodeVerified = false;
            this.prevWindowState = FormWindowState.Normal;
        }

        private int requestedProcessId = 0;
        public int RequestedProcessId
        {
            get
            {
                return requestedProcessId;
            }
            set
            {
                requestedProcessId = value;
                connection.RequestedProcessId = value;
            }
        }

        public string RemoteProcessName = "";

        #region Main Renderer
        string invoking_method_name;
        MethodInfo invoking_method;

        Control Render(Entity entity, Control parent_control = null)
        {
            Console.WriteLine("Rendering: {0}/{1}", entity.Type, entity.Name);

            if ((entity.States & States.DISABLED) != 0)
            {
                Console.WriteLine("{0}/{1} states is not enabled", entity.Type, entity.Name);
            }

            if ((entity.States & States.FOCUSABLE) != 0)
            {
                //Console.WriteLine("{0}/{1} states is FOCUSABLE", entity.Type, entity.Name);
            }

            if ((entity.States & States.FOCUSED) != 0)
            {
                Console.WriteLine("{0}/{1} states is FOCUSED", entity.Type, entity.Name);
            }

            if ((entity.States & States.INVISIBLE) != 0)
            {
                Console.WriteLine("{0}/{1} states is INVISIBLE", entity.Type, entity.Name);
            }

            entity.Type = entity.Type.First().ToString().ToUpper() + entity.Type.Substring(1); //turn "window" to "Window"
            invoking_method_name = string.Format("Render{0}", entity.Type);
            /* tweak for Mac Scraper*/
            if (invoking_method_name == "RenderLabel" && entity.Name != "" && entity.Value != "")
            {
                invoking_method_name = "RenderText";
                entity.Name = entity.Value; //so the calculator main display will show '0' instead of 'main display'
            }
            else if (invoking_method_name == "RenderMenubar")
            {
                invoking_method_name = "RenderMenuBar";
            }
            else if (invoking_method_name == "RenderMenuitem" || invoking_method_name == "RenderMenubaritem")
            {
                invoking_method_name = "RenderMenuItem";
            }
            /* tweak for Mac Scraper: End */

            if (invoking_method_name.Equals("RenderWindow") && parent_control != null)
            {
                /* trick for window7 calc statistic mode (window/pane/window)
                   treat sub window as pane so it will not be rendered.
                   otherwise exception in below session when window added under pane:
                   parent_control.Controls.Add(current_control); */
                invoking_method_name = "RenderPane";
            }

            invoking_method = type.GetMethod(invoking_method_name,
              BindingFlags.NonPublic | BindingFlags.Instance);
            Console.WriteLine("Method {0}", invoking_method);
            Control current_control = null;
            if (invoking_method != null)
            {
                current_control = (Control)invoking_method.Invoke(this,
                  new object[] { entity, parent_control });

                if (current_control == null)
                {
                    Console.WriteLine("Current control is null?");
                    return null;
                }

                if (parent_control != null && parent_control != current_control)
                {
                    if (invoking_method_name == "RenderMenuBar")
                    {
                        Form form = (Form)parent_control;
                        form.MainMenuStrip = (MenuStrip)current_control;
                    }
                    parent_control.Controls.Add(current_control);

                    // add to hash
                    hash.TryAdd(entity.UniqueID, current_control);
                }
            }

            // check children
            Console.WriteLine("{0}/{1} has {2} children", entity.Type, entity.Name, entity.Children.Count);
            if (!HasChildren(entity))
                return current_control;

            foreach (Entity child_entity in entity.Children)
                Render(child_entity, current_control);

            return current_control;
        }
        #endregion

        #region Invividual Render Methods
        private Control RenderButton(Entity entity, Control parent)
        {

            Console.WriteLine("RenderButton entity.UniqueId: {0} {1}", entity.UniqueID, entity.Name);
            if (hash.TryGetValue(entity.UniqueID, out Control res))
            {
                Console.WriteLine("Remove");
                parent.Controls.Remove(res);
                hash.TryRemove(entity.UniqueID, out Control rem);
            }

            if (entity.Name.Equals("Maximize") || entity.Name.Equals("Minimize") || entity.Name.Equals("Close"))
            {
                if (entity.Name.Equals("Close"))
                {
                    RemoteCloseButtonUID = entity.UniqueID;
                }
                else if (entity.Name.Equals("Minimize"))
                {
                    RemoteMinimizeButtonUID = entity.UniqueID;
                    if ((entity.States & States.DISABLED) != 0)
                    {
                        this.form.MinimizeBox = false;
                    }
                }
                else if (entity.Name.Equals("Maximize"))
                {
                    RemoteZoomButtonUID = entity.UniqueID;
                    if ((entity.States & States.DISABLED) != 0)
                    {
                        this.form.MaximizeBox = false;
                    }
                }
                return parent;
            }

            if (entity.Name.Equals("AXCloseButton") || entity.Name.Equals("AXMinimizeButton") || entity.Name.Equals("AXZoomButton"))
            {
                if (entity.Name.Equals("AXCloseButton"))
                {
                    RemoteCloseButtonUID = entity.UniqueID;
                }
                else if (entity.Name.Equals("AXMinimizeButton"))
                {
                    RemoteMinimizeButtonUID = entity.UniqueID;
                }
                else if (entity.Name.Equals("AXZoomButton"))
                {
                    RemoteZoomButtonUID = entity.UniqueID;
                }
                return parent;
            }

            Control control = new Button();
            AdjustProperties(entity, ref control);
            Console.WriteLine("Executing RenderButton");

            control.Tag = entity;
            control.Click += Control_Click_Button;

            return control;
        }

        private Control RenderRadioButton(Entity entity, Control parent)
        {

            if (hash.TryGetValue(entity.UniqueID, out Control res))
            {
                Console.WriteLine("Remove");
                parent.Controls.Remove(res);
                hash.TryRemove(entity.UniqueID, out Control rem);
            }

            Control control = new RadioButton();
            AdjustProperties(entity, ref control);
            Console.WriteLine("Executing RenderRadioButton");

            control.Tag = entity;
            control.Click += Control_ClickRadioButton;

            return control;
        }

        private Control RenderCalendar(Entity entity, Control parent)
        {
            return parent;
        }

        private Control RenderCheckBox(Entity entity, Control parent)
        {
            Control control = new CheckBox();
            AdjustProperties(entity, ref control);
            Console.WriteLine("Executing RenderCheckBox");

            control.Tag = entity;
            ((CheckBox)control).Click += Control_Click_Button;

            return control;
        }

        private Control RenderComboBox(Entity entity, Control parent)
        {
            Console.WriteLine("Rendering ComboBox");
            Control control = new ComboBox();
            AdjustProperties(entity, ref control);
            control.Text = entity.Value;

            control.Tag = entity;

            foreach (Entity child_entity in entity.Children)
            {
                Console.WriteLine("{0} {1}", child_entity.Name, child_entity.Type);
                if (child_entity.Type == "List")
                {
                    foreach (Entity listItem in child_entity.Children)
                    {
                        ToolStripMenuItem tmp = new ToolStripMenuItem();
                        tmp.Name = listItem.Name;
                        tmp.Text = listItem.Name;
                        if ((listItem.States & States.SELECTED) != 0){
                            control.Text = listItem.Name;
                        }
                        tmp.Click += Control_Click_ComboBox;
                        ((ComboBox)control).Items.Add(tmp);
                        comboBoxParent.TryAdd(listItem.Name, child_entity);
                        comboBoxEntity.TryAdd(listItem.Name, listItem);
                    }
                }
            }
            Console.WriteLine("Rendered ComboBox");
            return control;
        }

        private Control RenderDataGrid(Entity entity, Control parent)
        {
            return parent;
        }

        private Control RenderDataItem(Entity entity, Control parent)
        {
            return parent;
        }

        private Control RenderDocument(Entity entity, Control parent)
        {

            TextBox text = new TextBox();
            text.Multiline = true;
            text.AcceptsReturn = true;
            text.AcceptsTab = true;
            text.WordWrap = true;

            Control control = text;
            control.Height = (int)(entity.Height * height_ratio);
            control.Width = (int)(entity.Width * width_ratio);

            control.Name = entity.Name;
            List<Word> words = entity.words;
            foreach (Word w in words) {
                if (w.newline == "1")
                {
                    control.Text += "\r";
                } else {
                    control.Text += w.text;
                }
            }

            control.Top = (int)((entity.Top - rootPoint.Y) * height_ratio);
            control.Left = (int)((entity.Left - rootPoint.X) * width_ratio);

            control.Tag = entity;
            control.KeyPress += Document_KeyPress;
            control.Click += Text_Click;

            // set default cursor to beginning of the document just like opening a file.
            text.SelectionStart = 0;
            text.SelectionLength = 0;

            return control;
        }

        private Control RenderEdit(Entity entity, Control parent)
        {
            Control control = null;
            Console.WriteLine("Executing RenderEdit");

            control = new TextBox();
            AdjustProperties(entity, ref control);

            control.Text = entity.Value;
            control.Tag = entity;
            control.KeyPress += Control_KeyPress;
            return control;
        }

        private Control RenderGroup(Entity entity, Control parent)
        {
            return parent;
        }

        private Control RenderHeader(Entity entity, Control parent)
        {
            return parent;
        }

        private Control RenderHeaderItem(Entity entity, Control parent)
        {
            return parent;
        }

        private Control RenderHyperlink(Entity entity, Control parent)
        {
            return parent;
        }

        private Control RenderImage(Entity entity, Control parent)
        {
            return parent;
        }

        private Control RenderList(Entity entity, Control parent)
        {
            ListView listView = new ListView();
            listView.View = View.List;

            listView.BeginUpdate();

            int i = 0; 
            foreach (Entity child in entity.Children)
            {
                ListViewItem li = new ListViewItem(child.Name);
                listView.Items.Add(li);
                if ((child.States & States.SELECTED) != 0)
                {
                    listView.Items[i].Selected = true;
                }
                i++;
            }
            Control control = (Control)listView;
            AdjustProperties(entity, ref control);
            Console.WriteLine("Executing RenderList");
            control.Tag = entity;
            control.Click += Control_Click_Button;
            listView.EndUpdate();
            return control;
        }

        private Control RenderListItem(Entity entity, Control parent)
        {
            Console.WriteLine("Rendered ListItem");
            return parent;
        }

        private Control RenderMenu(Entity entity, Control parent)
        {
            return parent;
        }

        private Control RenderMenuBar(Entity entity, Control parent)
        {
            // From http://www.c-sharpcorner.com/blogs/create-menustrip-dynamically-in-c-sharp1
            Console.WriteLine("Executing RenderMenuBar");
            Console.WriteLine("entity.Type: {0}", entity.Type);

            if (entity.Name == "System Menu Bar")
                return parent;

            MenuStrip ms = new MenuStrip();
            Control control = (Control)ms;
            AdjustProperties(entity, ref control);
            control.Tag = entity;
            // control.Click += Control_Click;

            // https://docs.microsoft.com/en-us/dotnet/api/system.windows.forms.menustrip.-ctor?view=netframework-4.8
            foreach (Entity child_entity in entity.Children)
            {
                Console.WriteLine("menuHashEntity.TryAdd(Name: {0}, entity: {0}", child_entity.Name, child_entity);
                menuHashEntity.TryAdd(child_entity.Name, child_entity);
                menuHash.TryAdd(child_entity.Name, control);

                ToolStripMenuItem mi = new ToolStripMenuItem(child_entity.Name);
                mi.Name = child_entity.Name;
                //mi.Click += Control_MenuItem_Expanded;
                mi.DropDownOpening += Control_MenuItem_Expanded;
                mi.DropDownClosed += Control_MenuItem_Collapsed;
                ((MenuStrip)control).Items.Add(mi);

                /* OSX server sends whole menu in DOM at the first beginning */
                if (child_entity.Children.Count > 0)
                {
                    menuHashMenuItem.TryAdd(mi.Name, mi);
                    foreach (Entity entity_mi in child_entity.Children[0].Children)
                    {
                        /* type of Children[0]: Menu */
                        /* type of children of Children[0]: MenuItem */
                        addSubMenuItem(entity_mi, ((MenuStrip)control), mi);
                    }
                }
            }

            return control;
        }

        private Control RenderMenuItem(Entity entity, Control parent)
        {
            /*Control control = new Button();
            AdjustProperties(entity, ref control);
            Console.WriteLine("Executing RenderButton");

            control.Tag = entity;
            control.Click += Control_Click;

            return control;*/
            return parent;
        }

        private Control RenderPane(Entity entity, Control parent)
        {
            /* Control control = new Panel();
             control.SendToBack();

              AdjustProperties(entity, ref control);

              control.Tag = entity;

              return control;*/
            return parent;
        }

        private Control RenderProgressBar(Entity entity, Control parent)
        {
            return parent;
        }

        private Control RenderScrollBar(Entity entity, Control parent)
        {
            return parent;
        }

        private Control RenderSeparator(Entity entity, Control parent)
        {
            return parent;
        }

        private Control RenderSlider(Entity entity, Control parent)
        {
            return parent;
        }

        private Control RenderSpinner(Entity entity, Control parent)
        {
            return parent;
        }

        private Control RenderSplitButton(Entity entity, Control parent)
        {
            return parent;
        }

        private Control RenderStatusBar(Entity entity, Control parent)
        {
            return parent;
        }

        private Control RenderTab(Entity entity, Control parent)
        {
            return parent;
        }

        private Control RenderTabItem(Entity entity, Control parent)
        {
            return parent;
        }

        private Control RenderTable(Entity entity, Control parent)
        {
            return parent;
        }

        private Control RenderText(Entity entity, Control parent)
        {
            Control control = new TextBox();
            Console.WriteLine("Executing RenderText");

            if (((entity.States & States.SELECTABLE) == 0) && (this.RemoteProcessName.Contains("calc --Calculator") == true))
            {
                //windows 7 calculator will fall to here
                ((TextBox)control).ReadOnly = true;
                ((TextBox)control).BorderStyle = 0;
            }
            
            AdjustProperties(entity, ref control);

            if (this.RemoteProcessName.Contains("calc --Calculator") && control.Text.Equals("Memory"))
            {
                control.Text = "";
            }



            control.Tag = entity;
            control.KeyPress += Control_KeyPress;
            return control;
        }


        private Control RenderThumb(Entity entity, Control parent)
        {
            return parent;
        }

        private Control RenderTitleBar(Entity entity, Control parent)
        {
            return parent;
        }

        private Control RenderToolBar(Entity entity, Control parent)
        {
            return parent;
        }

        private Control RenderToolTip(Entity entity, Control parent)
        {
            return parent;
        }

        private Control RenderTree(Entity entity, Control parent)
        {
            Control control = parent;
            TreeNode tree = new TreeNode(); // addRow(child)
            return control;
        }

        private Control RenderTreeItem(Entity entity, Control parent)
        {
            return parent;
        }

        private Control RenderUnknown(Entity entity, Control parent)
        {
            return parent;
        }


        private Control RenderWindow(Entity entity, Control parent)
        {

            // assign class variables
            root_entity = entity;
            rootPoint.X = root_entity.Left;
            rootPoint.Y = root_entity.Top;

            // make a form
            form = new AppForm();
            Control control = form as Control;

            // manual adjustment
            AdjustProperties(root_entity, ref control);
            form.Font = new Font("Segoe UI", 8);

            // potential fixes:
            //Rectangle screenRectangle = RectangleToScreen(this.ClientRectangle);
            //int titleHeight = screenRectangle.Top - this.Top;
            form.Width += (8 + 8);
            form.Height += (30 + 8);
            form.Height += 25;

            if ((entity.UniqueID.Contains("NS")) && (form.Width < 500))
            {
                //"NS" means comes from OSX "window__NS:476"
                // OSX usually have more than 8 menu items so we set larger windows to accommodate them  
                Console.WriteLine("original width = {0}", form.Width);
                form.Width = 500;
                Console.WriteLine("new width = {0}", form.Width);
            }

            //register event listener and delegate
            form.FormClosing += Form_Closing;
            form.SizeChanged += Form_SizeChanged;
            form.delegateKeyPresses = ProcessKeyPress;

            return control;
        }

        private Control RenderDefault(Entity entity, Control parent)
        {
            if (!winControls.TryGetValue(entity.Type, out object control_name) ||
                 control_name.Equals("unknown"))
            {
                return parent;
            }

            if (GetInstance(control_name as string) is Control control)
            {
                AdjustProperties(entity, ref control);
                return control;
            }

            return parent;
        }
        #endregion

        #region Renderer Utility
        public object GetInstance(string strFullyQualifiedName)
        {
            Type type = Type.GetType(strFullyQualifiedName);
            if (type != null)
            {
                return Activator.CreateInstance(type);
            }

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = asm.GetType(strFullyQualifiedName);
                if (type != null)
                    return Activator.CreateInstance(type);
            }
            return null;
        }

        bool isSent = false;
        private void Control_Click_Button(object sender, EventArgs e)
        {
            if (isSent)
                return;

            Control button = (Control)sender;
            Entity entity = (Entity)button.Tag;
            if (entity == null)
                return;

            Sinter sinter = new Sinter
            {
                HeaderNode = MsgUtil.BuildHeader(
                  serviceCodes["action"],
                  serviceCodes["action_default"],
                  entity.UniqueID
                ),
            };

            Console.WriteLine(sinter.HeaderNode.ParamsInfo.ToString());
            execute_mouse(sinter);

            isSent = true;
            timer.Enabled = true;
        }

        private void Control_ClickRadioButton(object sender, EventArgs e)
        {
            if (isSent)
                return;

            RadioButton button = (RadioButton)sender;
            Entity entity = (Entity)button.Tag;
            if (entity == null)
                return;

            Point center = GetCenter(entity);

            Sinter sinter = new Sinter
            {
                HeaderNode = MsgUtil.BuildHeader(
                  serviceCodes["mouse"],
                  serviceCodes["mouse_click_left"],
                  "",
                  null,
                  "0",
                  center.X.ToString(),
                  center.Y.ToString()
                ),
            };

            Console.WriteLine(sinter.HeaderNode.ParamsInfo.ToString());
            execute_mouse(sinter);

            isSent = true;
            timer.Enabled = true;
        }

        private void Control_MenuItem_Expanded(object sender, EventArgs e)
        {

            ToolStripDropDownItem item = sender as ToolStripDropDownItem;
            Console.WriteLine("MenuItem {0} Expanded", item.Text);

            menuHashEntity.TryGetValue(item.Text, out Entity entity);

            if (entity == null)
            {
                Console.WriteLine("[Warning!] cannot find entity");
                return;
            }
            Console.WriteLine("MenuItem ID {0}", entity.UniqueID);

            Sinter sinter = sinter = new Sinter
            {
                HeaderNode = MsgUtil.BuildHeader(
                      serviceCodes["action"],
                      serviceCodes["action_expand"],
                      entity.UniqueID),
            };

            Console.WriteLine(sinter.HeaderNode.ParamsInfo.ToString());
            execute_action(sinter);

            isSent = true;
            timer.Enabled = true;
        }

        private void Control_MenuItem_Collapsed(object sender, EventArgs e)
        {
            ToolStripDropDownItem item = sender as ToolStripDropDownItem;
            Console.WriteLine("MenuItem {0} Collapsed", item.Text);

            menuHashEntity.TryGetValue(item.Text, out Entity entity);

            if (entity == null)
            {
                Console.WriteLine("[Warning!] cannot find entity");
                return;
            }
            Console.WriteLine("MenuItem ID {0}", entity.UniqueID);

            Sinter sinter = sinter = new Sinter
            {
                HeaderNode = MsgUtil.BuildHeader(
                      serviceCodes["action"],
                      serviceCodes["action_collapse"],
                      entity.UniqueID),
            };

            Console.WriteLine(sinter.HeaderNode.ParamsInfo.ToString());
            execute_action(sinter);

            isSent = true;
            timer.Enabled = true;
        }

        private void Control_Click_ComboBox(object sender, EventArgs e)
        {
            Console.WriteLine("Begin Control_Click_Combo_Box");
            if (isSent)
                return;

            ComboBox item = (ComboBox)sender;
            // Entity entity;
            Console.WriteLine("MenuItem Click {0}", item.Text);
            comboBoxEntity.TryGetValue(item.Text, out Entity entity);
            if (entity == null)
                return;
            Console.WriteLine("MenuItem ID {0}", entity.UniqueID);
            List<string[]> lists = new List<string[]>();
            string name = item.Name;

            string id = null;
            comboBoxParent.TryGetValue(name, out Entity test);
            Console.WriteLine("{0}", test.UniqueID);

            Point start = GetCenter(entity);
            Console.WriteLine("Adding {0} to list...", entity.Name);
            lists.Add(new string[] { start.X.ToString(), start.Y.ToString() });

            while (comboBoxParent.TryGetValue(name, out Entity parent))
            {
                if (!comboBoxParent.TryGetValue(parent.Name, out Entity stop))
                {
                    id = parent.UniqueID;
                    Console.WriteLine("FInal Name: {0}", parent.Name);
                    break;
                }
                Point tmp = GetCenter(parent);
                Console.WriteLine("Adding {0} to list...", parent.Name);
                lists.Add(new string[] { tmp.X.ToString(), tmp.Y.ToString() });
                name = parent.Name;
            }

            lists.Reverse();

            Console.WriteLine("Menu Item {0}", id);

            Point center = GetCenter(entity);

            Sinter sinter = new Sinter
            {
                HeaderNode = MsgUtil.BuildHeader(
                  serviceCodes["action"],
                  serviceCodes["action_expand_and_select"],
                  id,
                  lists,
                  "0",
                  "",
                  ""
                ),
            };

            Console.WriteLine(sinter.HeaderNode.ParamsInfo.ToString());
            execute_action(sinter);

            isSent = true;
            timer.Enabled = true;
        }

        private void Control_Click_MenuItem(object sender, EventArgs e)
        {
            /*if (isSent)
                return; */

            ToolStripMenuItem item = (ToolStripMenuItem)sender;
            // Entity entity;
            Console.WriteLine("MenuItem Click {0}", item.Text);
            menuHashEntity.TryGetValue(item.Text, out Entity entity);
            if (entity == null)
                return;
            Console.WriteLine("MenuItem ID {0}", entity.UniqueID);

            Sinter sinter_mac = new Sinter
            {
                HeaderNode = MsgUtil.BuildHeader(
                              serviceCodes["action"],
                              serviceCodes["action_default"],
                              entity.UniqueID
                            ),
            };
            execute_action(sinter_mac);

            List<string[]> lists = new List<string[]>();
            string name = item.Name;

            string id = null;
            menuItemParent.TryGetValue(name, out Entity test);
            Console.WriteLine("menuItemParent ID: {0}", test.UniqueID);

            Point start = GetCenter(entity);
            Console.WriteLine("Adding {0} to list...", entity.Name);
            lists.Add(new string[] { start.X.ToString(), start.Y.ToString() });

            while (menuItemParent.TryGetValue(name, out Entity parent))
            {
                if (!menuItemParent.TryGetValue(parent.Name, out Entity stop))
                {
                    id = parent.UniqueID;
                    Console.WriteLine("FInal Name: {0}", parent.Name);
                    break;
                }
                Point tmp = GetCenter(parent);
                Console.WriteLine("Adding {0} to list...", parent.Name);
                lists.Add(new string[] { tmp.X.ToString(), tmp.Y.ToString() });
                name = parent.Name;
            }

            lists.Reverse();

            Console.WriteLine("Menu Item {0}", id);

            Point center = GetCenter(entity);

            Sinter sinter = new Sinter
            {
                HeaderNode = MsgUtil.BuildHeader(
                  serviceCodes["action"],
                  serviceCodes["action_expand_and_select"],
                  id,
                  lists,
                  "0",
                  "",
                  ""
                ),
            };

            Console.WriteLine(sinter.HeaderNode.ParamsInfo.ToString());
            execute_action(sinter);

            isSent = true;
            timer.Enabled = true;
        }

        private void Text_Click(object sender, EventArgs e)
        {
            isSent = false;
            TextBox text = (TextBox)sender;
            Entity entity = (Entity)text.Tag;
            Control textControl = text;
            MouseEventArgs args = e as MouseEventArgs;
            if (entity == null)
                return;
            Point point = new Point(args.X, args.Y);
            //Point click = textControl.PointToClient(point);
            Point center = GetCenter(entity);

            Sinter sinter = new Sinter
            {
                HeaderNode = MsgUtil.BuildHeader(
                  serviceCodes["mouse"],
                  serviceCodes["mouse_click_left"],
                  "",
                  "0",
                  (entity.Left + point.X).ToString(),
                  (entity.Top - point.Y).ToString()
                ),
            };

            Console.WriteLine(sinter.HeaderNode.ParamsInfo.ToString());
            execute_mouse(sinter);

            isSent = true;
            timer.Enabled = true;
        }

        private void Document_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            int start = textBox.SelectionStart;
            int length = textBox.SelectionLength;
            String caretPos = start.ToString() + "," + length.ToString();
            Entity entity = (Entity)textBox.Tag;
            if (entity != null)
            {
                ProcessKeyPress(e, entity.UniqueID, caretPos);
            }
        }

        private void Control_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            Entity entity = (Entity)textBox.Tag;

            if (entity != null)
            {
                ProcessKeyPress(e.KeyChar.ToString(), entity.UniqueID);
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            timer.Enabled = false;
            isSent = false;
            return;
        }
        #endregion

        #region AppForm handler/helper
        public void close_forms()
        {
            this.RemoteCloseButtonUID = null;
            if (this.form != null)
            {
                this.form.Invoke(new Action(() => this.form.Close()));
            }
            this.form = null;
        }

        private void Form_Closing(object sender, FormClosingEventArgs e)
        {
            Console.WriteLine("Form_Closing(), this.RemoteCloseButtonUID = {0}", this.RemoteCloseButtonUID);
            if (this.RemoteCloseButtonUID == null)
            {
                //just the local window
                this.form = null;
                root.Remove_Dict_Item(requestedProcessId);
                return;
            }
            else {
                const string message =
                    "Do you want to close the remote window as well?";
                string caption = this.RemoteProcessName;
                var result = MessageBox.Show(message, caption,
                                             MessageBoxButtons.YesNoCancel,
                                             MessageBoxIcon.Question,
                                             MessageBoxDefaultButton.Button2);

                // If the yes button was pressed, sends to remote
                if (result == DialogResult.Cancel)
                {
                    e.Cancel = true;
                }
                else if (result == DialogResult.Yes)
                {
                    // cancel the closure of the form.
                    e.Cancel = true;
                    Sinter sinter = new Sinter
                    {
                        HeaderNode = MsgUtil.BuildHeader(
                                      serviceCodes["action"],
                                      serviceCodes["action_default"],
                                      this.RemoteCloseButtonUID
                                    ),
                    };
                    execute_action(sinter);
                }
                else
                {
                    //just the local window
                    this.form = null;
                    root.Remove_Dict_Item(requestedProcessId);
                }
            }
        }

        private void Form_SizeChanged(object sender, EventArgs e)
        {
            if (this.form.WindowState == FormWindowState.Minimized)
            {
                Sinter sinter = new Sinter
                {
                    HeaderNode = MsgUtil.BuildHeader(
                                      serviceCodes["action"],
                                      serviceCodes["action_default"],
                                      this.RemoteMinimizeButtonUID
                                    ),
                };
                execute_action(sinter);
            }
            else if (this.form.WindowState == FormWindowState.Normal)
            {
                if (this.prevWindowState == FormWindowState.Minimized)
                {
                    //bring it back 
                    Sinter sinter = new Sinter
                    {
                        HeaderNode = MsgUtil.BuildHeader(
                                          serviceCodes["action"],
                                          serviceCodes["action_foreground"]
                                        ),
                    };
                    execute_action(sinter);
                }
            }
            else if (this.form.WindowState == FormWindowState.Maximized)
            {
                Sinter sinter = new Sinter
                {
                    HeaderNode = MsgUtil.BuildHeader(
                                      serviceCodes["action"],
                                      serviceCodes["action_foreground"],
                                      this.RemoteZoomButtonUID
                                    ),
                };
                execute_action(sinter);
            }
            this.prevWindowState = this.form.WindowState;

        }

        public void ProcessKeyPress(string keypresses, string targetId)
        {
            Sinter sinter = new Sinter
            {
                HeaderNode = MsgUtil.BuildHeader(serviceCodes["kbd"]),
            };

            sinter.HeaderNode.ParamsInfo = new Params()
            {
                TargetId = targetId,
                Data1 = keypresses,
            };

            execute_kbd(sinter);
        }

        public void ProcessKeyPress(char keypresses, string targetId)
        {
            Sinter sinter = new Sinter
            {
                HeaderNode = MsgUtil.BuildHeader(serviceCodes["kbd"]),
            };

            sinter.HeaderNode.ParamsInfo = new Params()
            {
                TargetId = targetId,
                KeyPress = keypresses,
            };

            execute_kbd(sinter);
        }

        public void ProcessKeyPress(KeyPressEventArgs e, string targetId, string caretPos)
        {
            Sinter sinter = new Sinter
            {
                HeaderNode = MsgUtil.BuildHeader(serviceCodes["kbd"]),
            };

            int characterCode = e.KeyChar;
            String code = e.KeyChar.ToString();

            sinter.HeaderNode.ParamsInfo = new Params()
            {
                TargetId = targetId,
                KeyPress = e.KeyChar,
                //Data1 = e.KeyChar.ToString(),
                Data2 = caretPos
            };

            execute_kbd(sinter);
        }

        public void ProcessKeyPress(string keypresses)
        {
            Sinter sinter = new Sinter {
                HeaderNode = MsgUtil.BuildHeader(serviceCodes["kbd"]),
            };

            sinter.HeaderNode.ParamsInfo = new Params() {
                TargetId = "",
                Data1 = keypresses,
            };

            execute_kbd(sinter);
        }
        #endregion

        #region IWinCommands Implementation
        public ConnectionHandler connection { get; set; }

        public void execute_verify_passcode_req(Sinter _)
        {
            Header header = MsgUtil.BuildHeader(serviceCodes["verify_passcode"], serviceCodes["verify_passcode_req"]);
            header.ParamsInfo = new Params
            {
                Data1 = root.passcode,
            };

            Sinter sinter = new Sinter()
            {
                HeaderNode = header,
            };

            connection.SendMessage(sinter);
        }

        public void execute_verify_passcode(Sinter sinter)
        {
            //handles verify_passcode_res
            bool res = Boolean.Parse(sinter.HeaderNode.ParamsInfo.Data1);
            if (res == false)
            {
                Console.WriteLine("Passcode not accepted by server - Close Connection");
                root.Disconnect(null, null);
                MessageBox.Show("Passcode not correct!");
            }
            else
            {
                Console.WriteLine("Passcode Verified");
                this.bPasscodeVerified = true;
                root.ShowConnected();
            }
        }


        public void execute_stop_scraping()
        {

        }

        public void execute_ls_req(Sinter _)
        {
            Sinter sinter = new Sinter()
            {
                HeaderNode = MsgUtil.BuildHeader(serviceCodes["ls_req"]),
            };

            connection.SendMessage(sinter);
        }

        public void execute_ls_l_req(Sinter _)
        {
            Sinter sinter = new Sinter()
            {
                HeaderNode = MsgUtil.BuildHeader(serviceCodes["ls_l_req"]),
            };

            connection.SendMessage(sinter);
        }


        public void execute_delta(Sinter sinter)
        {
            int subCode = sinter.HeaderNode.SubCode;
            Console.WriteLine("Execute Delta {0}", subCode);

            // TargetId is optional
            string targetId = null;
            if (sinter.HeaderNode.ParamsInfo != null &&
                !string.IsNullOrEmpty(sinter.HeaderNode.ParamsInfo.TargetId))
            {
                targetId = sinter.HeaderNode.ParamsInfo.TargetId;
            }


            if (subCode == serviceCodes["delta_prop_change_name"])
            {
                if (hash.TryGetValue(targetId, out Control control))
                {
                    string newName = sinter.HeaderNode.ParamsInfo.Data2;

                    control.BeginInvoke((Action)(() =>
                    {
                        control.Name = newName;
                        control.Text = newName;
                    }));
                }
            }
            else if (subCode == serviceCodes["delta_prop_change_value"])
            {
                if (this.form!= null && this.form.WindowState == FormWindowState.Minimized)
                {
                    return; //ignore, the msg is due to proxy asked scraper to minimize
                }
                root_entity = sinter.EntityNode;
                // Console.WriteLine("{0}", root_entity.Type);
                if (sinter.EntityNode != null)
                {


                    Console.WriteLine("Prop Change {0}", sinter.EntityNode.Type);
                    if (sinter.EntityNode.Type.Equals("Window", StringComparison.Ordinal))
                    {
                        ((Control)form).BeginInvoke((Action)(() =>
                        {
                            root_entity = sinter.EntityNode;
                            rootPoint.X = root_entity.Left;
                            rootPoint.Y = root_entity.Top;

                            Control tmp = (Control)form;

                            AdjustProperties(root_entity, ref tmp);

                            // potential fixes:
                            //Rectangle screenRectangle = RectangleToScreen(this.ClientRectangle);
                            //int titleHeight = screenRectangle.Top - this.Top;
                            form.Width += (8 + 8);
                            form.Height += (30 + 8);
                            form.Height += 25;

                            Console.WriteLine("Form adjusted");
                        }));
                    }

                }
                else
                {
                    if (hash.TryGetValue(targetId, out Control control))
                    {
                        string newValue = sinter.HeaderNode.ParamsInfo.Data2;
                        control.BeginInvoke((Action)(() =>
                        {
                            if (this.RemoteProcessName.Contains("calc --Calculator") && newValue.Equals("Memory"))
                            {
                                newValue = "";
                            }
                            control.Text = newValue;
                        }));
                    }
                }
            }
            else if (subCode == serviceCodes["delta_subtree_expand"])
            {
                if (sinter.EntityNode == null)
                    return;

                if (sinter.EntityNode.Type.Equals("Menu") || sinter.EntityNode.Type.Equals("MenuItem") || sinter.EntityNode.Type.Equals("menubar"))
                {
                    Console.WriteLine("delta_subtree_expand: type {0}", sinter.EntityNode.Type);
                    UpdateMenu(sinter);
                    return;
                }
            }
            else if (subCode == serviceCodes["delta_subtree_menu"])
            {
                Console.WriteLine("delta_subtree_menu Not handled");
                //UpdateMenu(sinter);
            }
            else if (subCode == serviceCodes["delta_subtree_add"])
            {
                Console.WriteLine("delta_subtree_add Not handled");
                // Not Implemented
            }
            else if (subCode == serviceCodes["delta_subtree_remove"])
            {
                Console.WriteLine("delta_subtree_remove Not handled");
                // Not Implemented
            }
            else if (subCode == serviceCodes["delta_subtree_replace"])
            {
                if (sinter.EntityNode == null)
                    return;

                Console.WriteLine("Type: {0}", sinter.EntityNode.Type);
                if (sinter.EntityNode.Type.Equals("Menu") || sinter.EntityNode.Type.Equals("MenuItem") || sinter.EntityNode.Type.Equals("Text"))
                {
                    Console.WriteLine("delta_subtree_replace Not handled for type {0}", sinter.EntityNode.Type);
                    //UpdateMenu(sinter);
                    return;
                }
                else if (sinter.EntityNode.Type.Equals("List"))
                {
                    if (hash.TryGetValue(sinter.EntityNode.UniqueID, out Control ctrl))
                    {
                            ctrl.BeginInvoke((Action)(() =>
                            {
                                ctrl.Focus();
                                int i = 0;
                                foreach (Entity child in sinter.EntityNode.Children)
                                {
                                    if ((child.States & States.SELECTED) != 0)
                                    {
                                        ((ListView)ctrl).Items[i].Selected = true;
                                    }
                                    else
                                    {
                                        ((ListView)ctrl).Items[i].Selected = false;
                                    }
                                    i++;
                                }
                            }));
                    }
                    return;
                }

                //width = int.Parse(sinter.HeaderNode.ParamsInfo.Data1);
                //height = int.Parse(sinter.HeaderNode.ParamsInfo.Data2);

                // remote/local ratio
                //height_ratio = (float)System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height / height;
                //width_ratio = (float)System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width / width;

                Console.WriteLine("Is form null? {0} {1}", form, requestedProcessId);
                //CloseForm(form);
                // form = null;
                //root.Remove_Dict_Item(requestedProcessId);

                ((Control)form).BeginInvoke((Action)(() =>
               {
                   Console.WriteLine("All ctrl elements");
                   Control[] arr = new Control[form.Controls.Count];

                   form.Controls.CopyTo(arr, 0);

                   foreach (Control element in arr)
                   {
                       Console.WriteLine("{0}", ((Entity)element.Tag).Type);
                       if (!((Entity)element.Tag).Type.Equals("MenuBar"))
                       {
                           form.Controls.Remove(element);
                       }
                   }

                    // form.Controls.Clear();
                    Render(sinter.EntityNode, form);
               }));

                //now show it
                Console.WriteLine("Check that form is not null: {0}", form);
                root.DisplayProxy(form, requestedProcessId);
            }

        }

        // Credit: http://www.csharp411.com/close-all-forms-in-a-thread-safe-manner/
        delegate void CloseMethod(Form form);
        static private void CloseForm(Form form)
        {
            if (form != null)
            {
                if (!form.IsDisposed)
                {
                    if (form.InvokeRequired)
                    {
                        CloseMethod method = new CloseMethod(CloseForm);
                        form.Invoke(method, new object[] { form });
                    }
                    else
                    {
                        Console.WriteLine("CloseForm: Form closed");
                        form.Close();
                    }
                }
            }

        }

        public void execute_event(Sinter sinter)
        {
            //server send this msg to indicate app is closed in server side
            if ((sinter.HeaderNode.ParamsInfo == null)
                ||(sinter.HeaderNode.ParamsInfo.TargetId == null))
            {
                this.RemoteCloseButtonUID = null;
                if (this.form != null)
                {
                    this.form.Invoke(new Action(() => this.form.Close()));
                }
                this.form = null;

                //re-load the list from server
                this.execute_ls_req(null);
            }
        }

        // client related calls
        public void execute_ls_res(Sinter sinter)
        {
            List<Entity> new_processes = sinter.EntityNodes;
            root.PopulateGridView(new_processes);
        }

        int width, height;
        public void execute_ls_l_res(Sinter sinter)
        {
            if (sinter.EntityNode == null)
                return;

            if (sinter.HeaderNode.SubCode == serviceCodes["ls_l_res_dialog"])
            {
                return;
            }

            width = int.Parse(sinter.HeaderNode.ParamsInfo.Data1);
            height = int.Parse(sinter.HeaderNode.ParamsInfo.Data2);

            // remote/local ratio
            height_ratio = (float)System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height / height;
            width_ratio = (float)System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width / width;
            Control ctrl = null;
            if (this.form == null)
            {
                this.form = (AppForm)Render(sinter.EntityNode, null);
                foreach (Entity other_entity in sinter.EntityNodes)
                {
                    //this comes from mac scraper
                    ctrl = Render(other_entity, form);
                }
                this.prevWindowState = FormWindowState.Normal;
            }
            //now show it
            root.DisplayProxy(form, requestedProcessId);
        }

        public void execute_action(Sinter sinter)
        {
            connection.SendMessage(sinter);
        }

        public void execute_kbd(Sinter sinter)
        {
            Console.WriteLine("kbd: " + sinter.HeaderNode.ParamsInfo);

            connection.SendMessage(sinter);
        }

        public void execute_mouse(Sinter sinter)
        {
            connection.SendMessage(sinter);
        }

        public void execute_listener(Sinter sinter)
        {

        }

        #endregion

        #region Utility Methods

        private Point GetCenter(Entity entity)
        {
            int x = entity.Left + (entity.Width) / 2;
            int y = entity.Top + (entity.Height) / 2;
            return new Point(x, y);
        }

        private void UpdateMenu(Sinter sinter)
        {
            if (sinter.EntityNode.Type.Equals("menubar")) {
                /* from Mac OSX */
                foreach (Entity child in sinter.EntityNode.Children) {
                    UpdateMenuItem(child, true, null);
                }
                return;
            }
            else
            {
                /* from windows */
                Entity entity = sinter.EntityNode;
                if (sinter.EntityNode.Type.Equals("MenuItem")
                    && (sinter.EntityNode.Children.Count > 0))
                {
                    if (sinter.EntityNode.Children.Count > 1) Console.WriteLine("MenuItem has {0} children", sinter.EntityNode.Children.Count);
                    if (sinter.EntityNode.Children[0].Type.Equals("Menu"))
                    {
                        entity = sinter.EntityNode.Children[0];
                    }
                }
                UpdateMenuItem(entity, false, null);
            }
        }

        private void UpdateMenuItem(Entity entity, Boolean isOSX, Control ctrl)
        {
            /* mac : the entity is "menubaritem"/menu/menuitem (we cant use menu since the <name> of menu from osx is empty unlike windows)
            /* windows: the entity is type "menu"/menuitem */

            Console.WriteLine("entity.Name: {0}", entity.Name);

            if (ctrl == null)
            {
                if (menuHash.TryGetValue(entity.Name, out ctrl))
                {
                    if (!ctrl.IsHandleCreated)
                    {
                        Console.WriteLine("handle not created yet");
                        return;
                    }
                }
                else
                {
                    Console.WriteLine("cannot find control");
                    return;
                }
            }

            ctrl.BeginInvoke((Action)(() =>
            {
                MenuStrip mStrip = (MenuStrip)ctrl;

                if ((menuHashMenuItem.TryGetValue(entity.Name, out ToolStripMenuItem subMI)) && (subMI.HasDropDownItems))
                {
                    if (subMI.Visible == false)
                    {
                        Console.WriteLine("Show existed Drop Down");
                        subMI.ShowDropDown();
                    }
                }
                else
                {
                    Console.WriteLine("mStrip.Items Count: {0}", mStrip.Items.Count);
                    Boolean isFound = false;
                    foreach (ToolStripMenuItem mi in mStrip.Items)
                    {
                        if (mi.Text.Equals(entity.Name, StringComparison.Ordinal))
                        {
                            // Console.WriteLine("found mi.Text: {0}", mi.Text);
                            menuHashEntity.TryGetValue(mi.Text, out Entity parent);

                            Console.WriteLine("Children Count: {0}", entity.Children.Count);
                            if (isOSX) {
                                /* mac: menubarItem/menu/menuitem , we want menuitem instead of menu*/
                                entity = entity.Children[0];
                            }

                            foreach (Entity child in entity.Children)
                            {
                                addSubMenuItem(child, mStrip, mi);
                            }
                            menuHashMenuItem.TryAdd(entity.Name, mi);
                            Console.WriteLine("Show new Drop Down");
                            mi.ShowDropDown();
                            isFound = true;
                            break;
                        }
                    }
                    if (!isFound) Console.WriteLine("matched menu not found");
                }
            }));
        }


        private void addSubMenuItem(Entity entity_mi, MenuStrip ms, ToolStripMenuItem parent_mi)
        {
            ToolStripMenuItem newMenu = new ToolStripMenuItem(entity_mi.Name);
            menuHashEntity.TryGetValue(parent_mi.Text, out Entity parent);

            newMenu.Name = entity_mi.Name;
            if ((entity_mi.States & States.DISABLED) != 0 || (entity_mi.States & States.INVISIBLE) != 0)
            {
                newMenu.Enabled = false;
            }
            newMenu.Click += Control_Click_MenuItem;
            newMenu.ShortcutKeys = ParseShortcutKeys(entity_mi.Value);

            if (!newMenu.Name.Equals("separator", StringComparison.Ordinal) && parent_mi.DropDownItems.IndexOfKey(newMenu.Name) == -1)
            {
                Console.WriteLine("Adding menuitem: {0}", entity_mi.Name);
                parent_mi.DropDownItems.Add(newMenu);
                menuHash.TryAdd(newMenu.Name, (Control)ms);
                menuHashEntity.TryAdd(newMenu.Name, entity_mi);
                menuHashMenuItem.TryAdd(newMenu.Name, newMenu);
                menuItemParent.TryAdd(newMenu.Name, parent);
            }
            else
            {
                Console.WriteLine("skip menu: {0}", entity_mi.Name);
            }
        }

        private Keys ParseShortcutKeys(string ShortcutString){
            Keys ShortcutKeys = Keys.None;
            if (ShortcutString != null)
            {
                string[] keys = ShortcutString.Split(new Char[] { '+' });
                foreach (string s in keys)
                {
                    s.Trim();
                    Console.WriteLine("key = {0}", s);
                    switch (s)
                    {
                        case ("Ctrl"):
                            ShortcutKeys |= Keys.Control;
                            break;
                        case ("Alt"):
                            ShortcutKeys |= Keys.Alt;
                            break;
                        case ("Shift"):
                            ShortcutKeys |= Keys.Shift;
                            break;
                        case ("Space"):
                            ShortcutKeys |= Keys.Space;
                            break;
                        case ("Tab"):
                            ShortcutKeys |= Keys.Tab;
                            break;
                        case ("F12"):
                            ShortcutKeys |= Keys.F12;
                            break;
                        case ("F11"):
                            ShortcutKeys |= Keys.F11;
                            break;
                        case ("F10"):
                            ShortcutKeys |= Keys.F10;
                            break;
                        default:
                            char[] chars = s.ToCharArray();
                            if (s.Length == 2)
                            {
                                if (chars[0] == 'F' && (chars[1] >= '1' && chars[1] <= '9'))
                                {
                                    ShortcutKeys |= (Keys.F1 + (chars[1] - '1'));
                                    break;
                                }
                            }
                            else if (s.Length == 1)
                            {
                                if (chars[0] >= '0' && chars[0] <= '9')
                                {
                                    ShortcutKeys |= (Keys.D0 + (chars[0] - '0'));
                                    break;
                                }
                                else if (chars[0] >= 'A' && chars[0] <= 'Z')
                                {
                                    ShortcutKeys |= (Keys.A + (chars[0] - 'A'));
                                    break;
                                }
                            }
                            break;
                    }
                }
            }
            return ShortcutKeys;    
        }

        void AdjustProperties(Entity entity, ref Control control)
        {
            control.Height = (int)(entity.Height * height_ratio);
            control.Width = (int)(entity.Width * width_ratio);

            control.Name = entity.Name;
            control.Text = entity.Name;

            control.Top = (int)((entity.Top - rootPoint.Y) * height_ratio);
            control.Left = (int)((entity.Left - rootPoint.X) * width_ratio);

            if ((entity.States & States.DISABLED) != 0)
            {
                control.Enabled = false;
            }
            if ((entity.States & States.CHECKED) != 0)
            {
                Console.WriteLine("{0}/{1} states is CHECKED", entity.Type, entity.Name);
                if (entity.Type == "CheckBox")
                {
                    ((CheckBox)control).Checked = true;
                }
            }
            if ((entity.States & States.SELECTED) != 0)
            {
                Console.WriteLine("{0}/{1} states is SELECTED", entity.Type, entity.Name);
                if (entity.Type == "RadioButton")
                {
                    ((RadioButton)control).Checked = true;
                }
            }
            //control.Tag = new TagInfo(entity.UniqueID , GetCenter(entity));
        }

        private bool HasChildren(Entity entity)
        {
            return entity.Children != null &&
              entity.Children.Count > 0;
        }

#endregion
    }
}
