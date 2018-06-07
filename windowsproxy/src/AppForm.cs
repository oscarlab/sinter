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
  public partial class AppForm : Form
  {
    public AppForm()
    {
      InitializeComponent();
    }

    public DelegateKeyPresses delegateKeyPresses;
    
    #region keybroad handler
    override protected Boolean ProcessCmdKey(ref Message msg, Keys keyData)
    {
      if (keyData == Keys.Escape) // || keyData == Keys.Back || keyData == Keys.Delete || keyData == Keys.Enter)
      {
        KeyPressEventArgs e = new KeyPressEventArgs((char)keyData);
        Form_KeyPress(null, e);
        
        return true;
      }
      /* We retrun false for all keys except command keys like escape. This will
       * make sure a key event is generated for these guys and caught in Form_Keypress
       */
      return false;
    }

    // Detect all numeric characters at the form level and consume 1,  
    // 4, and 7. Note that Form.KeyPreview must be set to true for this 
    // event handler to be called. 
    void Form_KeyPress(object sender, KeyPressEventArgs e)
    {
      string key;
      switch (e.KeyChar)
      {
        case (char)Keys.Enter:
          key = "ENTER";
          break;

        case (char)Keys.Space:
          key = "SPACE";
          break;

        case (char)Keys.Escape:
          key = "ESCAPE";
          break;

        case (char)Keys.Tab:
            key = "TAB";
            break;

        case (char)Keys.Back:
          key = "DELETE";
          break;

        case (char)Keys.Delete:
          key = "DELETE";
          break;

        default:
          key = e.KeyChar.ToString();
          break;
      }

      Console.WriteLine("Key pressed: "+ key);
      delegateKeyPresses(key);
    }

    private void Form_KeyDown(object sender, KeyEventArgs e)
    {
      Console.WriteLine("Key down " + e.KeyValue);
    }

    private void Form_Load(object sender, EventArgs e)
    {
      KeyPreview = true;
      KeyPress += new KeyPressEventHandler(Form_KeyPress);
      KeyDown += Form_KeyDown;
    }
    #endregion
  }
}
