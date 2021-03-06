# EVIPBlocker

## Screenshot
<img src="https://user-images.githubusercontent.com/19261780/133896978-d341c6c0-273e-45c9-abb4-2e944ca9b9c4.gif" width="300">

## Description
A software for listing incoming/outgoing connections to/from a Windows machine, useful for managing Windows Server allowing operator to monitor active connections and allowing to automatically create a Windows Firewall to block connection from an IP that exceed configurable threshold of login failures.

By Default the software supports reading of Event Viewer to know any failure logon attempt to MS SQL database and Remote Desktop and has settings to add other Event ID which reports failure attempts of other software (as long as the software writes failure attempt into Event Viewer log).

However please note that some misconfiguration of Windows machine will cause this software to unable to read Event Viewer logs such as when the registry does not have permission for Event Log Readers user group. The Setting button has a tab to test reading of all configured Event ID, so users will know whether the software is working as intended.

## Tabs
<img src="https://user-images.githubusercontent.com/19261780/133919229-5c09ec9a-1b4a-4c5e-968a-6069dd27c8a2.png" width="300">

The LOG tab display current status of logs scanning, and display messages of IP that is blocked (when exist), the condition for blocking is configurable from Setting button that limits attempt of logon failure. The Manual tab display successful/unsuccessful login attempt and when necessary user manually click a single IP and select block. 


<img src="https://user-images.githubusercontent.com/19261780/133919237-afbb4e12-be88-4c8f-8748-c0d0f5011b18.png" width="300">

The RDP IN display current active Remote Desktop connection session to the server running the software, user can click a single IP and click block to block any unauthorised session.


<img src="https://user-images.githubusercontent.com/19261780/133919253-5e38ca6c-3481-4806-9251-0d416174c87c.png" width="300">

The NonHTTP tab display any incoming/outgoing connection that is not on port 443/80, the details of the connection can be known by clicking a single IP and click View, it will show the Windows Service, Executable File Path that initiates the connection, it allows Kill of the process and Disable of the Windows Service.
The HTTP tab display incoming or outgoing connection to port 443/80, similar to NonHTTP, the View button show the details of the connection and allow Kill/Disable of the process/service.

<img src="https://user-images.githubusercontent.com/19261780/133919275-aa7ac2f9-1444-4a86-a6ba-16017da77921.png" width="300">

When the View button is clicked in each of the connection from different tabs, the details of the connection is shown. It displays the File Name that initiates the connection, the arguments that is passed to it, the source/destination Port number of the connection, and the Windows Service name that initiates the file (if available). It allows Kill of the process or Disabled of the Windows Service.


## Buttons
Start button is clicked when the program runs in order to initiate the first and periodic scanning, the Stop button otherwise stop the scanning.

Any IP address that is blocked will be appended to a file name blocked.txt. When the file is migrated to a new server the firewall rules are not migrated, but clicking the Confirm button will re-create the Firewall rule based on the blocked.txt file, an already existing rule will not be created again.

The Blocked button display all blocked IP Address, you can click an IP and click Remove to re-allow connection from the IP Address.

<img src="https://user-images.githubusercontent.com/19261780/133919446-5840a3b3-6189-4936-b979-28fe79da179d.png" width="300">
<img src="https://user-images.githubusercontent.com/19261780/133919577-1dafc4d3-4327-414a-9bcb-c4ff92b4d93b.png" width="300">

The Setting button allows you to see program supported via Event ID which by default support reading of Event ID 18456 for failure attempt of MS SQL logon and Event ID 4625 for failure attempt of RDP logon. If the attempt exceed the set Threshold within a scan period configured in Minutes Scan, the IP address will automatically blocked and display in LOG tab. You can add other Event ID for other failure attempt that is logged down in Event Viewer via the Event ID.

On the Setting page, clicking Test tab and click Start will test whether the Windows machine has correct configuration, any text displaying "may not have permission to access them" like above means the Windows is misconfigured causing the program does not have the rights to access Event Viewer logs. The error Event ID 4624 is used by Manual tab, which means the program is unable to perform scanning of successful logon attempt and display it which then user can click to block any successful RDP logon. Other Event ID reports successful reading such as 4625 and 18456, means that the automatic threshold based blocking is working as intended, HTTP and NonHTTP tab is not affected by Event Viewer logs permission.

## Advanced Configuration
EVIPBlocker.exe.config contains configuration of the RDP Port of the server configured in "RDPPort" value="3389" which is port 3389, you can change it by editing the config file via notepad.

If you notice, the NonHTTP and HTTP tab has a checkbox in every connection, sometimes it is checked and sometimes its not. When it is checked, it means that the File Name of the executable that initiates the connection does not contains any of the word "PHargaService "or "CefSharp" which is configured in "HTTPChecked" value="PHargaService|CefSharp" which is separated by pipe (|), this means the configuration configures allowed File Name keywords that will mark connections initiate by non matching File Name with a check on checkbox to mark suspicious/unintended connections.
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
