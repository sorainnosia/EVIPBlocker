using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace EVIPBlocker
{
    public partial class frmBlocked : Form
    {
        public frmBlocked()
        {
            InitializeComponent();
        }

        private void frmBlocked_Load(object sender, EventArgs e)
        {
            LoadBlocked();
        }

        private void LoadBlocked()
        {
            listView1.Items.Clear();
            List<RemoverDetail> alreadyBlocked = new List<RemoverDetail>();
            List<string> alreadyBlockedTemp = new List<string>();
            EVIPBlockerMain.GetBlocked(alreadyBlocked, alreadyBlockedTemp);
            foreach (RemoverDetail rd in alreadyBlocked)
            {
                ListViewItem lvi = new ListViewItem();
                lvi.Text = rd.IP;
                lvi.SubItems.Add(rd.AddedTime.ToString("dd MMM yyyy HH:mm:ss"));
                lvi.Tag = rd;
                listView1.Items.Add(lvi);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems == null || listView1.SelectedItems.Count <= 0) return;
            int success = 0;
            int fail = 0;
            for (int i = listView1.SelectedItems.Count - 1; i >= 0; i--)
            {
                ListViewItem lvi = listView1.SelectedItems[i];
                if (lvi.Tag != null && lvi.Tag is RemoverDetail)
                {
                    try
                    {
                        EVIPBlockerMain.RemoveRule((RemoverDetail)lvi.Tag);
                        success++;
                        listView1.Items.Remove(lvi);
                    }
                    catch
                    {
                        fail++;
                    }
                }
            }
            WriteScreen();
            LoadBlocked();
            MessageBox.Show("Success " + success.ToString() + " Fail " + fail.ToString());
        }

        private void WriteScreen()
        {
            string blockedfile = System.Configuration.ConfigurationManager.AppSettings["BlockedFile"];
            List<string> result = new List<string>();
            foreach (ListViewItem lvi in listView1.Items)
            {
                if (lvi.Tag != null && lvi.Tag is RemoverDetail)
                {
                    RemoverDetail rd = (RemoverDetail) lvi.Tag;
                    result.Add(rd.IP + "|" + rd.AddedTime.ToString());
                }
            }

            lock (EVIPBlockerMain.BlockedFileLock)
            {
                File.Delete(blockedfile);
                File.AppendAllLines(blockedfile, result);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
