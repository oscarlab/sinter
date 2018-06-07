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
