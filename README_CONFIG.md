# RetroNFC - RFID Tag Handler for Retro Gaming

## What's This?

RetroNFC is a Windows system that detects when you place RFID tags near a reader and launches emulators, files, URLs, or commands based on tag configuration.

**Example**: Tap an NES cartridge-shaped RFID tag → NES emulator launches with that game.

## Components

### 1. **RetroNFC Windows Service** (Background)
- Runs continuously and monitors the serial RFID reader
- Detects tag scans
- Triggers configured actions
- Stores scan history in `last_scan.txt`
- No user interaction needed

### 2. **Configuration Tool** (User Interface)
- DOS-style console application (no GUI bloat, no Python needed)
- Manage your RFID tags
- Configure serial port settings
- View available emulators
- Add/delete/edit tag mappings

### 3. **Deployment Package**
- One-click installer
- Update script
- All dependencies included

## Quick Start

### First Time
1. **Install**: Run `deployment\install-retrof.bat` as Administrator
2. **Configure**: Run `deployment\configure.bat` to add your tags
3. **Done**: System runs in the background

### Daily Use
- Place tags on reader
- Emulators launch automatically
- No interface needed

## How It Works

```
RFID Reader ──serial port──> RetroNFC Service ──writes──> last_scan.txt
                                    ↓
                            Check catalog.json
                                    ↓
                            Execute action
                                    ↓
                        (emulator/file/url/command)
```

## Configuration

### Where Stuff Lives
Everything is in your user profile: `%LOCALAPPDATA%\RetroNFC\`

No Admin Rights needed for changes (after initial install).

### Config Files
- **config.json** - Serial port, baud rate
- **catalog.json** - Your RFID tag mappings
- **emulators.json** - Available emulator definitions

### Adding Tags
```
Run: deployment\configure.bat
  ↓
Select: 1. Manage Tags
  ↓
Select: A. Add Tag
  ↓
Enter UID (or leave blank to scan)
  ↓
Enter tag name (e.g., "Mario Bros")
  ↓
Select action (1=emulator, 2=file, 3=url, 4=command)
  ↓
Enter target (e.g., "nes" for emulator)
  ↓
Press Y to save
```

## System Requirements

- Windows 10/11
- .NET 8.0 Runtime (included in installer)
- USB RFID Reader (serial or serial-over-USB)
- Administrator rights for installation only

## Documentation

- **[QUICK_START.md](QUICK_START.md)** - Get running in 5 minutes
- **[CONFIGURE_README.md](CONFIGURE_README.md)** - How to use the config tool
- **[CONSOLE_TOOL_SUMMARY.md](CONSOLE_TOOL_SUMMARY.md)** - Technical details
- **[ARCHITECTURE.md](ARCHITECTURE.md)** - How the system is built
- **[EMULATOR_GUIDE.md](EMULATOR_GUIDE.md)** - Setting up emulators

## Features

✓ **No Python needed** - Everything is built-in Windows executable  
✓ **No fancy GUI** - Console-based, clean and simple  
✓ **Background service** - Runs on login automatically  
✓ **User-writable config** - No elevation needed to manage tags  
✓ **Multiple action types** - Emulators, files, URLs, or commands  
✓ **Serial RFID** - Works with any USB RFID reader  

## Installation

```batch
cd deployment
install-retrof.bat
```

Or double-click `deployment\install-retrof.bat`

## Update

```batch
cd deployment
update-service.bat
```

Copies latest binaries and updates service.

## Uninstall

```powershell
cd deployment
.\install-retrof.ps1 -Uninstall
```

## Tag Action Types

| Type | What It Does | Example |
|------|-------------|---------|
| emulator | Launch emulator defined in emulators.json | Type: 1, Target: "nes" |
| file | Open file with default program | Type: 2, Target: "C:\Games\game.iso" |
| url | Open URL in default browser | Type: 3, Target: "https://example.com" |
| command | Run Windows command | Type: 4, Target: "explorer.exe C:\Games" |

## Common Tasks

### Add a new tag
```
configure.bat → 1 → A → scan/enter UID → name → action type → target → Y
```

### Change serial port
```
configure.bat → 3 → enter new port → Y
```

### See all your tags
```
configure.bat → 1 → (list shown)
```

### Remove a tag
```
configure.bat → 1 → D → enter UID → Y
```

## Troubleshooting

### "Service won't start"
Check Event Viewer for errors:
```powershell
Get-EventLog -LogName Application -Source RetroNFC -Newest 20
```

### "Config tool won't run"
Make sure .NET 8.0 is installed and config files exist in AppData.

### "Scans not working"
1. Check serial port setting (should match your reader)
2. Make sure reader is powered and connected
3. Check Device Manager for COM port number
4. Update config tool: Settings → verify COM port

### "Wrong emulator launches"
Check your tag's action target in config tool matches emulator name in emulators.json.

## Architecture Notes

- **Service**: Runs as SYSTEM user, has COM port access
- **Config Tool**: Runs as current user, reads/writes JSON files
- **Communication**: File-based IPC (no port conflicts)
- **Errors**: Logged to Event Viewer automatically

See [ARCHITECTURE.md](ARCHITECTURE.md) for details.

## Source Code

- `RetroNFCService/` - Windows Service source (C#)
- `RetroNFCConfigure/` - Configuration tool source (C#)
- `deployment/` - Installer and update scripts

All source is included in this repository.

## License

Retro Gaming Configuration Tool - Use freely

---

**Questions?** See [QUICK_START.md](QUICK_START.md) or check [CONFIGURE_README.md](CONFIGURE_README.md)

