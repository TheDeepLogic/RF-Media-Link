# RF Media Link Deployment Scripts

This folder contains installation and management scripts for RF Media Link.

## Installation

**Windows users:** 
1. Right-click `install.bat`
2. Select "Run as administrator"
3. Follow the prompts

The installer will:
- Install files to `C:\ProgramData\RFMediaLink`
- Create a scheduled task to run at login with elevated privileges (no UAC prompts)
- Create Start Menu shortcuts and desktop shortcut
- Copy default configuration files (`emulators.json`)
- Start the service

## Management Scripts

### Installation
- **`install.bat`** - Install RF Media Link
- **`uninstall.bat`** - Completely remove RF Media Link
- **`clean-install.bat`** - Uninstall then reinstall (useful for updates)

### Building & Packaging
- **`build-release.bat`** - Build both service and configurator for release
- **`package-release.bat`** - Create installer package and ZIP for distribution

### Updates (Right-click "Run as administrator")
- **`update-service.bat`** / **`update-service.ps1`** - Update service binaries (builds first)
- **`update-configurator.bat`** / **`update-configurator.ps1`** - Update configurator binaries (builds first)

### Service Management
- **`start-service.ps1`** - Start the service
- **`stop-service.ps1`** - Stop the service
- **`restart-service.ps1`** - Restart the service

### Configuration
- **`configure.bat`** - Launch the configurator tool
- **`open-catalog.bat`** - Open catalog.json in default text editor
- **`open-emulators.bat`** - Open emulators.json in default text editor

## After Installation

The configurator will be available via:
- Desktop shortcut: "RF Media Link Configure"
- Start Menu: Programs > RF Media Link > RF Media Link Configure

Or run directly:
```powershell
& "C:\ProgramData\RFMediaLink\RFMediaLink.exe"
```

## Service Management

The service runs automatically at login. Use the Start Menu shortcuts or:

```powershell
# Start via scheduled task
Start-ScheduledTask -TaskName "RF Media Link Service"

# Stop the service
Stop-Process -Name "RFMediaLinkService" -Force

# Check if running
Get-Process RFMediaLinkService
```

## Configuration Files

Located in `C:\ProgramData\RFMediaLink\`:
- **`config.json`** - Serial port and service settings (created automatically)
- **`catalog.json`** - RFID tag catalog (managed via configurator)
- **`emulators.json`** - Emulator definitions (copied from `inc/emulators.json` during install)

## Logs

View logs in Windows Event Viewer:
1. Open Event Viewer (eventvwr.msc)
2. Navigate to: Windows Logs > Application
3. Filter by source: "RFMediaLinkService"

## Troubleshooting

**Installation fails:**
- Ensure you're running as Administrator
- Check that no RF Media Link processes are running
- Try `clean-install.bat` for a fresh start

**Service won't start:**
- Check Event Viewer for errors
- Verify serial port settings in `config.json`
- Ensure .NET 8.0 runtime is installed

**Emulators don't get focus:**
- The service requires elevated privileges (automatically configured via scheduled task)
- If manually starting, right-click the exe and "Run as administrator"

## Uninstallation

Right-click `uninstall.bat` and select "Run as administrator". This will:
- Stop all running processes
- Remove the scheduled task
- Delete all installed files
- Remove Start Menu shortcuts and desktop shortcut

## For Developers

See parent directory README.md for build instructions.

