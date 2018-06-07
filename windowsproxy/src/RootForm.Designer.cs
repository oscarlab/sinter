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
      ((System.ComponentModel.ISupportInitialize)(this.remoteProcessesView)).BeginInit();
      this.SuspendLayout();
      // 
      // ls_button
      // 
      this.ls_button.Location = new System.Drawing.Point(32, 124);
      this.ls_button.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
      this.ls_button.Name = "ls_button";
      this.ls_button.Size = new System.Drawing.Size(119, 49);
      this.ls_button.TabIndex = 8;
      this.ls_button.Text = "Show Remote Processes";
      this.ls_button.UseVisualStyleBackColor = true;
      this.ls_button.Visible = false;
      this.ls_button.Click += new System.EventHandler(this.FetchRemoteProcesses);
      // 
      // remoteProcessesView
      // 
      this.remoteProcessesView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
      this.remoteProcessesView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Process});
      this.remoteProcessesView.Location = new System.Drawing.Point(179, 37);
      this.remoteProcessesView.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
      this.remoteProcessesView.Name = "remoteProcessesView";
      this.remoteProcessesView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
      this.remoteProcessesView.Size = new System.Drawing.Size(547, 284);
      this.remoteProcessesView.TabIndex = 9;
      this.remoteProcessesView.Visible = false;
      this.remoteProcessesView.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.LoadRemoteProcess);
      // 
      // Process
      // 
      this.Process.HeaderText = "Process Name";
      this.Process.Name = "Process";
      this.Process.ReadOnly = true;
      this.Process.Width = 165;
      // 
      // menuStrip1
      // 
      this.menuStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
      this.menuStrip1.Location = new System.Drawing.Point(0, 0);
      this.menuStrip1.Name = "menuStrip1";
      this.menuStrip1.Padding = new System.Windows.Forms.Padding(8, 2, 0, 2);
      this.menuStrip1.Size = new System.Drawing.Size(741, 24);
      this.menuStrip1.TabIndex = 10;
      this.menuStrip1.Text = "menuStrip1";
      // 
      // connect_button
      // 
      this.connect_button.Location = new System.Drawing.Point(32, 37);
      this.connect_button.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
      this.connect_button.Name = "connect_button";
      this.connect_button.Size = new System.Drawing.Size(119, 33);
      this.connect_button.TabIndex = 11;
      this.connect_button.Text = "Connect";
      this.connect_button.UseVisualStyleBackColor = true;
      this.connect_button.Click += new System.EventHandler(this.Connect);
      // 
      // disconnect_button
      // 
      this.disconnect_button.Location = new System.Drawing.Point(32, 289);
      this.disconnect_button.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
      this.disconnect_button.Name = "disconnect_button";
      this.disconnect_button.Size = new System.Drawing.Size(119, 32);
      this.disconnect_button.TabIndex = 12;
      this.disconnect_button.Text = "Disconnect";
      this.disconnect_button.UseVisualStyleBackColor = true;
      this.disconnect_button.Click += new System.EventHandler(this.Disconnect);
      // 
      // RootForm
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.ClientSize = new System.Drawing.Size(741, 354);
      this.Controls.Add(this.disconnect_button);
      this.Controls.Add(this.connect_button);
      this.Controls.Add(this.remoteProcessesView);
      this.Controls.Add(this.ls_button);
      this.Controls.Add(this.menuStrip1);
      this.MainMenuStrip = this.menuStrip1;
      this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
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
    }
}
