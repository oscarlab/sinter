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
