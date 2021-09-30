using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class RDPLoginIP
{
    #region Constants
    public const int WTS_CURRENT_SESSION = -1;
    #endregion

    #region Dll Imports
    [DllImport("wtsapi32.dll")]
    static extern int WTSEnumerateSessions(
        IntPtr pServer,
        [MarshalAs(UnmanagedType.U4)] int iReserved,
        [MarshalAs(UnmanagedType.U4)] int iVersion,
        ref IntPtr pSessionInfo,
        [MarshalAs(UnmanagedType.U4)] ref int iCount);

    [DllImport("Wtsapi32.dll")]
    public static extern bool WTSQuerySessionInformation(
        System.IntPtr pServer,
        int iSessionID,
        WTS_INFO_CLASS oInfoClass,
        out System.IntPtr pBuffer,
        out uint iBytesReturned);

    [DllImport("wtsapi32.dll")]
    static extern void WTSFreeMemory(
        IntPtr pMemory);
    #endregion

    #region Structures
    //Structure for Terminal Service Client IP Address
    [StructLayout(LayoutKind.Sequential)]
    private struct WTS_CLIENT_ADDRESS
    {
        public int iAddressFamily;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public byte[] bAddress;
    }

    //Structure for Terminal Service Session Info
    [StructLayout(LayoutKind.Sequential)]
    private struct WTS_SESSION_INFO
    {
        public int iSessionID;
        [MarshalAs(UnmanagedType.LPStr)]
        public string sWinsWorkstationName;
        public WTS_CONNECTSTATE_CLASS oState;
    }

    //Structure for Terminal Service Session Client Display
    [StructLayout(LayoutKind.Sequential)]
    private struct WTS_CLIENT_DISPLAY
    {
        public int iHorizontalResolution;
        public int iVerticalResolution;
        //1 = The display uses 4 bits per pixel for a maximum of 16 colors.
        //2 = The display uses 8 bits per pixel for a maximum of 256 colors.
        //4 = The display uses 16 bits per pixel for a maximum of 2^16 colors.
        //8 = The display uses 3-byte RGB values for a maximum of 2^24 colors.
        //16 = The display uses 15 bits per pixel for a maximum of 2^15 colors.
        public int iColorDepth;
    }
    #endregion

    #region Enumurations
    public enum WTS_CONNECTSTATE_CLASS
    {
        WTSActive,
        WTSConnected,
        WTSConnectQuery,
        WTSShadow,
        WTSDisconnected,
        WTSIdle,
        WTSListen,
        WTSReset,
        WTSDown,
        WTSInit
    }

    public enum WTS_INFO_CLASS
    {
        WTSInitialProgram,
        WTSApplicationName,
        WTSWorkingDirectory,
        WTSOEMId,
        WTSSessionId,
        WTSUserName,
        WTSWinStationName,
        WTSDomainName,
        WTSConnectState,
        WTSClientBuildNumber,
        WTSClientName,
        WTSClientDirectory,
        WTSClientProductId,
        WTSClientHardwareId,
        WTSClientAddress,
        WTSClientDisplay,
        WTSClientProtocolType,
        WTSIdleTime,
        WTSLogonTime,
        WTSIncomingBytes,
        WTSOutgoingBytes,
        WTSIncomingFrames,
        WTSOutgoingFrames,
        WTSClientInfo,
        WTSSessionInfo,
        WTSConfigInfo,
        WTSValidationInfo,
        WTSSessionAddressV4,
        WTSIsRemoteSession
    }
    #endregion

    public enum ServiceChangeState
    {
        Success, Fail, IsNotStarted, IsRunnning, AccessDenied
    }

    public enum TaskKillState
    {
        NotFound, Success, Fail, AccessDenied
    }

    public class ServerProcessObject
    {
        public string PID { get; set; }
        public string File { get; set; }
        public string FullPath { get; set; }
        public string CommandLine { get; set; }
        public string Service { get; set; }

        public void Refresh()
        {
            List<ServerProcessObject> spo = RDPLoginIP.GetServerProcess(PID);
            if (spo != null && spo.Count > 0)
            {
                PID = spo[0].PID;
                File = spo[0].File;
                FullPath = spo[0].FullPath;
                CommandLine = spo[0].CommandLine;
                Service = spo[0].Service;
            }
        }
    }

    public class ServerConnection
    {
        public string TcpUdp { get; set; }
        public string SourceIP { get; set; }
        public string SourcePort { get; set; }
        public string DestinationIP { get; set; }
        public string DestinationPort { get; set; }
        public string ConnectionState { get; set; }
        public string PID { get; set; }

        private ServerProcessObject _Process;
        public ServerProcessObject Process 
        {
            get
            {
                if (_Process == null)
                {
                    var t = RDPLoginIP.GetServerProcess(PID);
                    if (t != null && t.Count > 0)
                        _Process = t[0];
                }
                if (_Process != null && (string.IsNullOrEmpty(_Process.CommandLine) || string.IsNullOrEmpty(_Process.FullPath)))
                    RDPLoginIP.GetProcessPath(_Process);
                return _Process;
            }
            set
            {
                _Process = value;
            }
        }
    }

    public static List<string> LastResult = new List<string>();
    
    public static ServiceChangeState StopService(string servicename)
    {
        List<ServerProcessObject> result = new List<ServerProcessObject>();

        try
        {
            string output = RunCommand("net stop \"" + servicename + "\"");
            if (output.Contains("successfully")) return ServiceChangeState.Success;
            if (output.Contains("is not started")) return ServiceChangeState.IsNotStarted;
            if (output.Contains("access is denied")) return ServiceChangeState.AccessDenied;
        }
        catch { }
        return ServiceChangeState.Fail;
    }

    public static ServiceChangeState StartService(string servicename)
    {
        List<ServerProcessObject> result = new List<ServerProcessObject>();

        try
        {
            string output = RunCommand("net start \"" + servicename + "\"");
            if (output.Contains("successfully")) return ServiceChangeState.Success;
            if (output.Contains("has already been started")) return ServiceChangeState.IsRunnning;
            if (output.Contains("access is denied")) return ServiceChangeState.AccessDenied;
        }
        catch { }
        return ServiceChangeState.Fail;
    }

    public static bool DisableService(string servicename)
    {
        List<ServerProcessObject> result = new List<ServerProcessObject>();

        try
        {
            string output = RunCommand("sc config \"" + servicename + "\" start= disabled");
            if (output.Contains("successfully")) return true;
            
        }
        catch { }
        return true;
    }

    public static bool EnableService(string servicename)
    {
        List<ServerProcessObject> result = new List<ServerProcessObject>();

        try
        {
            string output = RunCommand("sc config \"" + servicename + "\" start= auto");
            if (output.Contains("successfully")) return true;
        }
        catch { }
        return false;
    }

    public static TaskKillState KillProcess(string pid)
    {
        try
        {
            string output = RunCommand("taskkill /F /T /PID " + pid);
            if (output.Contains("SUCCESS:")) return TaskKillState.Success;
            if (output.Contains("Access denied")) return TaskKillState.AccessDenied;
            if (output.Contains("not found")) return TaskKillState.NotFound;
        }
        catch { }
        return TaskKillState.Fail;
    }

    public static void GetProcessPath(ServerProcessObject spo)
    {
        try
        {
            string output = RunCommand("wmic process where \"processid='" + spo.PID + "'\" get CommandLine, ProcessID, ExecutablePath /FORMAT:LIST");

            string text2 = output;
            string[] lines = text2.Split(new char[] { '\n' });
            bool start = false;
            string exe = string.Empty;
            foreach (string line in lines)
            {
                string l = line;
                l = l.Trim();
                if (string.IsNullOrEmpty(l)) continue;
                if (l.StartsWith("ExecutablePath=")) start = true;
                else if (l.IndexOf("=") > 0) start = false;
                if (start)
                {
                    exe += l;
                }
            }
            spo.FullPath = exe;
            if (spo.FullPath.StartsWith("ExecutablePath=")) spo.FullPath = spo.FullPath.Substring(15);

            start = false;
            exe = string.Empty;
            foreach (string line in lines)
            {
                string l = line;
                l = l.Trim();
                if (string.IsNullOrEmpty(l)) continue;
                if (l.StartsWith("CommandLine=")) start = true;
                else if (l.IndexOf("=") > 0) start = false;
                if (start)
                {
                    exe += l;
                }
            }
            spo.CommandLine = exe;
            if (spo.CommandLine.StartsWith("CommandLine=")) spo.CommandLine = spo.CommandLine.Substring(12);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    public static List<ServerProcessObject> GetServerProcess(string pid = "")
    {
        List<ServerProcessObject> result = new List<ServerProcessObject>();
        
        try
        {
            string output = string.Empty;
            if (string.IsNullOrEmpty(pid) == false)
                output = RunCommand("tasklist /svc | find \"" + pid + "\"");
            else
                output = RunCommand("tasklist /svc");

            string text2 = output;
            string[] lines = text2.Split(new char[] { '\n' });
            bool start = true;
            foreach (string line in lines)
            {
                string l = line;
                l = l.Trim();
                if (start)
                {
                    ServerProcessObject scx = new ServerProcessObject();

                    string col1 = l.Substring(0, 29).Trim();
                    string col2 = l.Substring(29, 5).Trim();
                    string col3 = l.Substring(35).Trim();

                    scx.PID = col2;
                    scx.File = col1;
                    scx.Service = col3;

                    GetProcessPath(scx);
                    result.Add(scx);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

        if (string.IsNullOrEmpty(pid) == false)
        {
            result = result.Where(m => m.PID == pid).ToList();
        }
        return result;
    }

    public static List<ServerConnection> GetServerConnection(bool swap = false, string pid = "")
    {
        List<ServerConnection> result = new List<ServerConnection>();
        
        try
        {
            string output = string.Empty;
            if (string.IsNullOrEmpty(pid)) output = RunCommand("netstat -ano");
            else output = RunCommand("netstat -ano | find \"" + pid + "\"");

            string text2 = output;
            string[] lines = text2.Split(new char[] { '\n' });
            foreach (string line in lines)
            {
                string l = line;
                while(l.IndexOf("  ") >= 0)
                {
                    l = l.Replace("  ", " ");
                }
                while(l.IndexOf("\t\t") >= 0)
                {
                    l = l.Replace("\t\t", " ");
                }
                l = l.Trim();
                ServerConnection sc = new ServerConnection();
                string[] cols = l.Split(new char[] { ' ' });
                if (cols.Length > 3 && cols[0] != "Proto")
                {
                    sc.TcpUdp = cols[0].Trim();

                    string sip = cols[2].Trim();
                    int index = sip.LastIndexOf(":");
                    if (index > 0)
                    {
                        sc.SourceIP = sip.Substring(0, index);
                        sc.SourcePort = sip.Substring(index + 1);
                    }

                    if (swap == false)
                    {
                        if (sc.SourceIP == "127.0.0.1") continue;
                        if (sc.SourceIP == "0.0.0.0") continue;
                        if (sc.SourceIP == "[::]") continue;
                        if (sc.SourceIP == "[::1]") continue;
                        if (sc.SourceIP == "*:*") continue;
                        if (sc.SourceIP == "*") continue;
                    }
                    else
                    {
                        if (sc.DestinationIP == "127.0.0.1") continue;
                        if (sc.DestinationIP == "0.0.0.0") continue;
                        if (sc.DestinationIP == "[::]") continue;
                        if (sc.DestinationIP == "[::1]") continue;
                        if (sc.DestinationIP == "*:*") continue;
                        if (sc.DestinationIP == "*") continue;
                    }

                    string dip = cols[1].Trim();
                    index = dip.LastIndexOf(":");
                    if (index > 0)
                    {
                        sc.DestinationIP = dip.Substring(0, index);
                        sc.DestinationPort = dip.Substring(index + 1);
                    }

                    if (swap)
                    {
                        string ip = string.Empty;
                        string port = string.Empty;
                        ip = sc.DestinationIP;
                        port = sc.DestinationPort;
                        sc.DestinationIP = sc.SourceIP;
                        sc.DestinationPort = sc.SourcePort;
                        sc.SourceIP = ip;
                        sc.SourcePort = port;
                    }
                    
                    sc.ConnectionState = cols[3].Trim();
                    sc.PID = cols[4].Trim();
                    result.Add(sc);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

        List<ServerProcessObject> procs = RDPLoginIP.GetServerProcess();
        foreach(ServerConnection sc in result)
        {
            ServerProcessObject spo = procs.Where(m => m.PID == sc.PID).FirstOrDefault();
            sc.Process = spo;
        }

        if (string.IsNullOrEmpty(pid) == false)
        {
            result = result.Where(m => m.PID == pid).ToList();
        }
        //var oo = result.Where(m => m.SourceIP.Contains("[")).ToList();
        return result;
    }

    public static List<string> GetRDPClientIPs(bool swap = false)
    {
        string rdpport = System.Configuration.ConfigurationManager.AppSettings["RDPPort"] ?? "3389";
        List<RDPLoginIP.ServerConnection> objs = GetServerConnection(swap);
        List<RDPLoginIP.ServerConnection> ts = null;
        if (swap == false) ts = objs.Where(m => m.DestinationPort == rdpport).ToList();
        else ts = objs.Where(m => m.SourcePort == rdpport).ToList();

        List<string> result = null;
        if (swap == false) result = ts.Select(m => m.SourceIP).ToList();
        else result = ts.Select(m => m.DestinationIP).ToList();
        return result;
    }

    public static List<string> GetRDPServerIPs(bool swap = false)
    {
        string rdpport = System.Configuration.ConfigurationManager.AppSettings["RDPPort"] ?? "3389";
        List<RDPLoginIP.ServerConnection> objs = GetServerConnection(swap);
        List<RDPLoginIP.ServerConnection> ts = null;
        if (swap == false) ts = objs.Where(m => m.DestinationPort == rdpport).ToList();
        else ts = objs.Where(m => m.SourcePort == rdpport).ToList();

        List<string> result = null;
        if (swap == false) result = ts.Select(m => m.DestinationIP).ToList();
        else result = ts.Select(m => m.SourceIP).ToList();
        return result;
    }

    public static string RunCommand(string command)
    {
        List<ServerProcessObject> result = new List<ServerProcessObject>();
        ProcessStartInfo psi = null;
        Process p = null;
        try
        {
            psi = new ProcessStartInfo();
            psi.FileName = "cmd";
            psi.Arguments = "/c " + command;
            psi.CreateNoWindow = true;
            psi.WindowStyle = ProcessWindowStyle.Hidden;
            psi.UseShellExecute = false;
            psi.ErrorDialog = false;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardInput = true;
            psi.RedirectStandardError = true;
            psi.Verb = "runas";

            p = new Process();
            p.EnableRaisingEvents = true;
            p.StartInfo = psi;
            p.Start();

            string output = string.Empty;
            string standard_output = string.Empty;
            while ((standard_output = p.StandardOutput.ReadLine()) != null)
            {
                output += standard_output + "\r\n";
            }

            return output;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        finally
        {
            if (p.StandardOutput != null)
            {
                p.StandardOutput.Close();
                p.StandardOutput.Dispose();
            }
            if (p.StandardError != null)
            {
                p.StandardError.Close();
                p.StandardError.Dispose();
            }
            if (p.StandardInput != null)
            {
                p.StandardInput.Close();
                p.StandardInput.Dispose();
            }
            if (p != null)
            {
                p.Close();
                p.Dispose();
            }
        }
        return string.Empty;
    }

    private static List<string> GetRDPClientIPOld()
    {
        LastResult = new List<string>();
        string fname = "rdp_ip_logs.log";
        //string text = "netstat -n | find \":3389\" | find \"ESTABLISHED\" >> rdp_ip_logs.log";

        ProcessStartInfo psi = null;
        Process p = null;
        try
        {
            if (File.Exists(fname)) File.Delete(fname);
            string rdpport = System.Configuration.ConfigurationManager.AppSettings["RDPPort"] ?? "3389";

            psi = new ProcessStartInfo();
            psi.FileName = "cmd";
            psi.Arguments = "/c netstat -n | find \":" + rdpport + "\" | find \"ESTABLISHED\" >> " + fname;
            psi.CreateNoWindow = true;
            psi.WindowStyle = ProcessWindowStyle.Hidden;
            psi.UseShellExecute = false;
            psi.ErrorDialog = false;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardInput = true;
            psi.RedirectStandardError = true;
            psi.Verb = "runas";

            p = new Process();
            p.EnableRaisingEvents = true;
            p.StartInfo = psi;
            p.Start();

            while (File.Exists(fname) == false)
            {
                System.Threading.Thread.Sleep(100);
            }

            if (File.Exists(fname) == false) return LastResult;
            
            if (File.Exists(fname)) File.Delete(fname);
        }
        catch(Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        finally
        {
            if (p.StandardOutput != null)
            {
                p.StandardOutput.Close();
                p.StandardOutput.Dispose();
            }
            if (p.StandardError != null)
            {
                p.StandardError.Close();
                p.StandardError.Dispose();
            }
            if (p.StandardInput != null)
            {
                p.StandardInput.Close();
                p.StandardInput.Dispose();
            }
            if (p != null)
            {
                p.Close();
                p.Dispose();
            }
        }

        return LastResult;
    }

    private static List<string> GetRDPClientIP()
    {
        List<string> IPs = new List<string>();
        IntPtr pServer = IntPtr.Zero;
        string sUserName = string.Empty;
        string sDomain = string.Empty;
        string sClientApplicationDirectory = string.Empty;
        string sIPAddress = string.Empty;

        WTS_CLIENT_ADDRESS oClientAddres = new WTS_CLIENT_ADDRESS();
        WTS_CLIENT_DISPLAY oClientDisplay = new WTS_CLIENT_DISPLAY();

        IntPtr pSessionInfo = IntPtr.Zero;

        int iCount = 0;
        int iReturnValue = WTSEnumerateSessions(pServer, 0, 1, ref pSessionInfo, ref iCount);
        int iDataSize = Marshal.SizeOf(typeof(WTS_SESSION_INFO));

        int iCurrent = (int)pSessionInfo;

        if (iReturnValue != 0)
        {
            //Go to all sessions
            for (int i = 0; i < iCount; i++)
            {
                WTS_SESSION_INFO oSessionInfo = (WTS_SESSION_INFO)Marshal.PtrToStructure((System.IntPtr)iCurrent, typeof(WTS_SESSION_INFO));
                iCurrent += iDataSize;

                uint iReturned = 0;

                //Get the IP address of the Terminal Services User
                IntPtr pAddress = IntPtr.Zero;
                if (WTSQuerySessionInformation(pServer, oSessionInfo.iSessionID, WTS_INFO_CLASS.WTSClientAddress, out pAddress, out iReturned) == true)
                {
                    oClientAddres = (WTS_CLIENT_ADDRESS)Marshal.PtrToStructure (pAddress, oClientAddres.GetType());
                    sIPAddress = oClientAddres.bAddress[2] + "." + oClientAddres.bAddress[3] + "." + oClientAddres.bAddress[4] + "." + oClientAddres.bAddress[5];
                }
                //Get the User Name of the Terminal Services User
                if (WTSQuerySessionInformation(pServer, oSessionInfo.iSessionID, WTS_INFO_CLASS.WTSUserName, out pAddress, out iReturned) == true)
                {
                    sUserName = Marshal.PtrToStringAnsi(pAddress);
                }
                //Get the Domain Name of the Terminal Services User
                if (WTSQuerySessionInformation(pServer, oSessionInfo.iSessionID, WTS_INFO_CLASS.WTSDomainName, out pAddress, out iReturned) == true)
                {
                    sDomain = Marshal.PtrToStringAnsi(pAddress);
                }
                //Get the Display Information  of the Terminal Services User
                if (WTSQuerySessionInformation(pServer, oSessionInfo.iSessionID, WTS_INFO_CLASS.WTSClientDisplay, out pAddress, out iReturned) == true)
                {
                    oClientDisplay = (WTS_CLIENT_DISPLAY)Marshal.PtrToStructure(pAddress, oClientDisplay.GetType());
                }
                //Get the Application Directory of the Terminal Services User
                if (WTSQuerySessionInformation(pServer, oSessionInfo.iSessionID, WTS_INFO_CLASS.WTSClientDirectory, out pAddress, out iReturned) == true)
                {
                    sClientApplicationDirectory = Marshal.PtrToStringAnsi(pAddress);
                }

                Console.WriteLine("Session ID : " + oSessionInfo.iSessionID);
                Console.WriteLine("Session State : " + oSessionInfo.oState);
                Console.WriteLine("Workstation Name : " + oSessionInfo.sWinsWorkstationName);
                Console.WriteLine("IP Address : " + sIPAddress);
                Console.WriteLine("User Name : " + sDomain + @"\" + sUserName);
                Console.WriteLine("Client Display Resolution: " + oClientDisplay.iHorizontalResolution + " x " + oClientDisplay.iVerticalResolution);
                Console.WriteLine("Client Display Colour Depth: " + oClientDisplay.iColorDepth);
                Console.WriteLine("Client Application Directory: " + sClientApplicationDirectory);

                IPs.Add(sIPAddress);
                Console.WriteLine("-----------------------");
            }

            WTSFreeMemory(pSessionInfo);
        }
        return IPs;
    }
}