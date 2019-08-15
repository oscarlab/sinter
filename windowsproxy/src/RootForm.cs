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
using System.Windows.Forms;
using System.Collections.Concurrent;
using System.Xml;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.IO;

using Sintering;

namespace WindowsProxy {

  public partial class RootForm : Form {

    ConcurrentDictionary<int , object> form_table;
    string server_ip;
    int server_port;

    TcpClient client = null;
    ClientHandler client_handle;
    WindowsProxy proxy;
    SslStream sslStream;
    public String passcode;
    const SslPolicyErrors acceptedSslPolicyErrors = SslPolicyErrors.RemoteCertificateNameMismatch // we do not check server host name
                                                  | SslPolicyErrors.RemoteCertificateChainErrors; // for test certificate we ignore chain error
        private static log4net.ILog log = log4net.LogManager.GetLogger("Proxy");

    // constructor
    public RootForm() {

            /* store log to tmp folder: C:\Users\UserName\AppData\Local\Temp\  */
            log4net.GlobalContext.Properties["LogFileName"] = Path.GetTempPath() + @"\sinterproxy.log"; //log file path
            log4net.GlobalContext.Properties["XMLFileName"] = Path.GetTempPath() + @"\sinterproxy.xml"; //xml log file path

      InitializeComponent();
      form_table = new ConcurrentDictionary<int , object>();
    }

    public void DisplayDialog(dynamic form , int pid) {
      if (InvokeRequired) {
        BeginInvoke(new MethodInvoker(() => this.DisplayDialog(form , pid)));
        return;
      }

      try {
                form_table.TryAdd(pid, form);
                form.ShowDialog();
      }
      catch (Exception e) {
        log.Error(e.ToString());
      }
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
        log.Error(e.ToString());
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
        log.Error(e.ToString());
      }
    }


    public void ShowConnected()
    {
      /* Before rendering check whether we are in the UI thread */
      if (InvokeRequired)
      {
        BeginInvoke(new MethodInvoker(() => ShowConnected()));
        return;
      }

      try
      {
        this.ls_button.Visible = true;
        this.remoteProcessesView.Visible = false;
        this.connect_button.Enabled = false;
        this.disconnect_button.Enabled = true;
        this.textBoxPasscode.Enabled = false;
        this.textBoxIP.Enabled = false;
        this.textBoxPort.Enabled = false;
        
      }
      catch (Exception e)
      {
        log.Error(e.ToString());
      }
    }

    // The following method is invoked by the RemoteCertificateValidationDelegate.
    public static bool ValidateServerCertificate(
          object sender,
          X509Certificate certificate,
          X509Chain chain,
          SslPolicyErrors sslPolicyErrors)
    {

      log.InfoFormat("SSL Certificate validate results: {0}({1})", (int)sslPolicyErrors, sslPolicyErrors);
      log.InfoFormat("SSL acceptedSslPolicyErrors:      {0}", acceptedSslPolicyErrors);

      if ((sslPolicyErrors &(~acceptedSslPolicyErrors)) == 0)
        return true;

      // Do not allow this client to communicate with unauthenticated servers.
      return false;
    }

    private void Connect(object sender , EventArgs e) {
      // connect
      try
      {
        server_ip = this.textBoxIP.Text;
        server_port = int.Parse(textBoxPort.Text);
        log.InfoFormat("connecting to server {0}:{1}", server_ip, server_port);
                if(client != null)
                {
                    sslStream.Close();
                    client.Close();
                }
        client = new TcpClient(server_ip, server_port);
      }
      catch (SocketException ex)
      {
        log.Error("SocketException: {0}", ex);
        MessageBox.Show("Not able to reach sinter server");
        return;
      }

      if (client != null) {
        proxy = new WindowsProxy(this);
        

        /* implements SSL */
        sslStream = new SslStream(
                client.GetStream(),
                false,
                new RemoteCertificateValidationCallback(ValidateServerCertificate),
                null
                );
        try
        {
          sslStream.AuthenticateAsClient(@"SinterServer"); 
        }
        catch (AuthenticationException ee)
        {
          log.ErrorFormat("SSL AuthenticationException: {0}", ee.Message);
          if (ee.InnerException != null)
          {
            log.ErrorFormat("SSL Inner exception: {0}", ee.InnerException.Message);
          }
          log.Error("SSL Authentication failed - closing the connection.");
          client.Close();
          return;
        }
        catch (InvalidOperationException ee)
        {
          log.ErrorFormat("InvalidOperationException: {0}", ee.Message);
          if (ee.InnerException != null)
          {
            log.ErrorFormat("InvalidOperationException Inner exception: {0}", ee.InnerException.Message);
          }
          log.Error("closing the connection.");
          client.Close();
          return;
        }
        log.Info("SSL Authentication successful!!");

        //client_handle = new ClientHandler(proxy, client, "WinProxy Client");
        client_handle = new ClientHandler(proxy, client, "WinProxy Client", sslStream);

        passcode = this.textBoxPasscode.Text;
        proxy.execute_verify_passcode_req(null);
      }
    }

    private void FetchRemoteProcesses(object sender, EventArgs e)
    {
      proxy.execute_ls_req(null);
      this.remoteProcessesView.Visible = true;
    }

    private void LoadRemoteProcess(object sender , DataGridViewCellEventArgs e) {
      DataGridView grid = sender as DataGridView;
      Entity entity = (Entity)grid.SelectedRows[0].Tag;

      proxy.RequestedProcessId = int.Parse(entity.Process);
      proxy.RemoteProcessName = (string)grid.SelectedRows[0].Cells[0].Value;
      proxy.execute_ls_l_req(null);
    }

    private void RootForm_Load(object sender , EventArgs e) {
      using (XmlReader reader = new XmlTextReader("config.xml"))
      {
        reader.ReadToFollowing("server");
        reader.MoveToFirstAttribute();
        server_ip = reader.Value;
        this.textBoxIP.Text = server_ip.ToString();
        reader.MoveToNextAttribute();
        server_port = int.Parse(reader.Value);
        this.textBoxPort.Text = server_port.ToString();
      }
    }

    public ConcurrentDictionary<int , object> Form_table {
      get { return form_table; }
    }

    public void Remove_Dict_Item(int key) {
      form_table.TryRemove(key, out object dummy);
    }

    public void Disconnect(object sender, EventArgs e)
    {
      /* Before rendering check whether we are in the UI thread */
      if (InvokeRequired)
      {
        BeginInvoke(new MethodInvoker(() => Disconnect(sender, e)));
        return;
      }

      try
      {
        if (client_handle != null)
          client_handle.StopHandling();

        this.ls_button.Visible = false;
        this.remoteProcessesView.Visible = false;
        this.connect_button.Enabled = true;
        this.disconnect_button.Enabled = false;
        this.textBoxPasscode.Enabled = true;
        this.textBoxIP.Enabled = true;
        this.textBoxPort.Enabled = true;

        this.proxy.CloseAllForms();
      }
      catch (Exception ex)
      {
        log.Error(ex.ToString());
      }
    }

  }
}
