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
using System.Collections.Concurrent;

namespace WindowsProxy
{

  class TagInfo {
    String _id;
    Point _xy;

    public string Id {
      get { return _id; }
      set { _id = value; }
    }

    public Point XY {
      get { return _xy; }
      set { _xy = value; }
    }
    public TagInfo(string id , Point xy) {
      _id = id;
      _xy = xy;
    }
  };

  // keypress delegate
  public delegate void DelegateKeyPresses(string keys);

  public class WindowsProxy : IWinCommands {
    RootForm root;
    AppForm form;

    Point rootPoint;
    Entity root_entity;

    static Dictionary<string, object> winControls;
    Type type; 

    float height_ratio;
    float width_ratio;

    Dictionary<string, int> serviceCodes;
    Dictionary<int, string> serviceCodesRev;

    Timer timer;
    ConcurrentDictionary<string, Control> hash = new ConcurrentDictionary<string, Control>();

    public WindowsProxy(RootForm r) { // Form
      root = r;
      winControls = Config.getConfig("control_types_windows");
      
      // loading the service_code dictionary
      Dictionary<string, object> serviceCodesTemp = Config.getConfig("service_code");

      hash = new ConcurrentDictionary<string, Control>();

      if (serviceCodesTemp != null)
      {
        serviceCodes = serviceCodesTemp.ToDictionary(pair => pair.Key, pair => (int)pair.Value);
        serviceCodesRev = serviceCodes.ToDictionary(pair => pair.Value, pair => pair.Key);
      }

      type = GetType();

      // timer to avoid sending mouse click multiple times
      timer = new Timer
      {
        Interval = 50,
        Enabled = false,
      };
      timer.Tick += Timer_Tick;
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

      invoking_method_name = string.Format("Render{0}", entity.Type);
      invoking_method = type.GetMethod(invoking_method_name,
        BindingFlags.NonPublic | BindingFlags.Instance);

      Control current_control = null;
      if (invoking_method != null)
      {
        current_control = (Control)invoking_method.Invoke(this,
          new object[] { entity, parent_control });

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
      if (!HasChildren(entity))
        return current_control;

      foreach (Entity child_entity in entity.Children)
        Render(child_entity, current_control);

      return current_control;
    }
    #endregion

    #region Invividual Render Methods
    private Control RenderButton(Entity entity, Control parent) {
      Control control = new Button();
      AdjustProperties(entity, ref control);

      control.Tag = entity;
      control.Click += Control_Click;

      return control;
    }

    private Control RenderCalendar(Entity entity, Control parent) {
      return parent;
    }

    private Control RenderCheckBox(Entity entity, Control parent) {
      return parent;
    }

    private Control RenderComboBox(Entity entity, Control parent) {
      return parent;
    }

    private Control RenderDataGrid(Entity entity, Control parent) {
      return parent;
    }

    private Control RenderDataItem(Entity entity, Control parent) {
      return parent;
    }

    private Control RenderDocument(Entity entity, Control parent) {
      return parent;
    }

    private Control RenderEdit(Entity entity, Control parent) {
      return parent;
    }

    private Control RenderGroup(Entity entity, Control parent) {
      return parent;
    }

    private Control RenderHeader(Entity entity, Control parent) {
      return parent;
    }

    private Control RenderHeaderItem(Entity entity, Control parent) {
      return parent;
    }

    private Control RenderHyperlink(Entity entity, Control parent) {
      return parent;
    }

    private Control RenderImage(Entity entity, Control parent) {
      return parent;
    }

    private Control Renderlist(Entity entity, Control parent) {
      return parent;
    }

    private Control RenderListItem(Entity entity, Control parent) {
      return parent;
    }

    private Control RenderMenu(Entity entity, Control parent) {
      return parent;
    }

    private Control RenderMenuBar(Entity entity, Control parent) {
      return parent;
    }

    private Control RenderMenuItem(Entity entity, Control parent) {
      return parent;
    }

    private Control RenderPane(Entity entity, Control parent) {
      return parent;
    }

    private Control RenderProgressBar(Entity entity, Control parent) {
      return parent;
    }

    private Control RenderRadioButton(Entity entity, Control parent) {
      return parent;
    }

    private Control RenderScrollBar(Entity entity, Control parent) {
      return parent;
    }

    private Control RenderSeparator(Entity entity, Control parent) {
      return parent;
    }

    private Control RenderSlider(Entity entity, Control parent) {
      return parent;
    }

    private Control RenderSpinner(Entity entity, Control parent) {
      return parent;
    }

    private Control RenderSplitButton(Entity entity, Control parent) {
      return parent;
    }

    private Control RenderStatusBar(Entity entity, Control parent) {
      return parent;
    }

    private Control RenderTab(Entity entity, Control parent) {
      return parent;
    }

    private Control RenderTabItem(Entity entity, Control parent) {
      return parent;
    }

    private Control RenderTable(Entity entity, Control parent) {
      return parent;
    }

    private Control RenderText(Entity entity, Control parent) {
      Control control = new TextBox();

      AdjustProperties(entity, ref control);

      control.Tag = entity;
      control.KeyPress += Control_KeyPress; 
      return control;
    }    

    private Control RenderThumb(Entity entity, Control parent) {
      return parent;
    }

    private Control RenderTitleBar(Entity entity, Control parent) {
      return parent;
    }

    private Control RenderToolBar(Entity entity, Control parent) {
      return parent;
    }

    private Control RenderToolTip(Entity entity, Control parent) {
      return parent;
    }

    private Control RenderTree(Entity entity, Control parent) {
      Control control = parent;
      TreeNode tree = new TreeNode(); // addRow(child)
      return control;
    }

    private Control RenderTreeItem(Entity entity, Control parent) {
      return parent;
    }

    private Control RenderUnknown(Entity entity, Control parent) {
      return parent;
    }

    private Control RenderWindow(Entity entity, Control parent) {
      // assign class variables
      root_entity = entity;
      rootPoint.X = root_entity.Left;
      rootPoint.Y = root_entity.Top;

      // make a form
      form = new AppForm();
      Control control = form as Control;

      // manual adjustment
      AdjustProperties(root_entity, ref control);

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

    private void Control_KeyPress(object sender, KeyPressEventArgs e)
    {
      TextBox textBox = sender as TextBox;
      Entity entity = (Entity)textBox.Tag;

      if (entity != null) {
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

    public void ProcessKeyPress(string keypresses) {
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
    public ConnectionHandler connection { get; set;}

    public void execute_stop_scraping() {

    }

    public void execute_ls_req(Sinter _) {
      Sinter sinter = new Sinter()
      {
        HeaderNode = MsgUtil.BuildHeader(serviceCodes["ls_req"]),
      };

      connection.SendMessage(sinter);
    }

    public void execute_ls_l_req(Sinter _) {
      Sinter sinter = new Sinter()
      {
        HeaderNode = MsgUtil.BuildHeader(serviceCodes["ls_l_req"]),
      };

      connection.SendMessage(sinter);
    }

    public void execute_delta(Sinter sinter) {
      int subCode = sinter.HeaderNode.SubCode;
      string targetId = sinter.HeaderNode.ParamsInfo.TargetId;

      if (subCode == serviceCodes["delta_prop_change_name"]) {
        if (hash.TryGetValue(targetId, out Control control)) {
          string newName = sinter.HeaderNode.ParamsInfo.Data2;

          control.BeginInvoke((Action)(() =>
          {
            control.Name = newName;
          }));
        }
      }
      else if (subCode == serviceCodes["delta_prop_change_value"])
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

    public void execute_event(Sinter sinter) {

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
    public void execute_ls_res(Sinter sinter) {
      List<Entity> new_processes = sinter.EntityNodes;
      root.PopulateGridView(new_processes);
    }

    int width, height;
    public void execute_ls_l_res(Sinter sinter) {
      if (sinter.EntityNode == null)
        return;

      width = int.Parse(sinter.HeaderNode.ParamsInfo.Data1);
      height = int.Parse(sinter.HeaderNode.ParamsInfo.Data2);

      // remote/local ratio
      height_ratio = (float)System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height / height;
      width_ratio = (float)System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width / width;

      if(form == null)
        form = (AppForm)Render(sinter.EntityNode, null);

      //now show it
      root.DisplayProxy(form, requestedProcessId);
    }

    public void execute_action(Sinter sinter) {

    }

    public void execute_kbd(Sinter sinter) {
      Console.WriteLine("kbd: " + sinter.HeaderNode.ParamsInfo);

      //connection.SendMessage(sinter);
    }

    public void execute_mouse(Sinter sinter) {
      connection.SendMessage(sinter);
    }

    public void execute_listener(Sinter sinter) {

    }

    #endregion

    #region Utility Methods

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
