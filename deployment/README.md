# RF Media Link Deployment Package

This folder contains everything needed to install and run the RF Media Link Windows Service.

## Installation

**Windows users:** 
1. Right-click `install-rfmedialink.bat`
2. Select "Run as administrator"
3. Follow the prompts

The installer will:
- Install files to `%LOCALAPPDATA%\RFMediaLink`
- Create and start the RF Media Link Windows Service
- Copy template configuration files

## After Installation

Run the configurator from Start Menu or use:
```powershell
& "$env:LOCALAPPDATA\RFMediaLink\RFMediaLink.exe"
```

## Uninstallation

Right-click `uninstall-rfmedialink.bat` and select "Run as administrator"

## Files in This Package

- `install-rfmedialink.bat` - Double-click installer (requires admin)
- `install-rfmedialink.ps1` - PowerShell installer script
- `uninstall-rfmedialink.bat` - Uninstaller script
- `build/` - Pre-compiled service executable and dependencies

## Troubleshooting

If installation fails, try running PowerShell as Administrator and executing:
```powershell
powershell -ExecutionPolicy Bypass -File install-rfmedialink.ps1
```

See the main README.md in the parent directory for more help.
