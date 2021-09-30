using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Configuration;
using System.IO;
using NetFwTypeLib;
using System.Diagnostics;
using System.Threading;

namespace EVIPBlocker
{
    public partial class frmSetting : Form
    {
        public frmSetting()
        {
            InitializeComponent();
        }

        private void frmSetting_Load(object sender, EventArgs e)
        {
            LoadSetting();
        }

        private void LoadSetting()
        {
            try
            {
                string[] logtypes = ConfigurationManager.AppSettings["LogTypes"].Split(new char[] { '|' });
                foreach (string log in logtypes)
                {
                    string[] logs = log.Split(new char[] { '=' });
                    if (logs.Length != 4) continue;

                    ucSetting uc = new ucSetting(this, logs[0], logs[1], logs[2], logs[3]);
                    uc.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;
                    panel1.Controls.Add(uc);
                }
                Reorder();
                txtRemoveAfterHour.Enabled = false;
                chkRemoveBlock.Checked = false;
                if (ConfigurationManager.AppSettings["RemoveBlock"] == "Y")
                {
                    chkRemoveBlock.Checked = true;
                }
                string set = (System.Configuration.ConfigurationManager.AppSettings["ExcludeInteractiveIP"] ?? "").ToString().ToUpper();
                if (set == "FALSE" || set == "0" || set == "N" || set == "")
                    chkInteractive.Checked = false;
                else
                    chkInteractive.Checked = true;
                set = (System.Configuration.ConfigurationManager.AppSettings["Swap"] ?? "").ToString().ToUpper();
                if (set == "FALSE" || set == "0" || set == "N" || set == "")
                    chkSwap.Checked = false;
                else
                    chkSwap.Checked = true;
                int temp = 0;
                if (int.TryParse(ConfigurationManager.AppSettings["RemoveAfterHour"], out temp) == false)
                    txtRemoveAfterHour.Text = "0";
                else
                    txtRemoveAfterHour.Text = temp.ToString();
                if (int.TryParse(ConfigurationManager.AppSettings["Threshold"], out temp) == false)
                    txtThreshold.Text = "0";
                else
                    txtThreshold.Text = temp.ToString();
                if (int.TryParse(ConfigurationManager.AppSettings["MinutesScan"], out temp) == false)
                    txtMinutesScan.Text = "0";
                else
                    txtMinutesScan.Text = temp.ToString();

                txtDirectory.Text = Path.GetDirectoryName(ConfigurationManager.AppSettings["BlockedFile"]);
                txtIPRegex.Text = ConfigurationManager.AppSettings["IPRegex"];
            }
            catch (Exception ex)
            {
                EVIPBlockerMain.WriteFile(DateTime.Now.ToString("dd MMM yyyy HH:mm:ss") + " Load Settings failed : " + ex.Message);
            }
        }
        
        public void Remove(ucSetting uc)
        {
            panel1.SuspendLayout();
            if (panel1.Controls.Contains(uc)) panel1.Controls.Remove(uc);
            Reorder();
            panel1.ResumeLayout();
        }

        public void Reorder()
        {
            int top = 0;
            foreach (ucSetting uc in panel1.Controls)
            {
                uc.Top = top + panel1.AutoScrollPosition.Y;
                top += uc.Height - 2;
            }
        }

        public void RemoveSetting(ucSetting uc)
        {
            Remove(uc);
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            ucSetting uc = new ucSetting(this, string.Empty, string.Empty, string.Empty, string.Empty);
            uc.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;
            panel1.Controls.Add(uc);
            Reorder();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                int temp = 0;
                if (chkRemoveBlock.Checked && int.TryParse(txtRemoveAfterHour.Text, out temp) == false)
                {
                    MessageBox.Show("Remove After Hour must be number", "Fail", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                else
                    AddOrUpdateAppSettings("RemoveAfterHour", temp.ToString());
                AddOrUpdateAppSettings("ExcludeInteractiveIP", chkInteractive.Checked.ToString());
                AddOrUpdateAppSettings("Swap", chkSwap.Checked.ToString());
                if (int.TryParse(txtThreshold.Text, out temp) == false)
                {
                    MessageBox.Show("Threshold must be number", "Fail", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                else
                    AddOrUpdateAppSettings("Threshold", temp.ToString());
                if (int.TryParse(txtMinutesScan.Text, out temp) == false)
                {
                    MessageBox.Show("Minutes Scan must be number", "Fail", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                else
                    AddOrUpdateAppSettings("MinutesScan", temp.ToString());

                if (chkRemoveBlock.Checked)
                    AddOrUpdateAppSettings("RemoveBlock", "Y");
                else
                    AddOrUpdateAppSettings("RemoveBlock", "N");

                AddOrUpdateAppSettings("BlockedFile", Path.Combine(txtDirectory.Text, Path.GetFileName(ConfigurationManager.AppSettings["BlockedFile"])));
                AddOrUpdateAppSettings("LogFile", Path.Combine(txtDirectory.Text, Path.GetFileName(ConfigurationManager.AppSettings["LogFile"])));

                int position = 0;
                List<string> settings = new List<string>();
                foreach (ucSetting uc in panel1.Controls)
                {
                    string error = uc.ValidateSetting();
                    if (string.IsNullOrEmpty(error) == false)
                    {
                        MessageBox.Show("Setting " + position.ToString() + " : " + error, "Fail", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    settings.Add(uc.GetString());
                    position++;
                }
                string value = string.Join("|", settings);
                if (string.IsNullOrEmpty(value) == false)
                    AddOrUpdateAppSettings("LogTypes", value);
                if (string.IsNullOrEmpty(txtIPRegex.Text) == false)
                    AddOrUpdateAppSettings("IPRegex", txtIPRegex.Text);
                MessageBox.Show("Saved Successfully\r\nYou need to restart Monitor to take effect", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Close();
            }
            catch(Exception ex)
            {
                EVIPBlockerMain.WriteFile(DateTime.Now.ToString("dd MMM yyyy HH:mm:ss") + " Save Setting fails with exception : " + ex.Message);
            }
        }

        public static void AddOrUpdateAppSettings(string key, string value)
        {
            try
            {
                var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var settings = configFile.AppSettings.Settings;
                if (settings[key] == null)
                {
                    settings.Add(key, value);
                }
                else
                {
                    settings[key].Value = value;
                }
                configFile.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
            }
            catch (Exception ex)
            {
                EVIPBlockerMain.WriteFile(DateTime.Now.ToString("dd MMM yyyy HH:mm:ss") + " Error writing app settings : " + ex.Message);
            }
        }

        private void chkRemoveBlock_CheckedChanged(object sender, EventArgs e)
        {
            txtRemoveAfterHour.Enabled = chkRemoveBlock.Checked;
        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void label6_Click(object sender, EventArgs e)
        {

        }

        private void txtDirectory_TextChanged(object sender, EventArgs e)
        {

        }

        private void txtIPRegex_TextChanged(object sender, EventArgs e)
        {

        }

        private void chkSwap_CheckedChanged(object sender, EventArgs e)
        {

        }

        public void AddText(string str)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(AddText), str);
                return;
            }
            richTextBox1.AppendText(str);
        }

        private void ButtonText(Button btn, string txt)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<Button, string>(ButtonText), btn, txt);
                return;
            }
            btn.Text = txt;
        }

        Thread t = null;
        private void button2_Click(object sender, EventArgs e)
        {
            if (button2.Text == "Start")
            {
                button2.Text = "Stop";
                ThreadStart ts = new ThreadStart(Test);
                t = new Thread(ts);
                t.Start();
            }
            else
            {
                button2.Text = "Start";
                try
                {
                    if (t != null) t.Abort();
                }
                catch { }
            }
        }

        private void Test()
        {
            AddText("Reading SuccessTypes\r\n");
            AddText("====================\r\n");
            ReadConfig("SuccessTypes");
            AddText("Reading LogTypes\r\n");
            AddText("====================\r\n");
            ReadConfig("LogTypes");
        }

        private void ReadConfig(string t)
        {
            string[] types = (ConfigurationManager.AppSettings[t] ?? "").Split(new char[] { '|' });
            foreach (string type in types)
            {
                string[] ss = type.Split(new char[] { '=' });
                if (ss.Length < 2) continue;
                AddText("EventID : " + ss[1] + "\r\n");

                bool found = false;
                EventLog ev = new EventLog(ss[0], System.Environment.MachineName);
                for (int i = 0; i < ev.Entries.Count; i++)
                {
                    EventLogEntry CurrentEntry = ev.Entries[i];
                    if (CurrentEntry.TimeGenerated >= DateTime.Now.AddDays(-1 * 7))
                    {
                        if (CurrentEntry.EventID.ToString() != ss[1]) continue;
                        {
                            found = true;
                            AddText(CurrentEntry.Message + "\r\n");
                            AddText("====================\r\n");
                            break;
                        }
                    }
                }
                if (found == false)
                {
                    AddText("Unable to find this EventID\r\n");
                }
            }
            ButtonText(button2, "Start");
        }
    }
}
