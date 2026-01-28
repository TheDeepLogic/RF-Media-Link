# RetroNFC Service - Installation Guide

## Quick Installation

1. **Right-click `install-retrof.bat` and select "Run as administrator"**
   - This will install the service to `C:\Program Files\RetroNFC`
   - All configuration files will be copied there
   - The Windows Service will be created and started automatically

## After Installation

### Configure Your RFID Tags
Run `configure.bat` or:
```
python "C:\Program Files\RetroNFC\configure.py"
```

This GUI tool lets you:
- Add/view RFID tags (with optional serial port scanning)
- Set what action each tag triggers (emulator, file, URL, command)
- Configure serial port settings

### Configuration Files

All files are stored in `C:\Program Files\RetroNFC\`:

- **config.json** - Serial port and general settings
- **catalog.json** - RFID tag mappings
- **emulators.json** - Emulator definitions and arguments

### Service Management

**Start the service:**
```
net start RetroNFC
```

**Stop the service:**
```
net stop RetroNFC
```

**View service status:**
```
sc query RetroNFC
```

## Uninstallation

**Right-click `uninstall-retrof.bat` and select "Run as administrator"**

This will:
- Stop the Windows Service
- Remove all files from `C:\Program Files\RetroNFC`

## Troubleshooting

### Service not starting?
Check Windows Event Viewer:
- Windows Logs → System
- Look for RetroNFC entries

### RFID scanner not detected?
- Ensure scanner is connected to the configured serial port
- Check `config.json` for correct port (e.g., COM9)
- Verify scanner baud rate matches config (default: 115200)

### Python not found?
Ensure Python is installed and added to PATH, or specify the full path in `configure.bat`

## File Structure After Installation

```
C:\Program Files\RetroNFC\
├── RetroNFCService.exe      (The service executable)
├── RetroNFCService.dll      (Service library)
├── configure.py             (Configuration tool)
├── configure.bat            (Launcher for configuration tool)
├── config.json              (Settings)
├── catalog.json             (RFID tags)
├── emulators.json           (Emulator definitions)
├── *.dll                    (Dependencies)
└── runtimes/                (Runtime files)
```
