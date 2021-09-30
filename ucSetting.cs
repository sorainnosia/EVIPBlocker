using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EVIPBlocker
{
    public partial class ucSetting : UserControl
    {
        public frmSetting SettingParent = null;

        public ucSetting(frmSetting parent, string loggroup, string eventid, string worddetected, string scanname)
        {
            SettingParent = parent;
            InitializeComponent();
            txtLogGroup.Text = loggroup;
            txtEventID.Text = eventid;
            txtWordDetected.Text = worddetected;
            txtScanName.Text = scanname;
        }

        private void btnDel_Click(object sender, EventArgs e)
        {
            if (SettingParent != null) SettingParent.RemoveSetting(this);
        }

        public string GetString()
        {
            return txtLogGroup.Text + "=" + txtEventID.Text + "=" + txtWordDetected.Text + "=" + txtScanName.Text;
        }

        public string ValidateSetting()
        {
            if (string.IsNullOrEmpty(txtLogGroup.Text.Trim()))
                return "Log Group must not be empty";
            if (string.IsNullOrEmpty(txtEventID.Text.Trim()))
                return "EventID must not be empty";
            if (string.IsNullOrEmpty(txtWordDetected.Text.Trim()))
                return "Word Detected must not be empty";
            if (string.IsNullOrEmpty(txtScanName.Text.Trim()))
                return "Scan Name must not be empty";
            if (txtLogGroup.Text.IndexOf("=") >= 0)
                return "Log Group can't contains =";
            if (txtEventID.Text.IndexOf("=") >= 0)
                return "EventID can't contains =";
            if (txtWordDetected.Text.IndexOf("=") >= 0)
                return "Word Detected can't contains =";
            if (txtScanName.Text.IndexOf("=") >= 0)
                return "Scan Name can't contains =";
            long temp2 = 0;
            if (long.TryParse(txtEventID.Text, out temp2) == false)
            {
                return "EventID must be a number in Event Viewer";
            }   
            return string.Empty;
        }
    }
}
