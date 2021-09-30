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
    public partial class frmProcess : Form
    {
        public string PID = string.Empty;
        public string IP = string.Empty;
        public string Port = string.Empty;
        public RDPLoginIP.ServerConnection Conn = null;
        public frmProcess(RDPLoginIP.ServerConnection con, string pid, string ip, string port)
        {
            Conn = con;
            PID = pid;
            IP = ip;
            Port = port;
            InitializeComponent();
            LoadPID();
        }

        public void LoadPID()
        {
            if (Conn != null && Conn.Process != null)
            {
                txtPath.Text = Conn.Process.FullPath;
                txtCommand.Text = Conn.Process.CommandLine;
                txtService.Text = Conn.Process.Service;
                if (EVIPBlockerMain.GetSwap() == false)
                {
                    txtSource.Text = Conn.DestinationIP;
                    txtSourcePort.Text = Conn.DestinationPort;
                }
                else
                {
                    txtSource.Text = Conn.SourceIP;
                    txtSourcePort.Text = Conn.SourcePort;
                }
            }
            txtPID.Text = PID;
            txtIP.Text = IP;
            txtPort.Text = Port;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //bool result = RDPLoginIP.StopService("AVCTP service");
            //bool result2 = RDPLoginIP.DisableService("AVCTP service");
            //bool result3 = RDPLoginIP.EnableService("AVCTP service");
            //bool result4 = RDPLoginIP.StartService("AVCTP service");
            Close();
        }

        private void frmProcess_Load(object sender, EventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (txtService.Text.ToUpper() == "N/A")
            {
                MessageBox.Show("The process is not a service\r\nTry kill the process it instead");
                return;
            }
            RDPLoginIP.ServiceChangeState state = RDPLoginIP.StopService(txtService.Text);
            if (state == RDPLoginIP.ServiceChangeState.AccessDenied)
            {
                MessageBox.Show("Program needs elevated permission");
                return;
            }
            if (state == RDPLoginIP.ServiceChangeState.IsNotStarted || state == RDPLoginIP.ServiceChangeState.Success)
            {
                bool result = RDPLoginIP.DisableService(txtService.Text);
                if (result)
                {
                    MessageBox.Show("Service '" + txtService.Text + "' is stopped and disabled");
                    return;
                }
                else
                {
                    MessageBox.Show("Service '" + txtService.Text + "' is stopped but unable to be disabled");
                    return;
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            RDPLoginIP.TaskKillState state = RDPLoginIP.KillProcess(txtPID.Text); 
            if (state == RDPLoginIP.TaskKillState.Success)
            {
                MessageBox.Show("Process is killed");
                return;
            }
            if (state == RDPLoginIP.TaskKillState.NotFound)
            {
                MessageBox.Show("Process has ended before kill");
                return;
            }
            if (state == RDPLoginIP.TaskKillState.AccessDenied)
            {
                MessageBox.Show("Programs need elevated permission or process access is denied");
                return;
            }
            MessageBox.Show("Fail killing process");
            return;
        }
    }
}