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

        #region Main Renderer
        string invoking_method_name;
        MethodInfo invoking_method;

        Control Render(Entity entity, Control parent_control = null)
        {
            Console.WriteLine("current type {0}", entity.Type);
            Console.WriteLine("current name {0}", entity.Name);

            entity.Type = entity.Type.First().ToString().ToUpper() + entity.Type.Substring(1); //turn "window" to "Window"
            invoking_method_name = string.Format("Render{0}", entity.Type);
            invoking_method = type.GetMethod(invoking_method_name,
              BindingFlags.NonPublic | BindingFlags.Instance);
            Console.WriteLine("Method {0}", invoking_method);
            Control current_control = null;
            if (invoking_method != null)
            {
                current_control = (Control)invoking_method.Invoke(this,
                  new object[] { entity, parent_control });

                Console.WriteLine("Is current control null? {0}", current_control);
               if (current_control == null)
                    return null;

                if (parent_control != null && parent_control != current_control)
                {
                    parent_control.Controls.Add(current_control);

                    // add to hash
                    hash.TryAdd(entity.UniqueID, current_control);
                }
            }

            // check children
            Console.WriteLine("{1} HasChildren? {0}", entity.Name, HasChildren(entity));
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
            if(hash.TryGetValue(entity.UniqueID, out Control res))
            {
                Console.WriteLine("Remove");
                parent.Controls.Remove(res);
                hash.TryRemove(entity.UniqueID, out Control rem);
            }

            if (entity.Name.Equals("Maximize") || entity.Name.Equals("Minimize") || entity.Name.Equals("Close"))
            {
                return parent;
            }
            Control control = new Button();
            AdjustProperties(entity, ref control);
            Console.WriteLine("Executing RenderButton");

            control.Tag = entity;
            control.Click += Control_Click;

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
            return parent;
        }

        private Control RenderComboBox(Entity entity, Control parent)
        {
            Console.WriteLine("Rendering ComboBox");
            Control control = new ComboBox();
            AdjustProperties(entity, ref control);

            control.Tag = entity;

            foreach (Entity child_entity in entity.Children)
            {
                Console.WriteLine("{0} {1}", child_entity.Name, child_entity.Type);
                if(child_entity.Type == "List")
                {
                    foreach(Entity listItem in child_entity.Children)
                    {
                        ToolStripMenuItem tmp = new ToolStripMenuItem();
                        tmp.Name = listItem.Name;
                        tmp.Text = listItem.Name;
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
              foreach ( Word w in words) {
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
              return control;
        }

        private Control RenderEdit(Entity entity, Control parent)
        {
            return parent;
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
            return parent;
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
            Console.WriteLine("{0}", entity.Type);

            if (entity.Name == "System Menu Bar")
                return parent;

            Control control = new MenuStrip();
            AdjustProperties(entity, ref control);
            control.Tag = entity;
            // control.Click += Control_Click;

            ToolStripMenuItem child;
            foreach (Entity child_entity in entity.Children)
            {
                Console.WriteLine("{0}", child_entity.Name);
                child = new ToolStripMenuItem(child_entity.Name);
                child.Name = child_entity.Name;
                child.Click += Control_Expand_MenuItem;
                Console.WriteLine("{0}", child_entity);
                menuHashEntity.TryAdd(child_entity.Name, child_entity);
                ((MenuStrip)control).Items.Add(child);
            }

            foreach (Entity child_entity in entity.Children)
            {
                menuHash.TryAdd(child_entity.Name, control);
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
            return null;
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

            //register event listener and delegate
            form.FormClosing += Form_Closed;
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
        private void Control_Click(object sender, EventArgs e)
        {
            if (isSent)
                return;

            Button button = (Button)sender;
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

        private void Control_Expand_MenuItem(object sender, EventArgs e)
        {
            if (isSent)
                return;

            ToolStripMenuItem item = (ToolStripMenuItem)sender;

            // Entity entity;
            Console.WriteLine("MenuItem Click {0}", item.Text);
            menuHashEntity.TryGetValue(item.Text, out Entity entity);
            menuItemParent.TryGetValue(item.Text, out Entity parentEntity);
            Console.WriteLine("{0}", entity);

            if (entity == null)
                return;
            Console.WriteLine("MenuItem ID {0}", entity.UniqueID);

            // string[] ids = { entity.UniqueID, parentEntity.UniqueID };
            Sinter sinter = new Sinter
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
            if (isSent)
                return;

            ToolStripMenuItem item = (ToolStripMenuItem)sender;
            // Entity entity;
            Console.WriteLine("MenuItem Click {0}", item.Text);
            menuHashEntity.TryGetValue(item.Text, out Entity entity);
            if (entity == null)
                return;
            Console.WriteLine("MenuItem ID {0}", entity.UniqueID);
            List<string[]> lists = new List<string[]>();
            string name = item.Name;

            string id = null;
            menuItemParent.TryGetValue(name, out Entity test);
            Console.WriteLine("{0}", test.UniqueID);

            Point start = GetCenter(entity);
            Console.WriteLine("Adding {0} to list...", entity.Name);
            lists.Add(new string[] { start.X.ToString(), start.Y.ToString() });

            while (menuItemParent.TryGetValue(name, out Entity parent))
            {
                if(!menuItemParent.TryGetValue(parent.Name, out Entity stop))
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
            if (this.form != null)
            {
                this.form.Invoke(new Action(() => this.form.Close()));
            }
            this.form = null;
        }

        private void Form_Closed(object sender, EventArgs e)
        {
            Console.WriteLine("Form closed\n");
            this.form = null;
            root.Remove_Dict_Item(requestedProcessId);
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
                    }));
                }
            }
            else if (subCode == serviceCodes["delta_prop_change_value"])
            {
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
                            control.Text = newValue;
                        }));
                    }
                }
            }
            else if (subCode == serviceCodes["delta_subtree_expand"])
            {
                //if(sinter.EntityNode.Type.Equals("" + winControls["MenuItem"]))
                //   UpdateMenu(sinter);
            }
            else if (subCode == serviceCodes["delta_subtree_menu"])
            {
                UpdateMenu(sinter);
            }
            else if (subCode == serviceCodes["delta_subtree_add"])
            {
                // Not Implemented
            }
            else if (subCode == serviceCodes["delta_subtree_remove"])
            {
                // Not Implemented
            }
            else if (subCode == serviceCodes["delta_subtree_replace"])
            {
                if (sinter.EntityNode == null)
                    return;

                Console.WriteLine("Type: {0}", sinter.EntityNode.Type);
                if (sinter.EntityNode.Type.Equals("Menu") || sinter.EntityNode.Type.Equals("MenuItem"))
                {
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

                ((Control) form).BeginInvoke((Action)(() =>
                {
                    Console.WriteLine("All ctrl elements");
                    Control[] arr = new Control[form.Controls.Count];

                    form.Controls.CopyTo(arr, 0);

                    foreach(Control element in arr)
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
            if(form != null)
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
            // Entity e = sinter.EntityNode;

            //server send this msg to indicate app is closed in server side
            if (this.form != null)
            {
              this.form.Invoke(new Action(() => this.form.Close()));
            }
            this.form = null;

            //re-load the list from server
            this.execute_ls_req(null);          
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

            if (form == null)
                form = (AppForm)Render(sinter.EntityNode, null);

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
        private void UpdateMenu(Sinter sinter)
        {
            if (menuHash.TryGetValue(sinter.EntityNode.Name, out Control ctrl))
            {
                ctrl.BeginInvoke((Action)(() =>
                {
                    MenuStrip mStrip = (MenuStrip)ctrl;

                    if (menuHashMenuItem.TryGetValue(sinter.EntityNode.Name, out ToolStripMenuItem subMI))
                    {
                        /* menuHashEntity.TryGetValue(sinter.EntityNode.Name, out Entity parent);
                         foreach (Entity child in sinter.EntityNode.Children)
                         {
                             Console.WriteLine("{0}", child.ChildCount);
                             ToolStripMenuItem tmpItem = new ToolStripMenuItem(child.Name);
                             tmpItem.Name = child.Name;
                             tmpItem.Click += Control_Click_MenuItem;
                             Console.WriteLine("{0}", child.Name);
                             Console.WriteLine("{0}", subMI.DropDownItems.IndexOfKey(tmpItem.Name));
                             if (!tmpItem.Name.Equals("separator", StringComparison.Ordinal) && subMI.DropDownItems.IndexOfKey(tmpItem.Name) == -1)
                             {
                                 Console.WriteLine("Adding in first case...");
                                 menuHash.TryAdd(tmpItem.Name, ctrl);
                                 menuHashEntity.TryAdd(tmpItem.Name, child);
                                 menuHashMenuItem.TryAdd(tmpItem.Name, tmpItem);
                                 menuItemParent.TryAdd(tmpItem.Name, parent);
                                 subMI.DropDownItems.Add(tmpItem);
                             }

                         }*/
                        Console.WriteLine("Has Items");
                        if(subMI.HasDropDownItems)
                        {
                            Console.WriteLine("Show Drop Down");
                            subMI.ShowDropDown();
                        }

                    }
                    else
                    {
                        foreach (ToolStripMenuItem mi in mStrip.Items)
                        {
                            Console.WriteLine("{0}", sinter.EntityNode.Name);
                            Console.WriteLine("{0}", mi.Text);
                            if (mi.Text.Equals(sinter.EntityNode.Name, StringComparison.Ordinal))
                            {
                                menuHashEntity.TryGetValue(mi.Text, out Entity parent);
                                foreach (Entity child in sinter.EntityNode.Children)
                                {
                                    Console.WriteLine("{0}", child.ChildCount);
                                    ToolStripMenuItem tmpItem = new ToolStripMenuItem(child.Name);
                                    tmpItem.Name = child.Name;
                                    tmpItem.Click += Control_Click_MenuItem;
                                    Console.WriteLine("{0}", child.Name);
                                    Console.WriteLine("{0}", mi.DropDownItems.IndexOfKey(tmpItem.Name));
                                    if (!tmpItem.Name.Equals("separator", StringComparison.Ordinal) && mi.DropDownItems.IndexOfKey(tmpItem.Name) == -1)
                                    {
                                        Console.WriteLine("Adding in second case...");
                                        menuHash.TryAdd(tmpItem.Name, ctrl);
                                        menuHashEntity.TryAdd(tmpItem.Name, child);
                                        menuHashMenuItem.TryAdd(tmpItem.Name, tmpItem);
                                        menuItemParent.TryAdd(tmpItem.Name, parent);
                                        mi.DropDownItems.Add(tmpItem);
                                    }

                                }
                                menuHashMenuItem.TryAdd(sinter.EntityNode.Name, mi);
                                mi.ShowDropDown();
                                break;
                            }
                        }
                    }

                    Console.WriteLine("entity valid");
                }));
            }
        }

        private Point GetCenter(Entity entity)
        {
            int x = entity.Left + (entity.Width) / 2;
            int y = entity.Top + (entity.Height) / 2;
            return new Point(x, y);
        }

        void AdjustProperties(Entity entity, ref Control control)
        {
            control.Height = (int)(entity.Height * height_ratio);
            control.Width = (int)(entity.Width * width_ratio);

            control.Name = entity.Name;
            control.Text = entity.Name;

            control.Top = (int)((entity.Top - rootPoint.Y) * height_ratio);
            control.Left = (int)((entity.Left - rootPoint.X) * width_ratio);

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
