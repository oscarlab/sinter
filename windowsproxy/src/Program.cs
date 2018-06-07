using System;
using System.Windows.Forms;
//using PListLib;

namespace WindowsProxy
{
  static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            /*String dict_name = "roles";
            Dictionary<string, object> roles = plistParser.getdict(dict_name);
            if (roles != null)
            {
                foreach (KeyValuePair<string, object> kvp in roles)
                {
                    Console.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~");
                    Console.WriteLine("Key = {0}, Value = {1}", kvp.Key, kvp.Value);

                }
            }*/

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            RootForm form = new RootForm();
            Application.Run(form);
        }
    }
}
