using System;
using System.Windows.Forms;

namespace Sintering {

  public enum StreamStatusFlags {
    NoMoreData,
    NoTrailer,
    NoHeader,
    ClientQuitted,
  }

  static class Program {
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main() {
      Application.EnableVisualStyles();
      Application.SetCompatibleTextRenderingDefault(false);
      Application.Run(new Form1());
    }
  }
}
