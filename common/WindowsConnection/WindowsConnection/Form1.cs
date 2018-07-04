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
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;

namespace Sintering {

  public partial class Form1 : Form {
    public Form1() {
      InitializeComponent();
    }

    private void Form1_Load(object sender , EventArgs e) {
    }

        //ClientHandler clientHandler = null;
    private void button1_Click(object sender , EventArgs e) {
      //clientHandler = new ClientHandler();
    }

    private void button2_Click(object sender , EventArgs e) {
      Sinter sinter = new Sinter {
        HeaderNode = new Header {
          ServiceCode = 1 ,
        }
      };

      //if (clientHandler != null)
      //  clientHandler.sendMessage(sinter);
    }
  }
}
