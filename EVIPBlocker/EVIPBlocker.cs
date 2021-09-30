using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.IO;
using NetFwTypeLib;
using System.Threading;

namespace EVIPBlocker
{
    public class EVIPBlockerMain
    {
        public bool IsRunning = false;
        public static object BlockedFileLock = new object();
        public static object LogFileLock = new object();
        public Thread BlockerThread = null;
        public Thread RemoverThread = null;
        public static Action<string> WriteLog = null;
        public string IPRegex = @"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b";
        public static void Log(string str)
        {
            if (WriteLog != null)
                WriteLog(str);
        }

        public bool Sleep(int seconds)
        {
            DateTime last = DateTime.Now;
            TimeSpan ts = DateTime.Now - last;
            do
            {
                if (IsRunning == false) return false;
                ts = DateTime.Now - last;
                Thread.Sleep(500);
            } while (ts.TotalSeconds < seconds);
            return true;
        }

        public void Confirm()
        {
            ThreadStart ts = new ThreadStart(ConfirmReal);
            Thread t = new Thread(ts);
            t.Start();
        }

        public static void CheckAddRuleToFile(string ip)
        {
            string blockedfile = System.Configuration.ConfigurationManager.AppSettings["BlockedFile"];
            string dir = Path.GetDirectoryName(blockedfile);
            if (Directory.Exists(dir) == false) Directory.CreateDirectory(dir);
            File.AppendAllLines(blockedfile, new string[] { ip + "|" + DateTime.Now.ToString() });
        }

        public void ConfirmReal()
        {
            List<string> alreadyBlocked = new List<string>();
            List<string> alreadyBlockedTemp = new List<string>();

            lock (BlockedFileLock)
            {
                string blockedfile = System.Configuration.ConfigurationManager.AppSettings["BlockedFile"];
                string dir = Path.GetDirectoryName(blockedfile);
                if (Directory.Exists(dir) == false) Directory.CreateDirectory(dir);
                if (File.Exists(blockedfile))
                {
                    alreadyBlockedTemp = File.ReadAllLines(blockedfile).ToList();
                    foreach (string s in alreadyBlockedTemp)
                    {
                        string[] strs = s.Split(new char[] { '|' });
                        if (strs.Length != 2) continue;
                        alreadyBlocked.Add(strs[0]);
                    }
                }
            }

            INetFwPolicy2 firewallPolicy = (INetFwPolicy2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));
            foreach (string b in alreadyBlocked)
            {
                bool exist = false;

                foreach (var t in firewallPolicy.Rules)
                {
                    if (t is INetFwRule2)
                    {
                        INetFwRule2 tt = (INetFwRule2)t;

                        if (tt.Name == "EVIPBlocker: IP Access Block " + b)
                        {
                            exist = true;
                            break;
                        }
                    }
                }
                if (exist == false) CheckAddRule(b, "Confirm");
            }

            Log("Confirm : Successfully confirm all IPs from File is blocked in Firewall");
        }

        public void Start()
        {
            IsRunning = true;

            string rdpport = System.Configuration.ConfigurationManager.AppSettings["RDPPort"] ?? "3389";
            var os = RDPLoginIP.GetServerConnection(GetSwap()).Where(m => m.DestinationPort == rdpport).ToList();
            var sources = os.Select(m => m.SourceIP).ToList();
            sources.AddRange(os.Select(m => m.DestinationIP).ToList());
            
            Log("RDP Client IP : " + string.Join("|", sources));
            string set = (System.Configuration.ConfigurationManager.AppSettings["ExcludeInteractiveIP"] ?? "").ToString().ToUpper();
            if (set == "TRUE" || set == "1" || set == "Y") 
                Log("Excluding Interactive RDP IP Mode");
            else 
                Log("Including Interactive RDP IP Mode");

            ThreadStart ts = new ThreadStart(Blocker);
            BlockerThread = new Thread(ts);
            BlockerThread.Start();

            string removeblock = System.Configuration.ConfigurationManager.AppSettings["RemoveBlock"];
            if (string.IsNullOrEmpty(removeblock) == false && removeblock.Trim().ToUpper() == "Y")
            {
                ThreadStart ts2 = new ThreadStart(Remover);
                RemoverThread = new Thread(ts2);
                RemoverThread.Start();
            }
        }

        public void Stop()
        {
            IsRunning = false;
            Thread.Sleep(500);
            if (BlockerThread != null)
            {
                try
                {
                    //BlockerThread.Abort();
                }
                catch { }
            }
            if (RemoverThread != null)
            {
                try
                {
                    //RemoverThread.Abort();
                }
                catch { }
            }
        }

        public string GetWorkstation(EventLogEntry CurrentEntry)
        {
            int i = CurrentEntry.Message.IndexOf("Workstation Name:");
            if (i >= 0)
            {
                int j = CurrentEntry.Message.IndexOf("\r", i);
                int k = CurrentEntry.Message.IndexOf("\n", i);
                if (k < j) j = k;
                if (j >= 0) return CurrentEntry.Message.Substring(i + 17, j - i - 17).Trim();
            }
            return string.Empty;
        }

        public List<EventDetails> GetSuccessUnsuccessLogin()
        {
            List<EventDetails> result = new List<EventDetails>();
            string[] logtypes = System.Configuration.ConfigurationManager.AppSettings["LogTypes"].Split(new char[] { '|' });
            string[] successtypes = System.Configuration.ConfigurationManager.AppSettings["SuccessTypes"].Split(new char[] { '|' });
            
            List<EventDetails> evLists = new List<EventDetails>();
            //Fails types
            foreach (string logtype in logtypes)
            {
                string[] chars = logtype.Split(new char[] { '=' });
                if (chars == null || chars.Length < 3) continue;

                try
                {
                    EventLog ev = new EventLog(chars[0], System.Environment.MachineName);
                    for (int i = 0; i < ev.Entries.Count; i++)
                    {
                        EventLogEntry CurrentEntry = ev.Entries[i];
                        if (CurrentEntry.TimeGenerated >= DateTime.Now.AddDays(-1 * 7))
                        {
                            if (CurrentEntry.EventID.ToString() != chars[1]) continue;
                            {
                                Regex rg = new Regex(IPRegex);
                                var matches = rg.Matches(CurrentEntry.Message);
                                if (matches.Count <= 0) continue;

                                if (CurrentEntry.Message.Contains(chars[2]))
                                {
                                    string ipToAdd = matches[matches.Count - 1].Value;
                                    evLists.Add(new EventDetails() { data = CurrentEntry.Message, dateAdded = CurrentEntry.TimeGenerated, ip = ipToAdd, source = CurrentEntry.Source, type = "Fail", workstation = GetWorkstation(CurrentEntry) });
                                }

                            }
                        }
                    }
                }
                catch(Exception ex)
                {
                    Log(ex.Message + "\r\n" + ex.StackTrace);
                }
            }

            //Success types
            foreach (string logtype in successtypes)
            {
                string[] chars = logtype.Split(new char[] { '=' });
                if (chars == null || chars.Length < 3) continue;

                try
                {
                    EventLog ev = new EventLog(chars[0], System.Environment.MachineName);
                    for (int i = 0; i < ev.Entries.Count; i++)
                    {
                        EventLogEntry CurrentEntry = ev.Entries[i];
                        if (CurrentEntry.TimeGenerated >= DateTime.Now.AddDays(-1 * 7))
                        {
                            if (CurrentEntry.EventID.ToString() != chars[1]) continue;
                            {
                                //if (CurrentEntry.Message.Contains("194.5.49.34"))
                                //    File.AppendAllText("a.txt", "enter " + chars[1]);
                                Regex rg = new Regex(IPRegex);
                                var matches = rg.Matches(CurrentEntry.Message);
                                if (matches.Count <= 0)
                                {
                                    continue;
                                }

                                if (CurrentEntry.Message.ToLower().Contains(chars[2].ToLower()))
                                {
                                    string ipToAdd = matches[matches.Count - 1].Value;
                                    evLists.Add(new EventDetails() { data = CurrentEntry.Message, dateAdded = CurrentEntry.TimeGenerated, ip = ipToAdd, source = CurrentEntry.Source, type = "Success", workstation = GetWorkstation(CurrentEntry) });
                                }
                                else
                                {
                                    //if (CurrentEntry.Message.Contains("194.5.49.34"))
                                    //   File.AppendAllText("a.txt", CurrentEntry.Message+"\r\n\r\n");
                                }
                            }
                        }
                    }
                }
                catch(Exception ex)
                {
                    Log(ex.Message + "\r\n" + ex.StackTrace);
                }
            }
            return evLists;
        }
        void Blocker()
        {
            Log("Blocker thread running...");
            WriteFile("[" + DateTime.Now.ToString("dd MMM yyyy HH:mm:ss") + "] Blocker thread running...");

            int minutesScan = 15;
            if (int.TryParse(System.Configuration.ConfigurationManager.AppSettings["MinutesScan"], out minutesScan) == false)
                Log("   MinutesScan config is not a number, use default " + minutesScan.ToString() + " minutes");

            int numberThreshhold = 15;
            if (int.TryParse(System.Configuration.ConfigurationManager.AppSettings["Threshold"], out numberThreshhold) == false)
                Log("   Threshold config is not a number, use default " + numberThreshhold.ToString() + " times");

            string temp = System.Configuration.ConfigurationManager.AppSettings["IPRegex"];
            if (string.IsNullOrEmpty(temp) == false) IPRegex = temp;
            else Log("   IPRegex is empty, use default");

            do
            {
                EventLog ev = null;
                try
                {
                    List<string> alreadyBlocked = new List<string>();
                    List<string> alreadyBlockedTemp = new List<string>();
                    lock (BlockedFileLock)
                    {
                        string blockedfile = System.Configuration.ConfigurationManager.AppSettings["BlockedFile"];
                        string dir = Path.GetDirectoryName(blockedfile);
                        if (Directory.Exists(dir) == false) Directory.CreateDirectory(dir);
                        if (File.Exists(blockedfile))
                        {
                            alreadyBlockedTemp = File.ReadAllLines(blockedfile).ToList();
                            foreach(string s in alreadyBlockedTemp)
                            {
                                string[] strs = s.Split(new char[] { '|' });
                                if (strs.Length != 2) continue;
                                alreadyBlocked.Add(strs[0]);
                            }
                        }

                        List<string> newBlocked = new List<string>();

                        int countBlocked = 0;
                        string[] logtypes = System.Configuration.ConfigurationManager.AppSettings["LogTypes"].Split(new char[] { '|' });
                        foreach (string l in logtypes)
                        {
                            string[] log = l.Split(new char[] { '=' });
                            if (log.Length != 4) continue;

                            long eventid = 4625;
                            if (long.TryParse(log[1], out eventid) == false)
                            {
                                Log(DateTime.Now.ToString("dd MMM yyyy HH:mm:ss") + "EventID Configuration should be number : " + log[1]);
                                WriteFile("EventID Configuration should be number : " + log[1]);
                            }
                            string logType = log[0];
                            string apptype = log[3];

                            Log("   Scanning " + apptype);

                            ev = new EventLog(logType, System.Environment.MachineName);
                            int LastLogToShow = ev.Entries.Count;
                            if (LastLogToShow <= 0)
                                WriteFile("[" + DateTime.Now.ToString("dd MMM yyyy HH:mm:ss") + "]   No Event Logs in the Log :" + logType);
                            else
                                WriteFile("[" + DateTime.Now.ToString("dd MMM yyyy HH:mm:ss") + "]   LogType :" + logType + " Count : " + ev.Entries.Count);

                            List<EventDetails> evLists = new List<EventDetails>();
                            for (int i = 0; i < ev.Entries.Count; i++)
                            {
                                EventLogEntry CurrentEntry = ev.Entries[i];
                                if (CurrentEntry.EventID == eventid)
                                {
                                    if (CurrentEntry.TimeGenerated >= DateTime.Now.AddMinutes(-1 * minutesScan))
                                    {
                                        if (CurrentEntry.Message.Contains(l[2]))
                                        {
                                            Regex rg = new Regex(IPRegex);
                                            var matches = rg.Matches(CurrentEntry.Message);
                                            if (matches.Count >= 1)
                                            {
                                                string ipToAdd = matches[matches.Count - 1].Value;
                                                evLists.Add(new EventDetails() { data = CurrentEntry.Message, dateAdded = CurrentEntry.TimeGenerated, ip = ipToAdd, source = CurrentEntry.Source, type = "Fail", workstation = GetWorkstation(CurrentEntry) });
                                                WriteFile("[" + DateTime.Now.ToString("dd MMM yyyy HH:mm:ss") + "]   [" + apptype + "] IP: " + ipToAdd + "");
                                            }
                                        }
                                    }
                                }
                            }

                            var numberGroups = evLists
                            .GroupBy(n => n.ip)
                            .Select(n => new
                            {
                                Ip = n.Key,
                                Count = n.Count()
                            }
                            )
                            .OrderBy(n => n.Ip);

                            foreach (var evs in numberGroups)
                            {
                                WriteFile("[" + DateTime.Now.ToString("dd MMM yyyy HH:mm:ss") + "]   IP: " + evs.Ip + " count : " + evs.Count);

                                if (evs.Count >= numberThreshhold)
                                {
                                    if (alreadyBlocked.Contains(evs.Ip))
                                    {
                                        WriteFile("[" + DateTime.Now.ToString("dd MMM yyyy HH:mm:ss") + "]   [" + apptype + "] IP: " + evs.Ip + " Count : " + evs.Count + " already blocked list");
                                    }
                                    else
                                    {
                                        try
                                        {
                                            countBlocked++;
                                            bool b = CheckAddRule(evs.Ip, log[3]); //evs.Grouper.Count()
                                            if (b)
                                            {
                                                newBlocked.Add(evs.Ip + "|" + DateTime.Now.ToString());
                                                Log("   [" + apptype + "] Blocking IP: " + evs.Ip + " Count : " + evs.Count);
                                                WriteFile("[" + DateTime.Now.ToString("dd MMM yyyy HH:mm:ss") + "]   [" + apptype + "] Blocking IP: " + evs.Ip + " Count : " + evs.Count);
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Log("   [" + apptype + "] Fail Blocking IP: " + evs.Ip + " Count : " + evs.Count + " Message : " + ex.Message);
                                            WriteFile("[" + DateTime.Now.ToString("dd MMM yyyy HH:mm:ss") + "]   [" + apptype + "] Fail Blocking IP: " + evs.Ip + " Count : " + evs.Count + " Message : " + ex.Message);
                                        }
                                    }
                                }
                            }
                            ev.Close();
                        }

                        WriteFile("[" + DateTime.Now.ToString("dd MMM yyyy HH:mm:ss") + "]   Done, newly blocked: " + countBlocked);
                        
                        File.AppendAllLines(blockedfile, newBlocked);
                    }

                    Log("   Blocker thread - Sleep for " + minutesScan.ToString() + " minutes until next scan");
                    WriteFile("[" + DateTime.Now.ToString("dd MMM yyyy HH:mm:ss") + "]   Blocker thread - Sleep for " + minutesScan.ToString() + " minutes until next scan");
                    if (Sleep(minutesScan * 60) == false) break;
                }
                catch(Exception ex)
                {
                    WriteFile("[" + DateTime.Now.ToString("dd MMM yyyy HH:mm:ss") + "]   Blocker thread - Exception :" + ex.Message);
                    Log("   Blocker thread - Exception :" + ex.Message);
                    if (Sleep(minutesScan * 60) == false) break;
                }
                finally
                {
                    try
                    {
                        if (ev != null)
                            ev.Close();
                    }
                    catch { }
                }
            } while (IsRunning);
            Log("Blocker Thread Stopped");
            WriteFile("[" + DateTime.Now.ToString("dd MMM yyyy HH:mm:ss") + "] Blocker Thread Stopped");
        }

        public static void GetBlocked(List<RemoverDetail> alreadyBlocked, List<string> alreadyBlockedTemp)
        {
            string blockedfile = System.Configuration.ConfigurationManager.AppSettings["BlockedFile"];
            if (File.Exists(blockedfile))
            {
                alreadyBlockedTemp = File.ReadAllLines(blockedfile).ToList();
                foreach (string s in alreadyBlockedTemp)
                {
                    string[] strs = s.Split(new char[] { '|' });
                    if (strs.Length != 2) continue;
                    try
                    {
                        RemoverDetail rd = new RemoverDetail();
                        rd.IP = strs[0];
                        rd.AddedTime = DateTime.Parse(strs[1]);
                        alreadyBlocked.Add(rd);
                    }
                    catch { }
                }
            }
        }

        void Remover()
        {
            Log("Remover thread running...");
            WriteFile("[" + DateTime.Now.ToString("dd MMM yyyy HH:mm:ss") + "] Remover thread running...");

            int removeAfterHour = 72;
            if (int.TryParse(System.Configuration.ConfigurationManager.AppSettings["RemoveAfterHour"], out removeAfterHour) == false)
            {
                Log("   RemoveAfterHour config is not a number, use default " + removeAfterHour.ToString() + " hours");
                WriteFile("   RemoveAfterHour config is not a number, use default " + removeAfterHour.ToString() + " hours");
            }

            do
            {
                try
                {
                    List<RemoverDetail> alreadyBlocked = new List<RemoverDetail>();
                    List<string> alreadyBlockedTemp = new List<string>();
                    lock (BlockedFileLock)
                    {
                        string blockedfile = System.Configuration.ConfigurationManager.AppSettings["BlockedFile"];
                        GetBlocked(alreadyBlocked, alreadyBlockedTemp);

                        List<string> newBlockedFile = new List<string>();
                        for(int i=alreadyBlocked.Count - 1; i>=0; i--)
                        {
                            RemoverDetail l = alreadyBlocked[i];
                            TimeSpan ts = DateTime.Now - l.AddedTime;
                            if (ts.TotalHours >= removeAfterHour)
                            {
                                try
                                {
                                    RemoveRule(l);
                                    Log("   Remove Blocked : " + l.IP + " Already pass : " + ts.TotalHours + " hours");
                                    WriteFile("[" + DateTime.Now.ToString("dd MMM yyyy HH:mm:ss") + "]   Remove Blocked : " + l.IP + " Already pass : " + ts.TotalHours + " hours");
                                }
                                catch(Exception ex)
                                {
                                    Log("   Fail Remove Blocked : " + l.IP + " Already pass : " + ts.TotalHours + " hours Message : " + ex.Message);
                                    WriteFile("[" + DateTime.Now.ToString("dd MMM yyyy HH:mm:ss") + "]   Fail Remove Blocked : " + l.IP + " Already pass : " + ts.TotalHours + " hours Message : " + ex.Message);
                                }
                            }
                            else
                            {
                                newBlockedFile.Add(l.IP + "|" + l.AddedTime.ToString());
                            }
                        }

                        Log("   Remover thread - Sleep for 1 hour until next scan");
                        WriteFile("[" + DateTime.Now.ToString("dd MMM yyyy HH:mm:ss") + "]   Remover thread - Sleep for 1 hour until next scan");
                        File.Delete(blockedfile);
                        File.AppendAllLines(blockedfile, newBlockedFile);
                    }
                    if (Sleep(60 * 60) == false) break;
                }
                catch (Exception ex)
                {
                    WriteFile("   [" + DateTime.Now.ToString("dd MMM yyyy HH:mm:ss") + "] Remover thread - Exception :" + ex.Message);
                    Log("   Remover thread - Exception :" + ex.Message);
                    if (Sleep(60) == false) break;
                }
            } while (IsRunning);
            Log("Remover Thread Stopped");
            WriteFile("[" + DateTime.Now.ToString("dd MMM yyyy HH:mm:ss") + "] Remover Thread Stopped");
        }

        public static bool GetExcludeInteractiveIP()
        {
            string set = (System.Configuration.ConfigurationManager.AppSettings["ExcludeInteractiveIP"] ?? "").ToString().ToUpper();
            if (set == "TRUE" || set == "1" || set == "Y")
                return true;
            return false;
        }

        public static bool GetSwap()
        {
            string set = (System.Configuration.ConfigurationManager.AppSettings["Swap"] ?? "").ToString().ToUpper();
            if (set == "TRUE" || set == "1" || set == "Y")
                return true;
            return false;
        }

        public static bool CheckAddRule(string ip, string type)
        {
            if (GetExcludeInteractiveIP())
            { 
                string rdpport = System.Configuration.ConfigurationManager.AppSettings["RDPPort"] ?? "3389";
                var os = RDPLoginIP.GetServerConnection(GetSwap()).Where(m => m.DestinationPort == rdpport).ToList();
                var sources = os.Select(m => m.SourceIP).ToList();
                sources.AddRange(os.Select(m => m.DestinationIP).ToList());

                if (sources.Contains(ip))
                {
                    Log("Fail Add Rule : IP Address " + ip + " is current Remote Desktop IP");
                    return false;
                }
            }
            
            return addRule(ip, type);
        }

        public static bool addRule(string ip, string type)
        {
            INetFwRule2 firewallRule = (INetFwRule2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FWRule"));

            firewallRule.Name = "EVIPBlocker: IP Access Block " + ip;
            firewallRule.Description = "[" + type + "] Block Incoming Connections from IP Address.";
            firewallRule.Action = NET_FW_ACTION_.NET_FW_ACTION_BLOCK;
            firewallRule.Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN;
            firewallRule.Enabled = true;
            firewallRule.InterfaceTypes = "All";
            firewallRule.RemoteAddresses = ip;

            INetFwPolicy2 firewallPolicy = (INetFwPolicy2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));
            firewallPolicy.Rules.Add(firewallRule);
            return true;
        }

        public static void RemoveRule(RemoverDetail l)
        {
            string name = "EVIPBlocker: IP Access Block " + l.IP;
            string name2 = "BruteForceBlocker: IP Access Block " + l.IP;
            INetFwPolicy2 firewallPolicy = (INetFwPolicy2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));
            firewallPolicy.Rules.Remove(name);
            try
            {
                firewallPolicy.Rules.Remove(name2);
            }
            catch { }
        }

        public static void WriteFile(string str)
        {
            lock(LogFileLock)
            {
                string fileName = System.Configuration.ConfigurationManager.AppSettings["LogFile"] + DateTime.Now.ToString("dd_MMM_yyyy") + ".txt";
                string dir = Path.GetDirectoryName(fileName);
                if (Directory.Exists(dir) == false) Directory.CreateDirectory(dir);

                FileStream fs = null;
                StreamWriter sw = null;

                try
                {
                    if (File.Exists(fileName))
                        fs = new FileStream(fileName, FileMode.Append);
                    else
                        fs = new FileStream(fileName, FileMode.CreateNew);
                    sw = new StreamWriter(fs);
                    sw.WriteLine(str);
                }
                catch (Exception ex)
                {
                    Log("Error writing log : " + ex.Message);
                }
                finally
                {
                    if (sw != null) sw.Close();
                    if (fs != null) fs.Close();
                }
            }
        }
    }
}
