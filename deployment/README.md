# RetroNFC Deployment Package

This folder contains everything needed to install and run the RetroNFC Windows Service.

## Installation

**Windows users:** 
1. Right-click `install-retrof.bat`
2. Select "Run as administrator"
3. Follow the prompts

The installer will:
- Install files to `C:\Program Files\RetroNFC`
- Create and start the RetroNFC Windows Service
- Copy template configuration files

## After Installation

Run `configure.bat` to open the configuration tool and add/manage RFID tags.

## Uninstallation

Right-click `uninstall-retrof.bat` and select "Run as administrator"

## Files in This Package

- `install-retrof.bat` - Double-click installer (requires admin)
- `install-retrof.ps1` - PowerShell installer script
- `uninstall-retrof.bat` - Uninstaller script
- `configure.bat` - Launches the configuration tool
- `configure.py` - RFID tag configuration GUI
- `build/` - Pre-compiled service executable and dependencies

## Troubleshooting

If installation fails, try running PowerShell as Administrator and executing:
```powershell
powershell -ExecutionPolicy Bypass -File install-retrof.ps1
```

See the main README.md in the parent directory for more help.
