# http-htpc

Tiny LAN HTTP remote-control receiver for a Windows HTPC/VM.

`http-htpc` listens for simple HTTP GET requests and sends Windows media/keyboard keys, intended for use with tools like StreamDeck.

Example:

```text
http://WRAITH-IP:8787/playpause?token=YOUR_TOKEN
```

## Requirements

* Windows 10/11
* .NET 10 SDK for building
* No .NET install required on the target machine if published self-contained

Check Your SDK:

```powershell
dotnet --version
```

## Build

From the project folder:

```powershell
dotnet publish -c Release -r win-x64 -o publish `
  -p:PublishSingleFile=true `
  -p:PublishAot=true `
  --self-contained true
```

Output will be in:

```text
publish/
```

Copy the resulting `.exe` to the target machine, for example:

```text
C:\Tools\http-htpc\http-htpc.exe
```

## Run manually

On the target machine:

```powershell
C:\Tools\http-htpc\http-htpc.exe
```

## Test in browser

From the target machine:

```text
http://localhost:8787/playpause?token=YOUR_TOKEN
```

From another machine on the LAN:

```text
http://[HOST_IP]:8787/playpause?token=YOUR_TOKEN
```

If localhost works but LAN access does not, check the firewall rule.

## Firewall rule

Run PowerShell as Administrator on the target machine:

```powershell
New-NetFirewallRule `
  -DisplayName "http-htpc" `
  -Direction Inbound `
  -Action Allow `
  -Protocol TCP `
  -LocalPort 8787 `
  -Profile Private
```

Confirm the network profile is Private:

```powershell
Get-NetConnectionProfile
```

If needed:

```powershell
Set-NetConnectionProfile -InterfaceAlias "Ethernet" -NetworkCategory Private
```

Use the actual interface name shown by `Get-NetConnectionProfile`.

## Auto-start with Task Scheduler

Recommended instead of a Windows Service because this app sends keyboard/media input and should run in the logged-in desktop session.

Open Task Scheduler:

```text
Win + R
taskschd.msc
```

Create Task:

* Name: `http-htpc`
* General:

  * Run only when user is logged on
  * Optional: Run with highest privileges
* Triggers:

  * At log on
  * Optional delay: 30 seconds
* Actions:

  * Start a program
  * Program: `C:\Tools\http-htpc\http-htpc.exe`
* Settings:

  * Allow task to be run on demand
  * Restart on failure if desired

Test by right-clicking the task and choosing **Run**.

## Example endpoints

```text
/playpause
/stop
/next
/previous
/volume-up
/volume-down
/mute
/left
/right
```

Example StreamDeck URL:

Make a Website button and set to GET request in background.

```text
http://[HOST_IP]:8787/right?token=YOUR_TOKEN
```

## Security notes

This is intended for trusted LAN use only.

Recommended:

* Use a long random token
* Do not expose this port to the internet
* Keep the firewall rule limited to Private networks
* Only allow fixed known commands
* Never add arbitrary command execution
