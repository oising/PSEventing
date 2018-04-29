# PSEventing
PSEventing (Legacy Codeplex PowerShell 1.0)

<div class="wikidoc">

# [PSEventing Plus 1.0](https://pseventing.codeplex.com/releases/view/66587) Released (v1.0.0.1)

## About

Eventing extensions for PowerShell 2.0 Console Host. This module integrates with PowerShell 2.0 Eventing infrastructure. HotKey events can have a scriptblock action, or can put events in the event queue just like the OOTB eventing cmdlets. This is the recommended release for PowerShell 2.0 which already has built-in eventing support.

# [PSEventing 1.1](https://pseventing.codeplex.com/releases/view/5919) Released

Trap and respond to synchronous & asynchronous .NET, COM and WMI events or Hot Keys within your powershell scripts with this easy to use suite of cmdlets. Compatible with PowerShell 1.0, 2.0 & 3.0

## Important

This is intended for PowerShell v1.0 only. PowerShell v2.0 has introduced support for eventing with similarly named cmdlets, but with real-time (interrupt-based) event support. 

## New Features

*   Multiple named queue support and default queue with -QueueName parameter
*   Better COM support, window message pumping etc.
*   NoFlush / Peek parameter support for queue reading
*   Get-EventQueue command added for viewing queues and their message counts.
See [foreground / background swappable downloads in powershell](http://www.nivot.org/2007/12/05/ForegroundBackgroundSwappableDownloadsInPowerShell.aspx) for an advanced example.

## Cmdlet Name Changes

*   Get-Event -> Read-Event
*   Connect-EventListener -> Connect-Event
*   Disconnect-EventListener -> Disconnect-Event

### 64 bit PowerShell Support

To install for use with the 64bit version of PowerShell, please use the 64bit version of InstallUtil:  

<span class="codeInline">& "$env:windir\Microsoft.NET\Framework64\v2.0.50727\InstallUtil.exe" nivot.powershell.eventing.dll</span>  

## How does it work?

Events in PowerShell - but how? Events in a .NET executable are generally raised and handled in realtime in an asynchronous fashion; at first glance, this doesn't seem possible with our favourite little shell. PowerShell lets us wire up scriptblocks to .NET events as handlers relatively easily, but unfortunately if the event is raised on a different thread (as is the case with WMI watchers, FileSystemWatchers and other asynchronous objects), there is no runspace available to execute our script. So, how do we get a runspace to execute a handler? Well, we ensure that we are in a running pipeline before processing events. This is achieved by disconnecting the process: we separate the occurance of events from the act of handling them.  How does this work in practice? Let's play with <span class="codeInline">System.IO.FileSystemWatcher</span> :

## Example with FileSystemWatcher

This example uses the Cmdlets directly to demonstrate how the Snap-In works; you may find it easier and more familiar to work with the simple wrapper functions supplied on the Releases page. See the bottom of this page to read about these functions, called <span class="codeInline">Add-EventHandler</span>, <span class="codeInline">Remove-EventHandler</span> and <span class="codeInline">Do-Events</span> respectively.

### Step 1

    1# Add-PSSnapin pseventing

    2# $fsw = new-object system.io.filesystemwatcher
    3# $fsw.Path = "c:\temp"
    4# $fsw.EnableRaisingEvents = $true

This loads our snap-in, creates a <span class="codeInline">FileSystemWatcher</span> instance assigned to the variable named <span class="codeInline">fsw</span> and tells it to keep an eye on our temp directory. So, what events can we watch? Well, before you go running to MSDN...  

### Step 2

    #5 Get-EventBinding fsw -IncludeUnboundEvents | Format-Table -Auto

    VariableName EventName TypeName          Listening
    ------------ --------- --------          ---------
    fsw          Changed   FileSystemWatcher     False
    fsw          Created   FileSystemWatcher     False
    fsw          Deleted   FileSystemWatcher     False
    fsw          Disposed  FileSystemWatcher     False
    fsw          Error     FileSystemWatcher     False
    fsw          Renamed   FileSystemWatcher     False

This command lets us examine what events this variable exposes. Note that I say *variable*, and **not** object. This is a running theme throughout this library - **all PSEventing Cmdlets only deal with PSVariable objects, or variable names** so do not prefix your variable names with the $ symbol. This will end up passing the instance instead of the variable, which is not what we want. This is to keep the paradigm similar to other late-bound scripting languages, like vbscript; allowing direct object references would not have been very admin-friendly. Ok, so next, lets watch the <span class="codeInline">Changed</span> and <span class="codeInline">Deleted</span> events. Btw,  Event names are not case sensitive, unless you provide the <span class="codeInline">-CaseSensitive</span> SwitchParameter.  

### Step 3

    5# Connect-Event fsw changed,deleted

From this moment on, every time something is changed or deleted in c:\temp, a <span class="codeInline">PSEvent</span> object is added to our background event queue, even if you're busy running some other script or doing an unrelated task! Lets perform some related activity:  

### Step 4

    6# "foo" > c:\temp\bar.txt
    7# remove-item c:\temp\bar.txt

Now, we've just created and deleted a file. You could have also gone out to explorer and created and deleted the file; all events are captured, not just those triggered from within powershell. How do we find out whap's happened, and act on this info? with the <span class="codeInline">read-Event</span> cmdlet:  

### Step 5

    8# $events = Read-Event
    9# $events | Format-Table -Auto

    Occurred            Source                                  Name    Args
    --------            ------                                  ----    ----
    5/9/2007 3:31:39 PM System.Management.Automation.PSVariable Changed System.IO.FileSystemEventArgs
    5/9/2007 3:31:42 PM System.Management.Automation.PSVariable Deleted System.IO.FileSystemEventArgs

Two events occured since we wired up the listener (or since we last called <span class="codeInline">read-Event</span> since each call drains the queue). Lets have a look at the EventArgs for the first one:  

### Step 6

    10# $events[0].Args | format-table -Auto

    ChangeType FullPath        Name
    ---------- --------        ----
       Changed c:\temp\bar.txt bar.txt

And there you have it!   

Download the 1.1 release and have a look at the <span class="codeInline">sqlbackup.ps1</span> example script for more advanced uses.  

## Included Cmdlets

All cmdlets have -? help support, and you can also get more information via <span class="codeInline">get-help about_pseventing</span>  

*   **Connect-Event** : Start tracking one or more event(s) for one or more variable(s)

    *   <span class="codeInline">Connect-Event [-VariableName] <String> [-EventName] <String[]> [-CaseSensitive [<SwitchParameter>]] [-QueueName <identifier>] [<CommonParameters>]</span>
    *   <span class="codeInline">Connect-Event [-Variable] <PSVariable> [-EventName] <String[]> [-CaseSensitive [<SwitchParameter>]] [-QueueName <identifier>] <CommonParameters>]</span>

*   **Disconnect-Event** : Stop tracking one or more event(s) for one or more variable(s)

    *   <span class="codeInline">Disconnect-Event [-VariableName] <String> [-EventName] <String[]> [-CaseSensitive [<SwitchParameter>]] [<CommonParameters>]</span>
    *   <span class="codeInline">Disconnect-Event [-Variable] <PSVariable> [-EventName] <String[]> [-CaseSensitive [<SwitchParameter>]] <CommonParameters>]</span>

*   **Get-EventBinding** : Retrieve event binding information for variables

    *   <span class="codeInline">Get-EventBinding [[-Variable] [<PSVariable[]>]] [-IncludeUnboundEvents [<SwitchParameter>]] [<CommonParameters>]</span>
    *   <span class="codeInline">Get-EventBinding [[-VariableName] [<String[]>]] [-IncludeUnboundEvents [<SwitchParameter>]] [<CommonParameters>]</span>

*Hint : bind  or unbind all events at once for a variable*  

      1# $fsw = New-Object System.IO.FileSystemWatcher
      2# Get-EventBinding fsw -IncludeUnboundEvents | Connect-Event

*   **New-Event** : Create a custom event for insertion into the queue with an optional object payload

    *   <span class="codeInline">New-Event [-EventName] <String> [[-Data] [<PSObject>]] [-QueueName <identifier>] [<CommonParameters>]</span>

*   **Read-Event** : Retrieve one or more PSEvent objects representing tracked .NET events that have been raised since the last invocation of this command.

    *   <span class="codeInline">Read-Event [-Wait [<SwitchParameter>]] [-QueueName <identifier>] [-NoFlush <SwitchParameter>] [<CommonParameters>]</span>

*   **Get-EventQueue** :  Shows information about the default queue and/or named queues.

    *   <span class="codeInline">Get-EventQueue [[-QueueName] <String[]>] [-ShowEmpty] [<CommonParameters>]</span>

*   **Start-KeyHandler** : Start generating PSEvent objects for break (cltr+c) and/or keydown events.

    *   <span class="codeInline">Start-KeyHandler [-CaptureCtrlC [<SwitchParameter>]] [-CaptureKeys [<SwitchParameter>]] [<CommonParameters>]</span>

*   **Stop-KeyHandler** : Stop generating PSEvent objects for break (ctrl+c) and/or keydown events.

    *   <span class="codeInline">Stop-KeyHandler [<CommonParameters>]</span>

Read some more [Syntax Hints](https://pseventing.codeplex.com/wikipage?title=Syntax%20Hints&referringTitle=Home) for a quick start.  

- Oisin Grehan / x0n</div><div class="ClearBoth"></div>
