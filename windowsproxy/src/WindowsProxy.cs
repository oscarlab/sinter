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
using System.IO;
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
        private static log4net.ILog log = log4net.LogManager.GetLogger("Proxy");

        RootForm root;
        AppForm form;

        Point rootPoint;
        Entity root_entity;

        static Dictionary<string, object> winControls;
        Type type;

        float height_ratio;
        float width_ratio;
        bool flagDeferMenuExpansion; //windows only
        bool flagTriggerByMenuItemClick; //windows only

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
        //Dictionary<int, string> sendKeysCodes;

        public bool bPasscodeVerified { get; private set; }
        public Sinter baseXML { get; set; }

        Timer timer;
        ConcurrentDictionary<string, Control> hash = new ConcurrentDictionary<string, Control>();
        ConcurrentDictionary<string, Control> menuHash = new ConcurrentDictionary<string, Control>();
        ConcurrentDictionary<string, Entity> menuHashEntity = new ConcurrentDictionary<string, Entity>();
        ConcurrentDictionary<string, ToolStripMenuItem> menuHashMenuItem = new ConcurrentDictionary<string, ToolStripMenuItem>();
        ConcurrentDictionary<string, Entity> menuItemParent = new ConcurrentDictionary<string, Entity>();
        ConcurrentDictionary<string, Entity> comboBoxParent = new ConcurrentDictionary<string, Entity>();
        ConcurrentDictionary<string, Entity> comboBoxEntities = new ConcurrentDictionary<string, Entity>();

        private readonly ConcurrentDictionary<string, Form> dictSubForms = new ConcurrentDictionary<string, Form>();
        private readonly ConcurrentDictionary<Form, string[]> dictFormCtrlButtons = new ConcurrentDictionary<Form, string[]>();
        private readonly ConcurrentDictionary<Form, FormWindowState> dictFormWindowStatePrev = new ConcurrentDictionary<Form, FormWindowState>();

        //Boolean mainWindowOpened = false; // May need to change to accomodate more windows, use the names to distinguish the windows

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

#if DEBUG
            string xmlFilePath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "//sinterxml.txt";
            File.Delete(xmlFilePath);
#endif
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
        public string RemotePlatform = "";

        #region Main Renderer
        string invoking_method_name;
        MethodInfo invoking_method;

        Control Render(Entity entity, Control parent_control = null)
        {
            log.DebugFormat("Rendering: {0}/{1}", entity.Type, entity.Name);

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

            if (invoking_method_name.Equals("RenderDialog") && parent_control != null)
            {
                /* trick for window7 calc statistic mode (window/pane/window)
                   treat sub window as pane instead of dialog so it will show correctly */
                if (this.RemoteProcessName.Contains("Calculator") &&
                    this.RemotePlatform.StartsWith("Microsoft") &&
                    !entity.Name.Equals("About Calculator"))
                {
                    invoking_method_name = "RenderPane";
                }
            }

            invoking_method = type.GetMethod(invoking_method_name,
              BindingFlags.NonPublic | BindingFlags.Instance);
            //log.DebugFormat("Method {0}", invoking_method);
            Control current_control = null;
            if (invoking_method != null)
            {
                if (hash.TryGetValue(entity.UniqueID, out Control prev))
                {
                    hash.TryRemove(entity.UniqueID, out Control rem);
                    //log.DebugFormat("Remove previous control from parent");
                    if (parent_control != null)
                        parent_control.Controls.Remove(prev);
                }

                current_control = (Control)invoking_method.Invoke(this,
                  new object[] { entity, parent_control });

                if (current_control == null)
                {
                    log.DebugFormat("Current control is null?");
                    return null;
                }

                if (parent_control != null && parent_control != current_control)
                {
                    if (invoking_method_name == "RenderMenuBar")
                    {
                        Form form = (Form)parent_control;
                        form.MainMenuStrip = (MenuStrip)current_control;
                    }
                    if (invoking_method_name != "RenderDialog")
                        parent_control.Controls.Add(current_control);
                }
                if (parent_control != current_control)
                {
                    // add to hash
                    hash.TryAdd(entity.UniqueID, current_control);
                }


            }
            else
            {
                log.Error("[ERROR] invoking_method is null");
            }

            // check children
            log.DebugFormat("{0}/{1} has {2} children", entity.Type, entity.Name, entity.Children.Count);
            if (!HasChildren(entity))
                return current_control;

            if (entity.Type == "Spinner")
            {
                foreach (Entity child_entity in entity.Children)
                    Render(child_entity, parent_control); //otherwise "Botton"s won't show as children of "Edit"
            }
            else
            {
                foreach (Entity child_entity in entity.Children)
                    Render(child_entity, current_control);
            }

            return current_control;
        }
        #endregion

        #region Invividual Render Methods

        private int getButtonIndex(string buttonName)
        {
            //controlbuttons[0] : UniqueID of Minimize
            //controlbuttons[1] : UniqueID of Maximize
            //controlbuttons[2] : UniqueID of Close
            if (buttonName.Equals("Minimize"))
            {
                return 0;
            }
            else if (buttonName.Equals("Maximize"))
            {
                return 1;
            }
            else //(buttonName.Equals("Close"))
            {
                return 2;
            }
        }

        private Control RenderButton(Entity entity, Control parent)
        {

            log.DebugFormat("RenderButton entity.UniqueId: {0} {1}", entity.UniqueID, entity.Name);
            /*
            if (hash.TryGetValue(entity.UniqueID, out Control res))
            {
                log.DebugFormat("Remove");
                parent.Controls.Remove(res);
                hash.TryRemove(entity.UniqueID, out Control rem);
            }
            */

            /* mapping mac control buttons to windows */
            if (entity.Name.Equals("AXCloseButton"))
            {
                entity.Name = "Close";
            }
            else if (entity.Name.Equals("AXMinimizeButton"))
            {
                entity.Name = "Minimize";
            }
            else if (entity.Name.Equals("AXZoomButton"))
            {
                entity.Name = "Maximize";
            }
            /* mapping mac control buttons to windows */

            if (entity.Name.Equals("Maximize") || entity.Name.Equals("Minimize") || entity.Name.Equals("Close"))
            {
                string[] controlbuttonIDs;
                if (!dictFormCtrlButtons.TryGetValue((Form)parent, out controlbuttonIDs))
                {   //no entry yet, create new one
                    controlbuttonIDs = new string[3];
                    dictFormCtrlButtons.TryAdd((Form)parent, controlbuttonIDs);
                }
                controlbuttonIDs[getButtonIndex(entity.Name)] = entity.UniqueID;

                /*
                if (entity.Name.Equals("Close"))
                {
                    RemoteCloseButtonUID = entity.UniqueID;
                }
                
                else */
                if (entity.Name.Equals("Minimize"))
                {
                    //RemoteMinimizeButtonUID = entity.UniqueID;
                    if ((entity.States & States.DISABLED) == 0) //not disabled
                    {
                        this.form.MinimizeBox = true;
                    }
                }
                else if (entity.Name.Equals("Maximize"))
                {
                    //RemoteZoomButtonUID = entity.UniqueID;
                    if ((entity.States & States.DISABLED) == 0) //not disabled
                    {
                        this.form.MaximizeBox = true;
                    }
                }
                return parent;
            }

            Control control = new Button();
            AdjustProperties(entity, ref control);
            log.DebugFormat("Executing RenderButton");

            control.Tag = entity;
            control.Click += Control_Click_Button;

            return control;
        }

        private Control RenderRadioButton(Entity entity, Control parent)
        {
            /*
            if (hash.TryGetValue(entity.UniqueID, out Control res))
            {
                log.DebugFormat("Remove");
                parent.Controls.Remove(res);
                hash.TryRemove(entity.UniqueID, out Control rem);
            }
            */

            Control control = new RadioButton();
            AdjustProperties(entity, ref control);
            log.DebugFormat("Executing RenderRadioButton");

            control.Tag = entity;
            control.Click += Control_ClickRadioButton;

            return control;
        }

        private Control RenderCalendar(Entity entity, Control parent)
        {
            return parent;
        }

        private Control RenderDateTimePicker(Entity entity, Control parent)
        {
            Control control = new DateTimePicker();
            AdjustProperties(entity, ref control);
            log.DebugFormat("Executing RenderDateTimePicker");
            control.Tag = entity;

            DateTimePicker dateTimePicker = (DateTimePicker)control;
            dateTimePicker.CustomFormat = "MM/dd/yyyy";
            dateTimePicker.Format = DateTimePickerFormat.Custom;
            dateTimePicker.Value = DateTime.Parse(entity.Name);

            return control;
        }

        private Control RenderCheckBox(Entity entity, Control parent)
        {
            Control control = new CheckBox();
            AdjustProperties(entity, ref control);
            log.DebugFormat("Executing RenderCheckBox");

            control.Tag = entity;
            ((CheckBox)control).Click += Control_Click_Button;

            return control;
        }

        private Control RenderComboBox(Entity entity, Control parent)
        {
            log.DebugFormat("Rendering ComboBox");
            Control control = new ComboBox();
            AdjustProperties(entity, ref control);
            control.Text = entity.Value;

            control.Tag = entity;

            foreach (Entity child_entity in entity.Children)
            {
                log.DebugFormat("{0} {1}", child_entity.Name, child_entity.Type);
                if (child_entity.Type == "List")
                {
                    foreach (Entity listItem in child_entity.Children)
                    {
                        ToolStripMenuItem tmp = new ToolStripMenuItem();
                        tmp.Name = listItem.Name;
                        tmp.Text = listItem.Name;
                        if ((listItem.States & States.SELECTED) != 0)
                        {
                            control.Text = listItem.Name;
                        }
                        ((ComboBox)control).Items.Add(tmp);
                        comboBoxParent.TryAdd(listItem.Name, child_entity);
                        comboBoxEntities.TryAdd(listItem.Name, listItem);
                    }
                    comboBoxParent.TryAdd(child_entity.Name, entity);
                }
            }
            ((ComboBox)control).SelectedIndexChanged += Control_ComboBox_SelectedIndexChanged;
            log.DebugFormat("Rendered ComboBox");
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
            foreach (Word w in words)
            {
                if (w.newline == "1")
                {
                    control.Text += "\r";
                }
                else
                {
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
            log.DebugFormat("Executing RenderEdit");

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
                log.DebugFormat("Rendered ListItem: {0}", child.Name);
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
            log.DebugFormat("Executing RenderList");
            control.Tag = entity;
            control.Click += Control_Click_Button;
            listView.EndUpdate();
            return control;
        }

        private Control RenderListItem(Entity entity, Control parent)
        {
            //log.DebugFormat("Rendered ListItem");
            return parent;
        }

        private Control RenderMenu(Entity entity, Control parent)
        {
            return parent;
        }

        private Control RenderMenuBar(Entity entity, Control parent)
        {
            // From http://www.c-sharpcorner.com/blogs/create-menustrip-dynamically-in-c-sharp1
            log.DebugFormat("Executing RenderMenuBar");
            log.DebugFormat("entity.Type: {0}", entity.Type);

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
                log.DebugFormat("menuHashEntity.TryAdd(Name: {0}, entity: {0}", child_entity.Name, child_entity);
                menuHashEntity.TryAdd(child_entity.Name, child_entity);
                menuHash.TryAdd(child_entity.Name, control);

                ToolStripMenuItem mi = new ToolStripMenuItem(child_entity.Name);
                mi.Name = child_entity.Name;
                //mi.Click += Control_MenuItem_Expanded;

                ContextMenuStrip miDropDown = new ContextMenuStrip();
                mi.DropDown = miDropDown;
                miDropDown.Opening += new System.ComponentModel.CancelEventHandler(ContextMenu_Opening);
                mi.DropDownOpening += Control_MenuItem_Expanded;
                mi.DropDownClosed += Control_MenuItem_Collapsed;
                ((MenuStrip)control).Items.Add(mi);
                menuHashMenuItem.TryAdd(mi.Name, mi);
                mi.ShortcutKeys = ParseShortcutKeys(child_entity.Value);

                /* OSX server sends whole menu in DOM at the first beginning */
                if (child_entity.Children.Count > 0)
                {
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
            log.DebugFormat("Executing RenderButton");

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
            //return parent;
            entity.Width -= 18; //not to cover the bottons of spinner
            return RenderEdit(entity, parent);
            /* 
            //possible tweak: in case we want to have better user experience in release version
            Control control = RenderText(entity, parent);
            ((TextBox)control).ReadOnly = true;
            ((TextBox)control).Text = entity.Value;
            return control;
            */
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
            log.DebugFormat("Executing RenderText");

            if (((entity.States & States.SELECTABLE) == 0) && (this.RemoteProcessName.Contains("Calculator") == true))
            {
                //windows 7 calculator will fall to here
                ((TextBox)control).ReadOnly = true;
                ((TextBox)control).BorderStyle = 0;
            }

            AdjustProperties(entity, ref control);

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
                log.DebugFormat("original width = {0}", form.Width);
                form.Width = 500;
                log.DebugFormat("new width = {0}", form.Width);
            }

            //register event listener and delegate
            form.FormClosing += Form_Closing;
            form.SizeChanged += Form_SizeChanged;
            form.delegateKeyPresses = ProcessKeyPress;
            form.MinimizeBox = false;
            form.MaximizeBox = false;
            return control;
        }

        private Control RenderDialog(Entity entity, Control parent)
        {
            Form localForm = new Form();
            Control control = localForm as Control;
            AdjustProperties(entity, ref control);
            localForm.Font = new Font("Segoe UI", 8);
            localForm.TopLevel = true;

            localForm.Width += 100;
            localForm.Height += 200;
            localForm.SizeChanged += Form_SizeChanged;
            localForm.FormClosing += Dialog_Closing;
            localForm.MinimizeBox = false;
            localForm.MaximizeBox = false;

            control.Tag = entity;
            dictSubForms.TryAdd(entity.UniqueID, localForm);

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

            log.DebugFormat("send action default: {0}", sinter.HeaderNode.ParamsInfo.ToString());
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

            log.DebugFormat(sinter.HeaderNode.ParamsInfo.ToString());
            execute_mouse(sinter);

            isSent = true;
            timer.Enabled = true;
        }

        private void ContextMenu_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = flagDeferMenuExpansion;
            if (e.Cancel)
            {
                log.DebugFormat("ContextMenu_Opening(): flagDeferMenuExpansion = " + flagDeferMenuExpansion);
            }
        }

        private void Control_MenuItem_Expanded(object sender, EventArgs e)
        {
            ToolStripDropDownItem item = sender as ToolStripDropDownItem;
            log.DebugFormat("MenuItem {0} Expanded", item.Text);

            menuHashEntity.TryGetValue(item.Text, out Entity entity);

            if (entity == null)
            {
                log.DebugFormat("[Warning!] cannot find entity");
                return;
            }
            log.DebugFormat("MenuItem ID {0}", entity.UniqueID);

            Sinter sinter = sinter = new Sinter
            {
                HeaderNode = MsgUtil.BuildHeader(
                      serviceCodes["action"],
                      serviceCodes["action_expand"],
                      entity.UniqueID),
            };

            log.DebugFormat(sinter.HeaderNode.ParamsInfo.ToString());
            execute_action(sinter);

            isSent = true;
            timer.Enabled = true;

            if (RemotePlatform != null && RemotePlatform.StartsWith("Microsoft"))
            {
                flagDeferMenuExpansion = true;
                log.DebugFormat("set FLAG flagDeferMenuExpansion");
            }
        }

        private void Control_MenuItem_Collapsed(object sender, EventArgs e)
        {
            ToolStripDropDownItem item = sender as ToolStripDropDownItem;
            log.DebugFormat("MenuItem {0} Collapsed", item.Text);

            menuHashEntity.TryGetValue(item.Text, out Entity entity);

            if (entity == null)
            {
                log.DebugFormat("[Warning!] cannot find entity");
                return;
            }
            log.DebugFormat("MenuItem ID {0}", entity.UniqueID);

            Sinter sinter = sinter = new Sinter
            {
                HeaderNode = MsgUtil.BuildHeader(
                      serviceCodes["action"],
                      serviceCodes["action_collapse"],
                      entity.UniqueID),
            };

            log.DebugFormat(sinter.HeaderNode.ParamsInfo.ToString());
            execute_action(sinter);

            isSent = true;
            timer.Enabled = true;
        }

        private void Control_ComboBox_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            ComboBox comboBox = (ComboBox)sender;
            int selectedIndex = comboBox.SelectedIndex;
            Object selectedItem = comboBox.SelectedItem;

            if (comboBoxEntities.TryGetValue(selectedItem.ToString(), out Entity listItem))
            {
                Sinter sinter = new Sinter
                {
                    HeaderNode = MsgUtil.BuildHeader(
                  serviceCodes["action"],
                  serviceCodes["action_default"],
                  listItem.UniqueID
                ),
                };
                execute_mouse(sinter);
            }
            else
            {
                Entity comboBox_entity = (Entity)((Control)comboBox).Tag;
                if (comboBox_entity == null)
                    return;
                Sinter sinter = new Sinter
                {
                    HeaderNode = MsgUtil.BuildHeader(
                  serviceCodes["action"],
                  serviceCodes["action_select"],
                  comboBox_entity.UniqueID,
                  selectedIndex.ToString()
                ),
                };
                execute_mouse(sinter);
            }
        }

        private void Control_Click_ComboBox(object sender, EventArgs e)
        {
            log.DebugFormat("Begin Control_Click_Combo_Box");
            if (isSent)
                return;

            ComboBox item = (ComboBox)sender;
            // Entity entity;
            log.DebugFormat("MenuItem Click {0}", item.Text);
            comboBoxEntities.TryGetValue(item.Text, out Entity entity);
            if (entity == null)
                return;
            log.DebugFormat("MenuItem ID {0}", entity.UniqueID);
            List<string[]> lists = new List<string[]>();
            string name = item.Name;

            string id = null;
            comboBoxParent.TryGetValue(name, out Entity test);
            log.DebugFormat("{0}", test.UniqueID);

            Point start = GetCenter(entity);
            log.DebugFormat("Adding {0} to list...", entity.Name);
            lists.Add(new string[] { start.X.ToString(), start.Y.ToString() });

            while (comboBoxParent.TryGetValue(name, out Entity parent))
            {
                if (!comboBoxParent.TryGetValue(parent.Name, out Entity stop))
                {
                    id = parent.UniqueID;
                    log.DebugFormat("FInal Name: {0}", parent.Name);
                    break;
                }
                Point tmp = GetCenter(parent);
                log.DebugFormat("Adding {0} to list...", parent.Name);
                lists.Add(new string[] { tmp.X.ToString(), tmp.Y.ToString() });
                name = parent.Name;
            }

            lists.Reverse();

            log.DebugFormat("Menu Item {0}", id);

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

            log.DebugFormat(sinter.HeaderNode.ParamsInfo.ToString());
            execute_action(sinter);

            isSent = true;
            timer.Enabled = true;
        }

        private void Control_Click_MenuItem(object sender, EventArgs e)
        {
            ToolStripMenuItem targetMenuItem = (ToolStripMenuItem)sender;
            log.DebugFormat("MenuItem Click {0}", targetMenuItem.Text);
            menuHashEntity.TryGetValue(targetMenuItem.Text, out Entity entity);
            if (entity == null)
            {
                log.DebugFormat("entity for clicked MenuItem not found");
                return;
            }

            // Begin: for Mac Scraper
            // action_default won't accept by WindowScraper because AutomationElement doesn't exist when menu is collapsed
            Sinter sinter_mac = new Sinter
            {
                HeaderNode = MsgUtil.BuildHeader(
                              serviceCodes["action"],
                              serviceCodes["action_default"],
                              entity.UniqueID
                            ),
            };
            execute_action(sinter_mac);
            // End: for Mac Scraper

            string name = targetMenuItem.Name;
            string parentRuntimeID = null;
            List<string[]> lists = new List<string[]>();
            lists.Add(new string[] { entity.Name, entity.UniqueID });

            while (menuItemParent.TryGetValue(name, out Entity parent))
            {
                if (!menuItemParent.TryGetValue(parent.Name, out Entity stop))
                {
                    parentRuntimeID = parent.UniqueID;
                    name = parent.Name;
                    log.DebugFormat("Final Parent Name: {0}, RuntimeID: {1}", parent.Name, parentRuntimeID);

                    break;
                }
                lists.Add(new string[] { parent.Name, parent.UniqueID });
                name = parent.Name;
            }
            lists.Reverse();

            Sinter sinter = new Sinter
            {
                HeaderNode = MsgUtil.BuildHeader(
                  serviceCodes["action"],
                  serviceCodes["action_expand_and_select"],
                  parentRuntimeID,
                  lists,
                  "0",
                  "",
                  ""
                ),
            };

            log.DebugFormat(sinter.HeaderNode.ParamsInfo.ToString());
            execute_action(sinter);

            isSent = true;
            timer.Enabled = true;

            if (RemotePlatform != null && RemotePlatform.StartsWith("Microsoft"))
            {
                flagTriggerByMenuItemClick = true;
                log.DebugFormat("set FLAG flagTriggerByMenuItemClick");
            }
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

            log.DebugFormat(sinter.HeaderNode.ParamsInfo.ToString());
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
        private void CloseSingleForm(Form form1)
        {
            if (form1 != null)
            {
                dictFormCtrlButtons.TryRemove(form1, out _);
                dictFormWindowStatePrev.TryRemove(form1, out _);

                if (form1.IsHandleCreated)
                {
                    form1.BeginInvoke((Action)(() =>
                    {
                        form1.Close();
                    }));
                }
            }
        }

        public void CloseAllForms()
        {
            dictFormWindowStatePrev.Clear();
            dictFormCtrlButtons.Clear();
            CloseSingleForm(this.form);
            this.form = null;

            foreach (KeyValuePair<string, Form> entry in dictSubForms)
            {
                CloseSingleForm(entry.Value);
            }
            dictSubForms.Clear();
        }

        private void Form_Closing(object sender, FormClosingEventArgs e)
        {
            Form closingForm = (Form)sender;
            if (dictFormCtrlButtons.TryGetValue(closingForm, out string[] controlbuttonIDs)
                && e.CloseReason == CloseReason.UserClosing)
            {
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
                    // cancel the closure of the form.
                    e.Cancel = true;
                }
                else if (result == DialogResult.Yes)
                {
                    // cancel local closure of the form, pass to scraper
                    e.Cancel = true;
                    Sinter sinter = new Sinter
                    {
                        HeaderNode = MsgUtil.BuildHeader(
                                      serviceCodes["action"],
                                      serviceCodes["action_default"],
                                      controlbuttonIDs[getButtonIndex("Close")]
                                    ),
                    };
                    execute_action(sinter);
                }
                else
                {
                    //User answer No, just the local window
                    this.form = null;
                    root.Remove_Dict_Item(requestedProcessId);
                }
            }
            else
            {
                //just the local window
                this.form = null;
                root.Remove_Dict_Item(requestedProcessId);
                return;
            }
        }


        private void Dialog_Closing(object sender, FormClosingEventArgs e)
        {
            Form closingForm = (Form)sender;
            if (dictFormCtrlButtons.TryGetValue(closingForm, out string[] controlbuttonIDs)
                && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Sinter sinter = new Sinter
                {
                    HeaderNode = MsgUtil.BuildHeader(
                                  serviceCodes["action"],
                                  serviceCodes["action_default"],
                                  controlbuttonIDs[getButtonIndex("Close")]
                                ),
                };
                execute_action(sinter);
            }
            else
            {
                Form form = (Form)sender;
                foreach (KeyValuePair<string, Form> entry in dictSubForms)
                {
                    if (entry.Value == form)
                    {
                        CloseSingleForm(entry.Value);
                        dictSubForms.TryRemove(entry.Key, out _);
                        return;
                    }
                }
            }
        }

        private void Form_SizeChanged(object sender, EventArgs e)
        {
            Form form1 = (Form)sender;
            FormWindowState prevState;
            if (!dictFormWindowStatePrev.TryGetValue(form1, out prevState))
            {
                log.DebugFormat("ERROR - no entry for dictFormWindowStatePrev");
            }


            if (dictFormCtrlButtons.TryGetValue(form1, out string[] controlbuttonIDs))
            {
                if (form1.WindowState == FormWindowState.Minimized)
                {
                    /* user triggered Minimized, notify Scraper */
                    Sinter sinter = new Sinter
                    {
                        HeaderNode = MsgUtil.BuildHeader(
                                          serviceCodes["action"],
                                          serviceCodes["action_default"],
                                          controlbuttonIDs[getButtonIndex("Minimize")]
                                        ),
                    };
                    execute_action(sinter);
                }
                else if (form1.WindowState == FormWindowState.Normal)
                {
                    if (prevState == FormWindowState.Minimized)
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
                else if (form1.WindowState == FormWindowState.Maximized)
                {
                    Sinter sinter = new Sinter
                    {
                        HeaderNode = MsgUtil.BuildHeader(
                                          serviceCodes["action"],
                                          serviceCodes["action_default"],
                                          controlbuttonIDs[getButtonIndex("Maximize")]
                                        ),
                    };
                    execute_action(sinter);
                }
            }
            dictFormWindowStatePrev.TryUpdate(form1, form1.WindowState, prevState);

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
            Sinter sinter = new Sinter
            {
                HeaderNode = MsgUtil.BuildHeader(serviceCodes["kbd"]),
            };

            sinter.HeaderNode.ParamsInfo = new Params()
            {
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
                log.Error("Passcode not accepted by server - Close Connection");
                root.Disconnect(null, null);
                MessageBox.Show("Passcode not correct!");
            }
            else
            {
                log.DebugFormat("Passcode Verified");
                this.bPasscodeVerified = true;
                root.ShowConnected();
                this.RemotePlatform = sinter.HeaderNode.ParamsInfo.Data2;
                log.DebugFormat("RemotePlatform = {0}", this.RemotePlatform);
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
            log.DebugFormat("Execute Delta {0}", subCode);

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
                if (this.form != null && this.form.WindowState == FormWindowState.Minimized)
                {
                    return; //ignore, the msg is due to proxy asked scraper to minimize
                }
                root_entity = sinter.EntityNode;
                // log.DebugFormat("{0}", root_entity.Type);
                if (sinter.EntityNode != null)
                {
                    log.DebugFormat("Prop Change {0}", sinter.EntityNode.Type);
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

                            log.DebugFormat("Window form adjusted");
                            foreach (Entity child in sinter.EntityNode.Children)
                            {
                                AdjustSubTreeProperties(child);
                            }
                        }));
                    }
                    else
                    {
                        AdjustSubTreeProperties(sinter.EntityNode);
                    }
                }
                else
                {
                    if (hash.TryGetValue(targetId, out Control control))
                    {
                        string newValue = sinter.HeaderNode.ParamsInfo.Data2;
                        control.BeginInvoke((Action)(() =>
                        {
                            if (this.RemoteProcessName.Contains("Calculator") &&
                                (newValue.Equals("Memory") || newValue.Equals("Running History")))
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

                if (flagDeferMenuExpansion == true)
                {
                    flagDeferMenuExpansion = false;
                    log.DebugFormat("clear FLAG flagDeferMenuExpansion");
                }

                if (flagTriggerByMenuItemClick == true)
                {
                    // the expansion is triggered by sinter (not user) when MenuItem clicked, so do not process/show to User
                    log.DebugFormat("clear FLAG flagTriggerByMenuItemClick");
                    flagTriggerByMenuItemClick = false;
                    return;
                }

                if (sinter.EntityNode.Type.Equals("menubar"))
                {
                    log.DebugFormat("ignore {0} from mac", subCode);
                    return;
                }
                else if (sinter.EntityNode.Type.Equals("Menu") || sinter.EntityNode.Type.Equals("MenuItem"))
                {
                    log.DebugFormat("delta_subtree_expand: {0}/{1}", sinter.EntityNode.Type, sinter.EntityNode.Name);
                    UpdateMenu(sinter);
                    return;
                }
            }
            else if (subCode == serviceCodes["delta_subtree_menu"])
            {
                log.DebugFormat("506 - delta_subtree_menu Not handled");
                //UpdateMenu(sinter);
            }
            else if (subCode == serviceCodes["delta_subtree_add"])
            {
                log.DebugFormat("501 - delta_subtree_add Not handled");
                // Not Implemented
            }
            else if (subCode == serviceCodes["delta_subtree_remove"])
            {
                log.DebugFormat("503 - delta_subtree_remove Not handled");
                // Not Implemented
            }
            else if (subCode == serviceCodes["delta_subtree_replace"])
            {
                execute_delta_subtree_replace(sinter.EntityNode);
            }
        }

        public void execute_delta_subtree_replace(Entity entity)
        {
            if (entity == null)
                return;

            switch (entity.Type)
            {
                case "Pane":
                    {
                        log.DebugFormat("502 - delta_subtree_replace {0}/{1}", entity.Type, entity.Name);
                        ((Control)form).BeginInvoke((Action)(() =>
                        {
                            log.DebugFormat("Remove All ctrl elements");
                            Control[] arr = new Control[form.Controls.Count];
                            form.Controls.CopyTo(arr, 0);
                            foreach (Control element in arr)
                            {
                            //log.DebugFormat("{0}", ((Entity)element.Tag).Type);
                            if (!((Entity)element.Tag).Type.Equals("MenuBar"))
                                {
                                    form.Controls.Remove(element);
                                }
                            }
                        // form.Controls.Clear();

                        log.DebugFormat("Render New Elements");
                            Render(entity, form);
                        }));
                        break;
                    }
                case "List":
                    {
                        log.DebugFormat("502 - delta_subtree_replace {0}/{1}", entity.Type, entity.Name);
                        if (hash.TryGetValue(entity.UniqueID, out Control ctrl))
                        {
                            ctrl.BeginInvoke((Action)(() =>
                            {
                            /* Calculator - Unit Converter "From Unit" and "To Unit" */
                                Control comboBoxControl;
                                if (comboBoxParent.TryGetValue(entity.Name, out Entity parent_entity)
                                    && parent_entity.Type.Equals("ComboBox")
                                    && hash.TryGetValue(parent_entity.UniqueID, out comboBoxControl))
                                {
                                    log.DebugFormat("Found Combobox Parent {0}", parent_entity.Name);

                                    foreach (ToolStripMenuItem oldLi in ((ComboBox)comboBoxControl).Items)
                                    {
                                        log.DebugFormat("oldlistItem {0}", oldLi.Text);
                                        comboBoxParent.TryRemove(oldLi.Text, out Entity _);
                                        comboBoxEntities.TryRemove(oldLi.Text, out Entity _);
                                    }
                                    ((ComboBox)comboBoxControl).Items.Clear();

                                    foreach (Entity child in entity.Children)
                                    {
                                        log.DebugFormat("Rendered ToolStripMenuItem: {0}", child.Name);
                                        ToolStripMenuItem mi = new ToolStripMenuItem();
                                        mi.Name = child.Name;
                                        mi.Text = child.Name;
                                        if ((child.States & States.SELECTED) != 0)
                                        {
                                            ((ComboBox)comboBoxControl).Text = child.Name;
                                        }
                                        ((ComboBox)comboBoxControl).Items.Add(mi);
                                        comboBoxParent.TryAdd(child.Name, entity);
                                        comboBoxEntities.TryAdd(child.Name, child);
                                    }
                                }
                                else
                                {
                                /* Calculator - History window */
                                    ctrl.Focus();
                                    int i = 0;
                                    ListView listView = (ListView)ctrl;

                                    foreach (ListViewItem oldLi in listView.Items)
                                    {
                                        log.DebugFormat("oldlistItem {0}", oldLi.Text);
                                    }
                                    listView.Items.Clear();

                                    foreach (Entity child in entity.Children)
                                    {
                                        log.DebugFormat("Rendered ListItem: {0}", child.Name);
                                        ListViewItem li = new ListViewItem(child.Name);
                                        listView.Items.Add(li);
                                        if ((child.States & States.SELECTED) != 0)
                                        {
                                            listView.Items[i].Selected = true;
                                        }
                                        i++;
                                    }
                                }
                            }));
                        }
                        break;
                    }
                case "Menu": //another 504 msg will be handled for Menu
                case "MenuItem": //another 504 msg will be handled for Menu
                case "Text":
                default:
                    log.DebugFormat("502 - delta_subtree_replace Not handled for {0} {1}", entity.Type, entity.Name);
                    return;
            }

            //now show it
            root.DisplayProxy(form, requestedProcessId);
            dictFormWindowStatePrev.TryUpdate(form, form.WindowState, FormWindowState.Normal);
            return;
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
                        log.DebugFormat("CloseForm: Form closed");
                        form.Close();
                    }
                }
            }

        }

        public void execute_event(Sinter sinter)
        {
            //server send this msg to indicate app is closed in server side
            if ((sinter.HeaderNode.ParamsInfo == null)
                || (sinter.HeaderNode.ParamsInfo.TargetId == null))
            {
                CloseAllForms();
                //re-load the list from server
                this.execute_ls_req(null);
            }
            else
            {
                if ((hash.TryGetValue(sinter.HeaderNode.ParamsInfo.TargetId, out Control control))
                    && (control != null))
                {
                    hash.TryRemove(sinter.HeaderNode.ParamsInfo.TargetId, out Control old_control);

                    Control parent_control = control.Parent;
                    if (parent_control != null)
                    {
                        parent_control.Invoke(new Action(() => parent_control.Controls.Remove(control)));
                    }
                    if (control is Form)
                    {
                        CloseSingleForm((Form)control);
                        dictSubForms.TryRemove(sinter.HeaderNode.ParamsInfo.TargetId, out _);
                    }
                    if (this.form != null)
                    {
                        root.DisplayProxy(form, requestedProcessId);
                        dictFormWindowStatePrev.TryUpdate(form, form.WindowState, FormWindowState.Normal);
                    }
                }
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
                if (!dictSubForms.ContainsKey(sinter.EntityNode.UniqueID))
                {
                    Form dialog = (Form)Render(sinter.EntityNode, null);
                    if (dialog != null)
                    {
                        dictFormWindowStatePrev.TryAdd(dialog, FormWindowState.Normal);
                        root.DisplayDialog(dialog, requestedProcessId);
                    }
                }
                return;
            }

            width = int.Parse(sinter.HeaderNode.ParamsInfo.Data1);
            height = int.Parse(sinter.HeaderNode.ParamsInfo.Data2);

            // remote/local ratio
            height_ratio = (float)System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height / height;
            width_ratio = (float)System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width / width;

            dictFormWindowStatePrev.Clear();
            if (this.form == null)
            {
                this.form = (AppForm)Render(sinter.EntityNode, null);
                foreach (Entity other_entity in sinter.EntityNodes)
                {
                    //this comes from mac scraper
                    Control ctrl = Render(other_entity, form);
                }
            }
            //now show it
            root.DisplayProxy(form, requestedProcessId);
            dictFormWindowStatePrev.TryAdd(form, form.WindowState);

            foreach (KeyValuePair<string, Form> entry in dictSubForms)
            {
                root.DisplayDialog(entry.Value, requestedProcessId);
                dictFormWindowStatePrev.TryAdd(entry.Value, (entry.Value).WindowState);
            }
        }

        public void execute_action(Sinter sinter)
        {
            connection.SendMessage(sinter);
        }

        public void execute_kbd(Sinter sinter)
        {
            log.DebugFormat("kbd: " + sinter.HeaderNode.ParamsInfo);

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
            /*
            // not processing menu updates from MAC, maybe need later but need a seperate function
            // since structure from Mac is different from windows
            if (sinter.EntityNode.Type.Equals("menubar")) {
                // from Mac OSX, which sends the whole menus 
                // mac: menubarItem/menu/menuitem , we want menuitem instead of menu
                foreach (Entity child in sinter.EntityNode.Children) {
                    Entity grandchild = child.Children[0];
                    grandchild.Name = child.Name;
                    UpdateMenuItem(grandchild, null);
                }
                return;
            }
            else 
            */
            {
                /* from windows */
                Entity entity = sinter.EntityNode;
                if (sinter.EntityNode.Type.Equals("MenuItem")
                    && (sinter.EntityNode.Children.Count > 0))
                {
                    if (sinter.EntityNode.Children.Count > 1) log.DebugFormat("MenuItem has {0} children", sinter.EntityNode.Children.Count);
                    if (sinter.EntityNode.Children[0].Type.Equals("Menu"))
                    {
                        entity = sinter.EntityNode.Children[0];
                    }
                }
                UpdateMenuItem(entity, null);
            }
        }

        private bool FindSubMenuItem(string menuItemName, ToolStripMenuItem parentMenuItem, out ToolStripMenuItem targetMenuItem)
        {
            if (parentMenuItem.Text.Equals(menuItemName, StringComparison.Ordinal))
            {
                targetMenuItem = parentMenuItem;
                return true;
            }
            else
            {
                foreach (ToolStripMenuItem subMenuItem in parentMenuItem.DropDownItems)
                {
                    if (FindSubMenuItem(menuItemName, subMenuItem, out ToolStripMenuItem target))
                    {
                        targetMenuItem = target;
                        return true;
                    }
                }
            }
            targetMenuItem = null;
            return false;
        }

        private void UpdateMenuItem(Entity entity, Control ctrl)
        {
            /* mac : the entity is "menubaritem"/menu/menuitem (we cant use menu since the <name> of menu from osx is empty unlike windows)
            /* windows: the entity is type "menu"/menuitem */

            log.DebugFormat("entity.Name: {0}", entity.Name);

            if (ctrl == null)
            {
                if (menuHash.TryGetValue(entity.Name, out ctrl))
                {
                    if (!ctrl.IsHandleCreated)
                    {
                        log.Warn("handle not created yet");
                        return;
                    }
                }
                else
                {
                    log.Error("cannot find control");
                    return;
                }
            }

            ctrl.BeginInvoke((Action)(() =>
            {
                MenuStrip mStrip = (MenuStrip)ctrl;
                ToolStripMenuItem targetMenuItem = null;

                if (!menuHashMenuItem.TryGetValue(entity.Name, out targetMenuItem))
                {
                    log.Error("matched menu not found");
                    return;
                }

                if (targetMenuItem.HasDropDownItems)
                {
                    removeSubMenuItems(targetMenuItem);
                }

                log.DebugFormat("new Children Count for SubMenu: {0}", entity.Children.Count);
                foreach (Entity child in entity.Children)
                {
                    addSubMenuItem(child, mStrip, targetMenuItem);
                }

                foreach (ToolStripMenuItem mi in mStrip.Items)
                {
                    if (FindSubMenuItem(entity.Name, mi, out targetMenuItem))
                    {
                        log.DebugFormat("Found {0} in {1}, Show '{1}' Drop Down", entity.Name, mi.Name);
                        mi.DropDown.Show();
                        targetMenuItem.DropDown.Show();
                        break;
                    }
                }
            }));
        }


        private void addSubMenuItem(Entity entity_mi, MenuStrip ms, ToolStripMenuItem parent_mi)
        {
            /*
            if (this.RemoteProcessName.Contains("Calculator") && entity_mi.Name.Equals("About Calculator"))
            {
                log.DebugFormat("skip menu: {0}", entity_mi.Name); 
                return;
            }
            */

            ToolStripMenuItem newMenu = new ToolStripMenuItem(entity_mi.Name);
            menuHashEntity.TryGetValue(parent_mi.Text, out Entity parent);

            newMenu.Name = entity_mi.Name;
            if ((entity_mi.States & States.DISABLED) != 0)
            {
                newMenu.Enabled = false;
            }
            if ((entity_mi.States & States.CHECKED) != 0)
            {
                newMenu.Checked = true;
            }
            if ((entity_mi.States & States.INVISIBLE) != 0)
            {
                newMenu.Visible = false;
            }
            if ((entity_mi.States & States.FOCUSED) != 0)
            {
                newMenu.Select();
            }
            newMenu.ShortcutKeys = ParseShortcutKeys(entity_mi.Value);

            if (!newMenu.Name.Equals("separator", StringComparison.Ordinal) && parent_mi.DropDownItems.IndexOfKey(newMenu.Name) == -1)
            {
                log.DebugFormat("Adding menuitem: {0}", entity_mi.Name);
                parent_mi.DropDownItems.Add(newMenu);
                menuHash.TryAdd(newMenu.Name, (Control)ms);
                menuHashEntity.TryAdd(newMenu.Name, entity_mi);
                menuHashMenuItem.TryAdd(newMenu.Name, newMenu);
                menuItemParent.TryAdd(newMenu.Name, parent);

                if ((entity_mi.States & States.COLLAPSED) != 0 || (entity_mi.States & States.EXPANDED) != 0)
                {
                    ContextMenuStrip miDropDown = new ContextMenuStrip();
                    newMenu.DropDown = miDropDown;
                    miDropDown.Opening += new System.ComponentModel.CancelEventHandler(ContextMenu_Opening);
                    newMenu.DropDownOpening += Control_MenuItem_Expanded;
                    newMenu.DropDownClosed += Control_MenuItem_Collapsed;
                    newMenu.Click += Control_MenuItem_Expanded;
                }
                else
                {
                    newMenu.Click += Control_Click_MenuItem;
                }
            }
            else
            {
                log.DebugFormat("skip menu: {0}", entity_mi.Name);
            }
        }

        private void removeSubMenuItems(ToolStripMenuItem parentMenuItem)
        {
            log.DebugFormat("removeSubMenuItems in {0}", parentMenuItem.Name);
            foreach (ToolStripMenuItem mi in parentMenuItem.DropDownItems)
            {
                menuHash.TryRemove(mi.Name, out Control _);
                menuHashEntity.TryRemove(mi.Name, out Entity _);
                menuHashMenuItem.TryRemove(mi.Name, out _);
                menuItemParent.TryRemove(mi.Name, out _);
            }
            parentMenuItem.DropDownItems.Clear();
        }

        private Keys ParseShortcutKeys(string ShortcutString)
        {
            Keys ShortcutKeys = Keys.None;
            if (ShortcutString != null)
            {
                string[] keys = ShortcutString.Split(new Char[] { '+' });
                foreach (string s in keys)
                {
                    s.Trim();
                    log.DebugFormat("key = {0}", s);
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


        void AdjustSubTreeProperties(Entity entity)
        {
            if (hash.TryGetValue(entity.UniqueID, out Control control))
            {
                AdjustProperties(entity, ref control);
            }
            else if (entity.Name.Equals("Minimize") || entity.Name.Equals("AXMinimizeButton"))
            {
                if ((entity.States & States.DISABLED) == 0) //not disabled
                {
                    this.form.MinimizeBox = true;
                }
            }
            else if (entity.Name.Equals("Maximize") || entity.Name.Equals("AXZoomButton"))
            {
                if ((entity.States & States.DISABLED) == 0) //not disabled
                {
                    this.form.MaximizeBox = true;
                }
            }

            foreach (Entity child in entity.Children)
            {
                AdjustSubTreeProperties(child);
            }
        }

        void AdjustProperties(Entity entity, ref Control control)
        {
            control.Height = (int)(entity.Height * height_ratio);
            control.Width = (int)(entity.Width * width_ratio);

            control.Name = entity.Name;
            if (entity.Type == "Edit")
            {
                control.Text = entity.Value;
            }
            else
            {
                control.Text = entity.Name;
            }

            /* following are treak for windows 7 calculator */
            if (this.RemoteProcessName.Contains("Calculator") &&
                (control.Text.Equals("Memory") || control.Text.Equals("Running History")))
            {
                control.Text = "";
            }
            if (this.RemoteProcessName.Contains("Calculator") && entity.Type == "Text"
                && entity.Name == "" && entity.Value == "")
            {
                control.Visible = false;
            }

            control.Top = (int)((entity.Top - rootPoint.Y) * height_ratio);
            control.Left = (int)((entity.Left - rootPoint.X) * width_ratio);
            //log.DebugFormat("AdjustProperties {0}/{1}/{2}, Top: {3}", entity.Type, entity.Name, entity.UniqueID, control.Top);

            if ((entity.States & States.DISABLED) != 0)
            {
                log.DebugFormat("{0}/{1} states is not enabled", entity.Type, entity.Name);
                control.Enabled = false;
            }
            else
            {
                control.Enabled = true;
            }

            if (entity.Type == "CheckBox")
            {
                log.DebugFormat("{0}/{1} states is CHECKED", entity.Type, entity.Name);
                if ((entity.States & States.CHECKED) != 0)
                {
                    ((CheckBox)control).Checked = true;
                }
                else
                {
                    ((CheckBox)control).Checked = false;
                }
            }

            if (entity.Type == "RadioButton")
            {
                log.DebugFormat("{0}/{1} states is SELECTED", entity.Type, entity.Name);
                if ((entity.States & States.SELECTED) != 0)
                {
                    ((RadioButton)control).Checked = true;
                }
                else
                {
                    ((RadioButton)control).Checked = false;
                }
            }

            if ((entity.States & States.INVISIBLE) != 0)
            {
                log.DebugFormat("{0}/{1} states is INVISIBLE", entity.Type, entity.Name);
                control.Visible = false;
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
