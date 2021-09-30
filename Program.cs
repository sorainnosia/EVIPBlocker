using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EVIPBlocker
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            frmMain frm = null;
            if (args == null || args.Length == 0)
                frm = new frmMain(false, false);
            else if (args != null && args.Length > 0 && args[0] != null && args[0].ToUpper() == "/START")
                frm = new frmMain(true, false);
            else if (args != null && args.Length > 0 && args[0] != null && args[0].ToUpper() == "/SERVICE")
                frm = new frmMain(true, true);
            Application.Run(frm);
        }
    }
}
