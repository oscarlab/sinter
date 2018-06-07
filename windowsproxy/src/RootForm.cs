using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Collections.Concurrent;
using System.Xml;
using System.Net.Sockets;
using Sintering;

namespace WindowsProxy {
  public partial class RootForm : Form {

    ConcurrentDictionary<int , object> form_table;

    // constructor
    public RootForm() {
      InitializeComponent();
      form_table = new ConcurrentDictionary<int , object>();
    }

    public void DisplayProxy(dynamic form , int pid) {
      // Before rendering check whether we are in the UI thread 
      if (InvokeRequired) {
        BeginInvoke(new MethodInvoker(() => this.DisplayProxy(form , pid)));
        return;
      }

      try {
        form_table.TryAdd(pid , form);
        form.Show();
      }
      catch (Exception e) {
        Console.WriteLine(e.ToString());
      }
    }

    public void PopulateGridView(List<Entity> new_processes) {
      /* Before rendering check whether we are in the UI thread */
      if (InvokeRequired) {
        BeginInvoke(new MethodInvoker(() => PopulateGridView(new_processes)));
        return;
      }

      try {
        remoteProcessesView.Rows.Clear();
        remoteProcessesView.Refresh();
        foreach (Entity proc in new_processes) {
          DataGridViewRow row = (DataGridViewRow)remoteProcessesView.Rows[0].Clone();
          row.Tag = proc;
          row.Cells [0].Value = proc.Name;
          remoteProcessesView.Rows.Add(row);
        }
      }
      catch (Exception e) {
        Console.WriteLine(e.ToString());
      }
    }

    string server_ip;
    int server_port;

    TcpClient client;
    ClientHandler client_handle;
    WindowsProxy proxy;

    private void Connect(object sender , EventArgs e) {
      // connect
      client = new TcpClient(server_ip , server_port);

      if (client != null) {
        proxy = new WindowsProxy(this);
        client_handle = new ClientHandler(proxy, client , "WinProxy Client");

        // hide some controls
        ls_button.Visible = true;
        remoteProcessesView.Visible = true;
      }
    }

    private void FetchRemoteProcesses(object sender, EventArgs e)
    {
      proxy.execute_ls_req(null);
    }

    private void LoadRemoteProcess(object sender , DataGridViewCellEventArgs e) {
      DataGridView grid = sender as DataGridView;
      Entity entity = (Entity)grid.SelectedRows[0].Tag;

      proxy.RequestedProcessId = int.Parse(entity.Process);
      proxy.execute_ls_l_req(null);
    }

    private void RootForm_Load(object sender , EventArgs e) {
      using (XmlReader reader = new XmlTextReader("config.xml"))
      {
        reader.ReadToFollowing("server");
        reader.MoveToFirstAttribute();
        server_ip = reader.Value;
        reader.MoveToNextAttribute();
        server_port = int.Parse(reader.Value);
      }
    }

    public ConcurrentDictionary<int , object> Form_table {
      get { return form_table; }
    }

    public void Remove_Dict_Item(int key) {
      form_table.TryRemove(key, out object dummy);
    }

    private void Disconnect(object sender, EventArgs e)
    {
      if (client_handle != null)
        client_handle.StopHandling();

      ls_button.Visible = false;
      remoteProcessesView.Visible = false;
    }
  }
}
