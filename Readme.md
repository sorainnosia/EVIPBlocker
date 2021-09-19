# EVIPBlocker

## Screenshot
<img src="https://user-images.githubusercontent.com/19261780/133896978-d341c6c0-273e-45c9-abb4-2e944ca9b9c4.gif" width="300">

## Description
A software for listing incoming/outgoing connections to/from a Windows machine, useful for managing Windows Server allowing operator to monitor active connections and allowing to automatically create a Windows Firewall to block connection from an IP that exceed configurable threshold of login failures.

By Default the software supports reading of Event Viewer to know any failure logon attempt to MS SQL database and Remote Desktop and has settings to add other Event ID which reports failure attempts of other software (as long as the software writes failure attempt into Event Viewer log).

However please note that some misconfiguration of Windows machine will cause this software to unable to read Event Viewer logs such as when the registry does not have permission for Event Log Readers user group. The Setting button has a tab to test reading of all configured Event ID, so users will know whether the software is working as intended.

## Tabs
The LOG tab display current status of logs scanning, and display messages of IP that is blocked (when exist), the condition for blocking is configurable from Setting button that limits attempt of logon failure. The manual tab display successful/unsuccessful login attempt and when necessary user manually click a single IP and select block. 

The RDP IN display current active Remote Desktop connection session to the server running the software, user can click a single IP and click block to block any unauthorised session. 

The NonHTTP tab display any incoming/outgoing connection that is not on port 443/80, the details of the connection can be known by clicking a single IP and click View, it will show the Windows Service, Executable File Path that initiates the connection, it allows Kill of the process and Disable of the Windows Service. The HTTP tab display incoming or outgoing connection to port 443/80, similar to NonHTTP, the View button show the details of the connection and allow Kill/Disable of the process/service.

## Buttons
Start button is clicked when the program runs in order to initiate the first and periodic scanning, the Stop button otherwise stop the scanning.

Any IP address that is blocked will be appended to a file name blocked.txt. When the file is migrated to a new server the firewall rules are not migrated, but clicking the Confirm button will re-create the Firewall rule based on the blocked.txt file, an already existing rule will not be created again.

The Blocked button display all blocked IP Address, you can click an IP and click Remove to re-allow connection from the IP Address.

The Setting button allows you to see program supported via Event ID which by default support reading of Event ID 18456 for failure attempt of MS SQL logon and Event ID 4625 for failure attempt of RDP logon. If the attempt exceed the set Threshold within a scan period configured in Minutes Scan, the IP address will automatically blocked and display in LOG tab. You can add other Event ID for other failure attempt that is logged down in Event Viewer via the Event ID.

## Advanced Configuration
EVIPBlocker.exe.config contains configuration of the RDP Port of the server configured in "RDPPort" value="3389" which is port 3389, you can change it by editing the config file via notepad.

If you notice, the NonHTTP and HTTP tab has a checkbox in every connection, sometimes it is checked and sometimes its not. When it is checked, it means that the File Name of the executable that initiates the connection does not contains any of the word "PHargaService "or "CefSharp" which is configured in "HTTPChecked" value="PHargaService|CefSharp" which is separated by pipe (|), this means the configuration configures allowed File Name keywords that will mark connections initiate by non matching File Name with a check on checkbox.
```
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <appSettings>
    <add key="BlockedFile" value="Data\blocked.txt" />
    <add key="LogFile" value="Data\EVIPBlocker_Log_" />
    <add key="MinutesScan" value="10" />
    <add key="RDPPort" value="3389" />
    <add key="UseRDP" value="true"></add>
    <add key="UseHTTP" value="true"></add>
    <add key="UseNonHTTP" value="true"></add>
    <add key="HTTPChecked" value="PHargaService|CefSharp"></add>
    <add key="Threshold" value="2" />
    <add key="RemoveBlock" value="N" />
    <add key="RemoveAfterHour" value="0" />
    <add key="IPRegex" value="\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b" />
    <add key="SuccessTypes" value="Security=4624=An account was successfully logged on=RDP"></add>
    <add key="LogTypes" value="Security=4625=An account failed=RDP|Application=18456=Login failed for user=MSSQL" />
    <add key="ExcludeInteractiveIP" value="False" />
  </appSettings>
  <startup>
    <supportedRuntime version="v4.0" sku=".NETFramework,Version=v4.0"/>
  </startup>
</configuration>
```
