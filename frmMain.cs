using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Reflection;
using System.Threading;

namespace EVIPBlocker
{
    public partial class frmMain : Form
    {
        public EVIPBlockerMain Prg = new EVIPBlockerMain();
        public bool StartImmediately = false;
        public bool StartOnlyEventViewer = false;
        public Thread manualThread = null;
        public bool ManualRunning = false;
        public frmMain(bool start, bool service)
        {
            StartImmediately = start;
            StartOnlyEventViewer = service;
            if (StartOnlyEventViewer) StartImmediately = true;

            InitializeComponent();
            EVIPBlockerMain.WriteLog = new Action<string>(WriteLog);
            CreateNotifyIcon();
            if (StartImmediately)
            {
                WindowState = FormWindowState.Minimized;
                ShowInTaskbar = false;
            }
        }

        private void WriteLog(string str)
        {
            if (StartOnlyEventViewer) return;
            if (InvokeRequired)
            {
                Invoke(new Action<string>(WriteLog), new object[] { str });
                return;
            }
            RichTextBox1.AppendText(str + "\r\n");
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            EnableDisable(false);
            timer2.Enabled = true;
            Prg.Start();
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            EnableDisable(true);
            timer2.Enabled = false;
            Prg.Stop();
        }

        private void EnableDisable(bool enable)
        {
            if (StartOnlyEventViewer) return;
            if (this.InvokeRequired)
            {
                Invoke(new Action<bool>(EnableDisable), new object[] { enable });
                return;
            }
            btnStart.Enabled = enable;
            btnStop.Enabled = !enable;
        }

        private void btnSetting_Click(object sender, EventArgs e)
        {
            if (StartOnlyEventViewer) return;
            frmSetting frm = new frmSetting();
            frm.ShowDialog();
        }

        private void frmMain_Resize(object sender, EventArgs e)
        {
            if (StartOnlyEventViewer) return;
            if (WindowState == FormWindowState.Minimized)
            {
                Minimize();
            }
        }


        private void Minimize()
        {
            Hide();
        }

        private void Maximize()
        {
            Show();
            WindowState = FormWindowState.Normal;
            ShowInTaskbar = true;
        }


        NotifyIcon _Icon;
        protected void CreateNotifyIcon()
        {
            _Icon = new NotifyIcon();
            _Icon.ContextMenuStrip = contextMenuStrip1;
            _Icon.Icon = this.Icon;
            _Icon.Text = "EVIPBlocker";
            _Icon.Visible = true;
            _Icon.MouseUp += new MouseEventHandler(_Icon_MouseUp);
        }

        private void EndForm()
        {
            _Icon.Visible = false;
            Visible = false;
            WindowState = FormWindowState.Normal;

            btnStop_Click(null, EventArgs.Empty);
            Environment.Exit(0);
        }


        private void _Icon_MouseUp(object sender, MouseEventArgs e)
        {
            if (StartOnlyEventViewer) return;
            if (e.Button == MouseButtons.Left)
            {
                MethodInfo mi = typeof(NotifyIcon).GetMethod("ShowContextMenu", BindingFlags.Instance | BindingFlags.NonPublic);
                mi.Invoke(_Icon, null);
            }
        }

        private void showToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            if (StartOnlyEventViewer) return;
            Maximize();
        }

        private void exitToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            if (StartOnlyEventViewer) return;
            EndForm();
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (StartOnlyEventViewer) return;
            Minimize();
            e.Cancel = true;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (StartOnlyEventViewer) return;
            frmBlocked frm = new frmBlocked();
            frm.ShowDialog();
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            if (StartOnlyEventViewer) return;
            Prg.Confirm();
        }

        public void SetCheck(ListViewItem lvi, bool chk)
        {
            if (StartOnlyEventViewer) return;
            if (InvokeRequired)
            {
                Invoke(new Action<ListViewItem, bool>(SetCheck), lvi, chk);
                return;
            }
            lvi.Checked = chk;
        }

        public void Remove(ListView lv, ListViewItem lvi)
        {
            if (StartOnlyEventViewer) return;
            if (InvokeRequired)
            {
                Invoke(new Action<ListView, ListViewItem>(Remove), lv, lvi);
                return;
            }
            lv.Items.Remove(lvi);
        }

        public void Add(ListView lv, ListViewItem lvi)
        {
            if (StartOnlyEventViewer) return;
            if (InvokeRequired)
            {
                Invoke(new Action<ListView, ListViewItem>(Add), lv, lvi);
                return;
            }
            lv.Items.Add(lvi);
        }

        public string ListViewItemText(ListViewItem lvi)
        {
            if (StartOnlyEventViewer) return string.Empty;
            if (InvokeRequired)
            {
                return (string)Invoke(new Func<ListViewItem, string>(ListViewItemText), lvi);
            }
            return lvi.Text;
        }

        public string ListViewItemSubItemText(ListViewItem lvi, int index)
        {
            if (StartOnlyEventViewer) return string.Empty;
            if (InvokeRequired)
            {
                return (string)Invoke(new Func<ListViewItem, int, string>(ListViewItemSubItemText), lvi, index);
            }
            return lvi.SubItems[index].Text;
        }

        public bool timerstart = false;
        public void timer(object obj)
        {
            if (StartOnlyEventViewer) return;
            timerstart = true;
            object[] objsx = (object[])obj;
            List<ListViewItem> items = (List<ListViewItem>)objsx[0];
            List<ListViewItem> items2 = (List<ListViewItem>)objsx[1];
            List<ListViewItem> items3 = (List<ListViewItem>)objsx[2];

            try
            {
                List<string> ips = RDPLoginIP.GetRDPServerIPs(EVIPBlockerMain.GetSwap());
                List<RDPLoginIP.ServerConnection> objs = RDPLoginIP.GetServerConnection(EVIPBlockerMain.GetSwap());
                List<RDPLoginIP.ServerConnection> rdp = null;
                List<RDPLoginIP.ServerConnection> nonhttp = null;
                List<RDPLoginIP.ServerConnection> http = null;

                string rdpport = System.Configuration.ConfigurationManager.AppSettings["RDPPort"] ?? "3389";
                if (ips != null && ips.Count > 0)
                    rdp = objs.Where(m => m.DestinationIP == ips[0] && m.DestinationPort == rdpport).ToList();
                else
                    rdp = new List<RDPLoginIP.ServerConnection>();

                nonhttp = objs.Where(m =>
                    m.SourceIP != m.DestinationIP && //Connect to ownself
                    m.DestinationPort != "443" && m.DestinationPort != "80" && //Incoming HTTP
                    m.SourcePort != "443" && m.SourcePort != "80" && //Outgoing Http
                    m.DestinationPort != rdpport && //RDP
                    m.SourceIP != "*" //Wildcard
                ).ToList();
                http = objs.Where(m =>
                    m.SourceIP != m.DestinationIP && //Connect to ownself
                    ((m.DestinationPort == "443" || m.DestinationPort == "80") || //Incoming HTTP
                    (m.SourcePort == "443" || m.SourcePort == "80")) && //Outgoing Http
                    m.DestinationPort != rdpport && //RDP
                    m.SourceIP != "*" //Wildcard
                ).ToList();


                bool exist = false;
                List<ListViewItem> delete = new List<ListViewItem>();
                string use = (System.Configuration.ConfigurationManager.AppSettings["UseRDP"] ?? "TRUE").ToUpper().Trim();
                if (use == "TRUE" || use == "Y" || use == "1")
                {
                    //RDP
                    foreach (RDPLoginIP.ServerConnection a in rdp)
                    {
                        List<string> added = new List<string>();
                        exist = false;
                        foreach (ListViewItem lvi in items)
                        {
                            if (ListViewItemText(lvi) == a.SourceIP)
                            {
                                exist = true;
                                break;
                            }
                        }
                        if (exist == false && added.Contains(a.SourceIP) == false)
                        {
                            ListViewItem lvi = new ListViewItem();
                            lvi.Text = a.SourceIP;
                            lvi.SubItems.Add(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                            lvi.Tag = a;
                            Add(listView1, lvi);
                            added.Add(a.SourceIP);
                        }
                    }

                    foreach (ListViewItem lvi in items)
                    {
                        bool exist2 = false;
                        foreach (RDPLoginIP.ServerConnection a in rdp)
                        {
                            if (a.SourceIP == ListViewItemText(lvi))
                            {
                                exist2 = true;
                                break;
                            }
                        }
                        if (exist2 == false) delete.Add(lvi);
                    }

                    foreach (ListViewItem d in delete)
                    {
                        Remove(listView1, d);
                    }
                    delete.Clear();
                }

                use = (System.Configuration.ConfigurationManager.AppSettings["UseNonHTTP"] ?? "TRUE").ToUpper().Trim();
                if (use == "TRUE" || use == "Y" || use == "1")
                {
                    // Non HTTP
                    foreach (RDPLoginIP.ServerConnection a in nonhttp)
                    {
                        List<string> added = new List<string>();
                        exist = false;
                        foreach (ListViewItem lvi in items2)
                        {
                            if (ListViewItemText(lvi) == a.SourceIP)
                            {
                                exist = true;
                                break;
                            }
                        }
                        if (exist == false && added.Contains(a.SourceIP) == false)
                        {
                            ListViewItem lvi = new ListViewItem();
                            if (EVIPBlockerMain.GetSwap() == false)
                            {
                                lvi.Text = a.SourceIP;
                                lvi.SubItems.Add(a.SourcePort.ToString());
                            }
                            else
                            {
                                lvi.Text = a.DestinationIP;
                                lvi.SubItems.Add(a.DestinationPort.ToString());
                            }
                            lvi.SubItems.Add(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                            lvi.SubItems.Add(a.ConnectionState);
                            lvi.Tag = a;
                            PopulateNonHTTPCheck(lvi);
                            Add(listView2, lvi);
                            added.Add(a.SourceIP);
                        }
                    }


                    delete = new List<ListViewItem>();
                    foreach (ListViewItem lvi in items2)
                    {
                        bool exist2 = false;
                        foreach (RDPLoginIP.ServerConnection a in nonhttp)
                        {
                            if (a.SourceIP == ListViewItemText(lvi))
                            {
                                exist2 = true;
                                break;
                            }
                        }
                        if (exist2 == false) delete.Add(lvi);
                    }

                    foreach (ListViewItem d in delete)
                    {
                        Remove(listView2, d);
                    }
                    delete.Clear();
                }

                use = (System.Configuration.ConfigurationManager.AppSettings["UseHTTP"] ?? "TRUE").ToUpper().Trim();
                if (use == "TRUE" || use == "Y" || use == "1")
                {
                    // HTTP
                    List<string> added = new List<string>();
                    foreach (RDPLoginIP.ServerConnection a in http)
                    {
                        exist = false;
                        foreach (ListViewItem lvi in items3)
                        {
                            if (ListViewItemText(lvi) == a.SourceIP)
                            {
                                exist = true;
                                break;
                            }
                        }
                        if (exist == false && added.Contains(a.SourceIP) == false)
                        {
                            ListViewItem lvi = new ListViewItem();
                            if (EVIPBlockerMain.GetSwap() == false)
                            {
                                lvi.Text = a.SourceIP;
                                lvi.SubItems.Add(a.SourcePort.ToString());
                            }
                            else
                            {
                                lvi.Text = a.DestinationIP;
                                lvi.SubItems.Add(a.DestinationPort.ToString());
                            }
                            lvi.SubItems.Add(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                            lvi.SubItems.Add(a.ConnectionState);
                            lvi.Tag = a;
                            PopulateHTTPCheck(lvi);
                            Add(listView3, lvi);
                            added.Add(a.SourceIP);
                        }
                    }

                    delete = new List<ListViewItem>();
                    foreach (ListViewItem lvi in items3)
                    {
                        bool exist2 = false;
                        foreach (RDPLoginIP.ServerConnection a in http)
                        {
                            if (a.SourceIP == ListViewItemText(lvi))
                            {
                                exist2 = true;
                                break;
                            }
                        }
                        if (exist2 == false) delete.Add(lvi);
                    }

                    foreach (ListViewItem d in delete)
                    {
                        Remove(listView3, d);
                    }
                }
            }
            catch (Exception ex) 
            {
                Console.WriteLine(ex.Message);
            }
            timerstart = false;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (StartOnlyEventViewer) return;
            if (Prg.IsRunning == false) return;

            List<ListViewItem> items = listView1.Items.OfType<ListViewItem>().ToList();
            List<ListViewItem> items2 = listView2.Items.OfType<ListViewItem>().ToList();
            List<ListViewItem> items3 = listView3.Items.OfType<ListViewItem>().ToList();

            if (timerstart == false)
            {
                ParameterizedThreadStart ts = new ParameterizedThreadStart(timer);
                Thread t = new Thread(ts);
                t.Start(new object[] { items, items2, items3 });
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (StartOnlyEventViewer) return;
            ListViewItem lvi = null;
            List<ListViewItem> items = listView1.SelectedItems.OfType<ListViewItem>().ToList();
            if (items != null && items.Count > 0) lvi = items[0];
            if (lvi == null) return;

            RDPLoginIP.ServerConnection tg = (RDPLoginIP.ServerConnection) lvi.Tag;

            DialogResult dr = MessageBox.Show("Are you sure you want to Block IP " + tg.SourceIP, "Sure?", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (dr == DialogResult.Yes)
            {
                bool b = EVIPBlockerMain.CheckAddRule(tg.SourceIP, "UI");
                if (b)
                {
                    EVIPBlockerMain.CheckAddRuleToFile(tg.SourceIP);
                    listView1.Items.Remove(lvi);
                    MessageBox.Show("IP " + tg.SourceIP + " successfully removed");
                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (StartOnlyEventViewer) return;
            ListViewItem lvi = null;
            List<ListViewItem> items = listView2.SelectedItems.OfType<ListViewItem>().ToList();
            if (items != null && items.Count > 0) lvi = items[0];
            if (lvi == null) return;

            RDPLoginIP.ServerConnection tg = (RDPLoginIP.ServerConnection)lvi.Tag;

            DialogResult dr = MessageBox.Show("Are you sure you want to Block IP " + tg.SourceIP, "Sure?", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (dr == DialogResult.Yes)
            {
                bool b = EVIPBlockerMain.CheckAddRule(tg.SourceIP, "UI");
                if (b)
                {
                    EVIPBlockerMain.CheckAddRuleToFile(tg.SourceIP);
                    listView2.Items.Remove(lvi);
                    MessageBox.Show("IP " + tg.SourceIP + " successfully removed");
                }
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (StartOnlyEventViewer) return;
            ListViewItem lvi = null;
            List<ListViewItem> items = listView1.SelectedItems.OfType<ListViewItem>().ToList();
            if (items != null && items.Count > 0) lvi = items[0];
            if (lvi == null) return;

            RDPLoginIP.ServerConnection tg = (RDPLoginIP.ServerConnection)lvi.Tag;
            frmProcess frm = new frmProcess(tg, tg.PID, lvi.Text, tg.SourcePort);
            frm.ShowDialog();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (StartOnlyEventViewer) return;
            ListViewItem lvi = null;
            List<ListViewItem> items = listView2.SelectedItems.OfType<ListViewItem>().ToList();
            if (items != null && items.Count > 0) lvi = items[0];
            if (lvi == null) return;

            RDPLoginIP.ServerConnection tg = (RDPLoginIP.ServerConnection)lvi.Tag;
            frmProcess frm = new frmProcess(tg, tg.PID, lvi.Text, lvi.SubItems[1].Text);
            frm.ShowDialog();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            if (StartOnlyEventViewer) return;
            ListViewItem lvi = null;
            List<ListViewItem> items = listView3.SelectedItems.OfType<ListViewItem>().ToList();
            if (items != null && items.Count > 0) lvi = items[0];
            if (lvi == null) return;

            RDPLoginIP.ServerConnection tg = (RDPLoginIP.ServerConnection)lvi.Tag;

            DialogResult dr = MessageBox.Show("Are you sure you want to Block IP " + tg.SourceIP, "Sure?", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (dr == DialogResult.Yes)
            {
                bool b = EVIPBlockerMain.CheckAddRule(tg.SourceIP, "UI");
                if (b)
                {
                    EVIPBlockerMain.CheckAddRuleToFile(tg.SourceIP);
                    listView3.Items.Remove(lvi);
                    MessageBox.Show("IP " + tg.SourceIP + " successfully removed");
                }
            }
        }

        public void PopulateHTTPCheck(ListViewItem lvi)
        {
            if (StartOnlyEventViewer) return;
            string[] items = (System.Configuration.ConfigurationManager.AppSettings["HTTPChecked"] ?? "").Split(new char[] { '|' });
            if (items == null || items.Length == 0 || (items.Length > 0 && items[0] == "")) return;

            try
            {
                lvi.Checked = false;
                RDPLoginIP.ServerConnection spo = (RDPLoginIP.ServerConnection)lvi.Tag;
                RDPLoginIP.ServerProcessObject proc = spo.Process;
                bool foundstr = false;
                foreach (string i in items)
                {
                    if (proc != null && proc.FullPath.IndexOf(i, StringComparison.CurrentCultureIgnoreCase) >= 0)
                    {
                        foundstr = true;
                        break;
                    }
                }
                if (foundstr == false)
                    lvi.Checked = true;
            }
            catch { }
        }

        public void PopulateNonHTTPCheck(ListViewItem lvi)
        {
            if (StartOnlyEventViewer) return;
            string[] items = (System.Configuration.ConfigurationManager.AppSettings["NonHTTPChecked"] ?? "").Split(new char[] { '|' });
            if (items == null || items.Length == 0 || (items.Length > 0 && items[0] == "")) return;

            try
            {
                lvi.Checked = false;
                RDPLoginIP.ServerConnection spo = (RDPLoginIP.ServerConnection)lvi.Tag;
                RDPLoginIP.ServerProcessObject proc = spo.Process;
                bool foundstr = false;
                foreach (string i in items)
                {
                    if (proc != null && proc.FullPath.IndexOf(i, StringComparison.CurrentCultureIgnoreCase) >= 0)
                    {
                        foundstr = true;
                        break;
                    }
                }
                if (foundstr == false)
                    lvi.Checked = true;
            }
            catch { }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            if (StartOnlyEventViewer) return;
            ListViewItem lvi = null;
            List<ListViewItem> items = listView3.SelectedItems.OfType<ListViewItem>().ToList();
            if (items != null && items.Count > 0) lvi = items[0];
            if (lvi == null) return;

            RDPLoginIP.ServerConnection tg = (RDPLoginIP.ServerConnection)lvi.Tag;
            frmProcess frm = new frmProcess(tg, tg.PID, lvi.Text, lvi.SubItems[1].Text);
            frm.ShowDialog();
        }

        private void button8_Click(object sender, EventArgs e)
        {
            if (StartOnlyEventViewer) return;
            listView1.Items.Clear();
        }

        private void button9_Click(object sender, EventArgs e)
        {
            if (StartOnlyEventViewer) return;
            listView2.Items.Clear();
        }

        private void button10_Click(object sender, EventArgs e)
        {
            if (StartOnlyEventViewer) return;
            listView3.Items.Clear();
        }

        private void button11_Click(object sender, EventArgs e)
        {
            if (StartOnlyEventViewer) return;
            try
            {
                foreach (ListViewItem lvi in listView3.Items)
                {
                    try
                    {
                        PopulateHTTPCheck(lvi);
                    }
                    catch { }
                }
            }
            catch { }
        }

        private void button12_Click(object sender, EventArgs e)
        {
            if (StartOnlyEventViewer) return;
            try
            {
                foreach (ListViewItem lvi in listView2.Items)
                {
                    try
                    {
                        PopulateNonHTTPCheck(lvi);
                    }
                    catch { }
                }
            }
            catch { }
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            if (StartImmediately)
            {
                btnStart_Click(btnStart, EventArgs.Empty);
            }
        }

        private void button14_Click(object sender, EventArgs e)
        {
            listView4.Items.Clear();
        }

        private List<ListViewItem> GetItems(ListView lv)
        {
            if (InvokeRequired)
            {
                return (List<ListViewItem>)Invoke(new Func<ListView, List<ListViewItem>>(GetItems), lv);
            }
            return lv.Items.OfType<ListViewItem>().ToList();
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            if (ManualRunning) return;
            
            ThreadStart ts = new ThreadStart(GetManual);
            manualThread = new Thread(ts);
            manualThread.Start();
        }

        private void GetManual()
        {
            ManualRunning = true;
            List<EventDetails> details = Prg.GetSuccessUnsuccessLogin();
            if (details == null || details.Count == 0) return;

            List<string> added = new List<string>();
            List<ListViewItem> items4 = GetItems(listView4);
            foreach (EventDetails det in details)
            {
                if (items4 != null)
                {
                    var it = items4.Where(m => m.SubItems[1].Text == det.ip).FirstOrDefault();
                    if (it != null) continue;
                }
                if (added.Contains(det.ip)) continue;

                ListViewItem lvi = new ListViewItem();
                lvi.Text = det.dateAdded.ToString("dd MMM yyyy HH:mm:ss");
                lvi.SubItems.Add(det.ip);
                lvi.SubItems.Add(det.workstation);
                lvi.SubItems.Add(det.type);
                lvi.Tag = det;
                Add(listView4, lvi);
                added.Add(det.ip);
            }
            ManualRunning = false;
        }

        private void button15_Click(object sender, EventArgs e)
        {
            if (StartOnlyEventViewer) return;
            ListViewItem lvi = null;
            List<ListViewItem> items = listView4.SelectedItems.OfType<ListViewItem>().ToList();
            if (items != null && items.Count > 0) lvi = items[0];
            if (lvi == null) return;

            EventDetails tg = (EventDetails)lvi.Tag;

            DialogResult dr = MessageBox.Show("Are you sure you want to Block IP " + tg.ip, "Sure?", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (dr == DialogResult.Yes)
            {
                bool b = EVIPBlockerMain.CheckAddRule(tg.ip, "UI");
                if (b)
                {
                    EVIPBlockerMain.CheckAddRuleToFile(tg.ip);
                    listView2.Items.Remove(lvi);
                    MessageBox.Show("IP " + tg.ip + " successfully removed");
                }
            }
        }
    }
}
