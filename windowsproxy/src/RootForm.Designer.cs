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
using System.Windows.Forms;
using System.Drawing;

namespace WindowsProxy
{
    partial class RootForm
    {
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
      this.ls_button = new System.Windows.Forms.Button();
      this.remoteProcessesView = new System.Windows.Forms.DataGridView();
      this.Process = new System.Windows.Forms.DataGridViewTextBoxColumn();
      this.menuStrip1 = new System.Windows.Forms.MenuStrip();
      this.connect_button = new System.Windows.Forms.Button();
      this.disconnect_button = new System.Windows.Forms.Button();
      this.textBoxPasscode = new System.Windows.Forms.TextBox();
      this.passcode_label = new System.Windows.Forms.Label();
      this.label1 = new System.Windows.Forms.Label();
      this.textBoxIP = new System.Windows.Forms.TextBox();
      this.label2 = new System.Windows.Forms.Label();
      this.textBoxPort = new System.Windows.Forms.TextBox();
      ((System.ComponentModel.ISupportInitialize)(this.remoteProcessesView)).BeginInit();
      this.SuspendLayout();
      // 
      // ls_button
      // 
      this.ls_button.ImageAlign = System.Drawing.ContentAlignment.TopRight;
      this.ls_button.Location = new System.Drawing.Point(28, 210);
      this.ls_button.Name = "ls_button";
      this.ls_button.Size = new System.Drawing.Size(104, 46);
      this.ls_button.TabIndex = 8;
      this.ls_button.Text = "Show Remote Applications";
      this.ls_button.UseVisualStyleBackColor = true;
      this.ls_button.Visible = false;
      this.ls_button.Click += new System.EventHandler(this.FetchRemoteProcesses);
      // 
      // remoteProcessesView
      // 
      this.remoteProcessesView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
      this.remoteProcessesView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Process});
      this.remoteProcessesView.Location = new System.Drawing.Point(156, 35);
      this.remoteProcessesView.Name = "remoteProcessesView";
      this.remoteProcessesView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
      this.remoteProcessesView.Size = new System.Drawing.Size(343, 266);
      this.remoteProcessesView.TabIndex = 9;
      this.remoteProcessesView.Visible = false;
      this.remoteProcessesView.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.LoadRemoteProcess);
      // 
      // Process
      // 
      this.Process.HeaderText = "Process Name";
      this.Process.Name = "Process";
      this.Process.ReadOnly = true;
      this.Process.Width = 300;
      // 
      // menuStrip1
      // 
      this.menuStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
      this.menuStrip1.Location = new System.Drawing.Point(0, 0);
      this.menuStrip1.Name = "menuStrip1";
      this.menuStrip1.Padding = new System.Windows.Forms.Padding(7, 1, 0, 1);
      this.menuStrip1.Size = new System.Drawing.Size(537, 24);
      this.menuStrip1.TabIndex = 10;
      this.menuStrip1.Text = "menuStrip1";
      // 
      // connect_button
      // 
      this.connect_button.Location = new System.Drawing.Point(28, 35);
      this.connect_button.Name = "connect_button";
      this.connect_button.Size = new System.Drawing.Size(104, 31);
      this.connect_button.TabIndex = 11;
      this.connect_button.Text = "Connect";
      this.connect_button.UseVisualStyleBackColor = true;
      this.connect_button.Click += new System.EventHandler(this.Connect);
      // 
      // disconnect_button
      // 
      this.disconnect_button.Enabled = false;
      this.disconnect_button.Location = new System.Drawing.Point(28, 271);
      this.disconnect_button.Name = "disconnect_button";
      this.disconnect_button.Size = new System.Drawing.Size(104, 30);
      this.disconnect_button.TabIndex = 12;
      this.disconnect_button.Text = "Disconnect";
      this.disconnect_button.UseVisualStyleBackColor = true;
      this.disconnect_button.Click += new System.EventHandler(this.Disconnect);
      // 
      // textBoxPasscode
      // 
      this.textBoxPasscode.Location = new System.Drawing.Point(82, 129);
      this.textBoxPasscode.Name = "textBoxPasscode";
      this.textBoxPasscode.Size = new System.Drawing.Size(50, 23);
      this.textBoxPasscode.TabIndex = 13;
      this.textBoxPasscode.Text = "123456";
      // 
      // passcode_label
      // 
      this.passcode_label.AutoSize = true;
      this.passcode_label.Location = new System.Drawing.Point(25, 132);
      this.passcode_label.Name = "passcode_label";
      this.passcode_label.Size = new System.Drawing.Size(59, 15);
      this.passcode_label.TabIndex = 14;
      this.passcode_label.Text = "Passcode:";
      // 
      // label1
      // 
      this.label1.AutoSize = true;
      this.label1.Location = new System.Drawing.Point(25, 69);
      this.label1.Name = "label1";
      this.label1.Size = new System.Drawing.Size(55, 15);
      this.label1.TabIndex = 16;
      this.label1.Text = "Server IP:";
      // 
      // textBoxIP
      // 
      this.textBoxIP.Location = new System.Drawing.Point(34, 84);
      this.textBoxIP.Name = "textBoxIP";
      this.textBoxIP.Size = new System.Drawing.Size(98, 23);
      this.textBoxIP.TabIndex = 15;
      // 
      // label2
      // 
      this.label2.AutoSize = true;
      this.label2.Location = new System.Drawing.Point(25, 109);
      this.label2.Name = "label2";
      this.label2.Size = new System.Drawing.Size(67, 15);
      this.label2.TabIndex = 18;
      this.label2.Text = "Server Port:";
      // 
      // textBoxPort
      // 
      this.textBoxPort.Location = new System.Drawing.Point(95, 106);
      this.textBoxPort.Name = "textBoxPort";
      this.textBoxPort.Size = new System.Drawing.Size(37, 23);
      this.textBoxPort.TabIndex = 17;
      // 
      // RootForm
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.ClientSize = new System.Drawing.Size(537, 335);
      this.Controls.Add(this.label2);
      this.Controls.Add(this.textBoxPort);
      this.Controls.Add(this.label1);
      this.Controls.Add(this.textBoxIP);
      this.Controls.Add(this.passcode_label);
      this.Controls.Add(this.textBoxPasscode);
      this.Controls.Add(this.disconnect_button);
      this.Controls.Add(this.connect_button);
      this.Controls.Add(this.remoteProcessesView);
      this.Controls.Add(this.ls_button);
      this.Controls.Add(this.menuStrip1);
      this.Font = new System.Drawing.Font("Segoe UI", 9F);
      this.MainMenuStrip = this.menuStrip1;
      this.MaximizeBox = false;
      this.Name = "RootForm";
      this.Text = "Sinter Proxy";
      this.Load += new System.EventHandler(this.RootForm_Load);
      ((System.ComponentModel.ISupportInitialize)(this.remoteProcessesView)).EndInit();
      this.ResumeLayout(false);
      this.PerformLayout();

        }

       

        #endregion

        private Button ls_button;
        private System.Windows.Forms.DataGridView remoteProcessesView;
        private MenuStrip menuStrip1;
        private Button connect_button;
        private Button disconnect_button;
        private DataGridViewTextBoxColumn Process;
    private TextBox textBoxPasscode;
    private Label passcode_label;
    private Label label1;
    private TextBox textBoxIP;
    private Label label2;
    private TextBox textBoxPort;
  }
}
