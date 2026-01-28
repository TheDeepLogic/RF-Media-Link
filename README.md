# RF Media Link - RFID Media Launcher

**RF Media Link** is a Windows service that launches media applications, files, and commands via RFID tags. Scan a tag to instantly launch your media player with the correct content, application with specific parameters, or execute custom commands. Perfect for media libraries and content management workflows.

> **AI-Assisted Development Notice**  
> This project was developed with GitHub Copilot assistance (Claude Haiku 4.5). The codebase and documentation were AI-generated based on specifications and iterative refinement. While functional and tested, please review code before production use and submit pull requests for any corrections.

---

## Features

- **Windows Service**: Runs in background monitoring RFID reader
- **Console Configuration Tool**: Easy tag and application management
- **Multiple Application Support**: Media players, file explorers, browsers, custom apps
- **Flexible Actions**: Launch applications, open files, navigate URLs, run commands
- **Customizable Arguments**: File paths, choices, toggles, and flags per application
- **Hot Reload**: Add tags via configurator while service runs
- **JSON Configuration**: Easy backup, version control, and manual editing

> **Reader Compatibility Note**
> RF Media Link is built around a **custom serial RFID reader** for this project. See the parts list in [BOM.md](BOM.md) and the ESP32 serial firmware in [host_examples/esp32c3_rfid_reader_with_display.ino](host_examples/esp32c3_rfid_reader_with_display.ino). Most off‑the‑shelf HID/keyboard‑wedge readers will **not** work with the service.
> 
> **Future Idea (Not Implemented)**: A configurable input string format could be added later to support other serial readers. This is documentation‑only for now.
>
> **Platform Scope**: The end‑to‑end solution is **Windows‑only** right now. Porting to macOS/Linux would require additional work and is out of scope for this project.

---

## Quick Start

### Requirements

- **Windows 10/11** (Windows Service requires .NET 8.0 runtime)
- **RFID Reader**: This project requires a **serial-output reader** compatible with the custom firmware described in [host_examples/README.md](host_examples/README.md) (see the ESP32 example with display). HID/keyboard-wedge readers are **not supported**.
- **Applications**: Install your chosen media applications

### Installation

1. **Clone or download** this repository
2. **Build the projects** (or use pre-built binaries from releases):
   ```powershell
   cd RFMediaLinkService
   dotnet publish -c Release
   
   cd ..\RFMediaLink
   dotnet publish -c Release
   ```

3. **Run the installer** (as Administrator):
   ```powershell
   cd deployment
   .\install-rfmedialink.ps1
   ```
   
   This will:
   - Copy binaries to `%LOCALAPPDATA%\RFMediaLink\`
   - Install the Windows Service
   - Create desktop and Start Menu shortcuts
   - Create configuration files

4. **Configure the serial port**:
   - Edit `%LOCALAPPDATA%\RFMediaLink\config.json`
   - Set `serial_port` to your RFID reader's COM port (e.g., `"COM3"`)
   - Set `baud_rate` if different from 115200

5. **Start the service**:
   ```powershell
   Start-Service "RF Media Link"
   ```

### First Tag Setup

1. **Run the configurator**:
   - Click the **RF Media Link** shortcut on your desktop, or
   ```powershell
   cd %LOCALAPPDATA%\RFMediaLink
   .\RFMediaLink.exe
   ```

2. **Add a tag**:
   - Press `A` to add tag
   - Place RFID tag on reader (it will auto-scan)
   - Or press Enter to type UID manually
   - Enter a name for the tag
   - Select action type `1` for application
   - Choose application from list
   - Configure arguments:
     - For **choice** fields (like model), select from numbered menu
     - For **file** fields (like media files), press `[B] Browse` to open file dialog or enter path
     - For **toggle** fields, enter `true`/`false` or press Enter for default
   - Press `S` at any prompt to skip remaining fields with defaults
   - Confirm save

3. **Test the tag**:
   - Scan the RFID tag
   - Application should launch with configured settings

---

## Configuration Files

All files are stored in `%LOCALAPPDATA%\RFMediaLink\`:

### `config.json`
```json
{
  "serial_port": "COM3",
  "baud_rate": 115200,
  "default_app": "vlc"
}
```

### `catalog.json`
Maps RFID UIDs to actions:
```json
{
  "66 DC 6E 05": {
    "name": "Movie Collection",
    "action_type": "app",
    "action_target": "vlc",
    "action_args": {
      "file": "D:\\Media\\Movies\\MyMovie.mp4"
    }
  }
}
```

### `emulators.json`
Defines available applications and their arguments:
```json
{
  "vlc": {
    "name": "VLC Media Player",
    "executable": "C:\\Program Files\\VideoLAN\\VLC\\vlc.exe",
    "arguments": [
      {
        "name": "file",
        "type": "file",
        "label": "Media File",
        "flag": ""
      }
    ]
  },
  "explorer": {
    "name": "File Explorer",
    "executable": "explorer.exe",
    "arguments": [
      {
        "name": "path",
        "type": "file",
        "label": "Folder Path",
        "flag": ""
      }
    ]
  },
      }
    ]
  }
}
```

---

## Emulator Argument Types

When configuring tags, arguments support different types:

| Type | Description | Example |
|------|-------------|---------|
| **file** | Full path to a disk/ROM file | `D:\Disks\game.dsk` |
| **choice** | Select from predefined options | Model: `apple2ee` |
| **toggle** | Boolean flag | Fullscreen: `True` |
| **flag** | Value passed with flag | `-rom cartridge.bin` |
| **positional** | Value without flag | `game.rom` |

The configurator automatically shows:
- **Choice fields** as numbered menus
- **Toggle fields** with true/false prompts
- **File fields** with path input
- **Defaults** for all fields (press Enter to use)

### Quick Entry Mode

When adding tags, use shortcuts to speed up configuration:
- Press **Enter** alone: Use default value for field
- Type **S** after a field: Skip rest with empty values
- Type **D** after a field: Default rest with their default values

---

## Included Emulators

RF Media Link comes preconfigured with:

- **AppleWin**: Apple II emulator
- **Stella**: Atari 2600 emulator
- **SNES9x**: Super Nintendo emulator
- **Classic99**: TI-99/4A emulator
- **VICE**: Commodore 64 emulator
- **TRS-80 GP**: TRS-80 emulator

To add more emulators, edit `emulators.json` manually with the same structure.

---

## How It Works

### Architecture

```
┌─────────────────┐
│  RFID Reader    │ USB Serial
│  (PN532/RC522)  │──────────┐
└─────────────────┘          │
                             ▼
                    ┌──────────────────┐
                    │ RF Media Link Service │
                    │  (Background)    │
                    └──────────────────┘
                             │
                    ┌────────┴────────┐
                    │                 │
         ┌──────────▼─────────┐  ┌───▼──────┐
         │  catalog.json      │  │ last_    │
         │  (UID→Action map)  │  │ scan.txt │
         └────────────────────┘  └──────────┘
                    │                 │
         ┌──────────▼─────────┐      │
         │ emulators.json     │      │
         │ (Emulator configs) │      │
         └────────────────────┘      │
                    │                 │
                    │            ┌────▼────────┐
                    └───────────►│ Configurator│
                                 │   (Tool)    │
                                 └─────────────┘
```

1. **Service monitors** serial port for RFID reader data
2. **On tag scan**:
   - Writes UID to `last_scan.txt` (for configurator)
   - **Reloads `catalog.json` from disk** (hot reload - no service restart needed!)
   - Looks up UID in catalog
   - Loads emulator config from `emulators.json`
   - Builds command-line arguments
   - Launches emulator process
3. **Configurator** reads `last_scan.txt` to detect scans while adding tags

### Scanning Workflow

The RFID reader sends data via serial in this format:
```
UID: 66 DC 6E 05
Type: Mifare Classic 1K
```

The service parses the UID, looks it up in `catalog.json`, and executes the mapped action.

---

## Configurator Usage

Run `configure.bat` or `RFMediaLink.exe` directly.

### Main Menu

```
═══════════════════════════════════════════════════════
  RF Media Link Configuration Tool
═══════════════════════════════════════════════════════
Config Location: C:\Users\...\AppData\Local\RFMediaLink

1. Manage Tags
2. View Emulators
3. Settings
4. Exit
```

### Manage Tags

Shows all configured tags:
```
═══════════════════════════════════════════════════════
  Manage RFID Tags
═══════════════════════════════════════════════════════

1. [66 DC 6E 05] Apple DOS 3.3
2. [AA BB CC DD] Ultima IV

A. Add Tag
E. Edit Tag
D. Delete Tag
B. Back to Menu
```

### Adding Tags

1. **Place tag on reader** - Auto-scans (or press Enter to type manually)
2. **Enter tag name** - Descriptive name for your reference
3. **Select action type**:
   - `1` - Emulator (launch emulator with arguments)
   - `2` - File (open a file)
   - `3` - URL (open in browser)
   - `4` - Command (run shell command)
4. **Configure** based on action type:
   - **Emulator**: Select from list, configure arguments with menus for choices
   - **File/URL/Command**: Enter target path/URL/command

### Deleting Tags

1. Press `D` from Manage Tags menu
2. Enter UID of tag to delete
3. Confirm deletion

### Editing Tags

1. Press `E` from Manage Tags menu
2. Enter UID of tag to edit
3. Press Enter to keep existing values, or enter new values
4. For emulator tags, you can change the emulator and update arguments
5. Confirm save

---

## Service Management

### Start/Stop/Restart

```powershell
# Start service
Start-Service "RF Media Link"

# Stop service
Stop-Service "RF Media Link"

# Restart service
Restart-Service "RF Media Link"

# Check status
Get-Service "RF Media Link"
```

### View Logs

Service logs to Windows Event Log:

```powershell
# View recent logs
Get-EventLog -LogName Application -Source "RF Media Link" -Newest 50

# Real-time monitoring
Get-EventLog -LogName Application -Source "RF Media Link" -Newest 1 -After (Get-Date).AddMinutes(-1)
```

### Update Service

After rebuilding the service DLL:

```powershell
cd deployment
.\update-service.ps1
```

This stops the service, copies new binaries, and restarts.

### Uninstall

```powershell
cd deployment
.\uninstall-retrof.bat
```

---

## Troubleshooting

### Service won't start

1. Check serial port in `config.json` matches your RFID reader
2. Verify .NET 8.0 Runtime is installed
3. Check Event Viewer for errors:
  ```powershell
  Get-EventLog -LogName Application -Source "RF Media Link" -Newest 10
  ```

### Tag not recognized

1. Verify tag is in `catalog.json`
2. Check UID format matches (spaces, uppercase: `66 DC 6E 05`)
3. Check Event Log - service logs "DEBUG: Catalog keys:" and "DEBUG: Looking for UID:" for comparison

### Emulator won't launch

1. Verify `executable` path in `emulators.json` is correct and points to the actual .exe file
2. Check disk/ROM file paths in `catalog.json` - must be full paths
3. Test emulator manually with same arguments
4. Check Event Log for "Error launching" messages with exception details

### Configurator doesn't see scans

1. Verify service is running: `Get-Service "RF Media Link"`
2. Check that `last_scan.txt` is being created in `%LOCALAPPDATA%\RFMediaLink\`
3. Restart service if needed

---

## Advanced Topics

### Custom Emulators

To add a new emulator, edit `emulators.json`:

```json
{
  "myemulator": {
    "name": "My Emulator",
    "executable": "C:\\Path\\To\\emulator.exe",
    "arguments": [
      {
        "name": "rom_file",
        "type": "file",
        "label": "ROM File",
        "flag": "-rom",
        "required": true
      },
      {
        "name": "fullscreen",
        "type": "toggle",
        "label": "Fullscreen",
        "flag": "-fullscreen",
        "default": false
      }
    ]
  }
}
```

Then restart the service: `Restart-Service "RF Media Link"`

### Non-Emulator Actions

You can map tags to other actions by editing `catalog.json`:

**Open a file:**
```json
{
  "AA BB CC DD": {
    "name": "Documentation",
    "action_type": "file",
    "action_target": "D:\\Docs\\manual.pdf"
  }
}
```

**Open a URL:**
```json
{
  "11 22 33 44": {
    "name": "Retro Gaming Site",
    "action_type": "url",
    "action_target": "https://www.retrogaming.com"
  }
}
```

**Run a command:**
```json
{
  "AA BB CC DD": {
    "name": "Backup Script",
    "action_type": "command",
    "action_target": "C:\\Scripts\\backup.bat"
  }
}
```

### Backup and Restore

All configuration is in JSON files in `%LOCALAPPDATA%\RFMediaLink\`:

```powershell
# Backup
Copy-Item "$env:LOCALAPPDATA\RFMediaLink\*.json" "D:\Backup\"

# Restore
Copy-Item "D:\Backup\*.json" "$env:LOCALAPPDATA\RFMediaLink\"
Restart-Service "RF Media Link"
```

---

## Development

### Project Structure

```
RF Media Link/
├── RFMediaLinkService/       # Windows Service (C# .NET 8.0)
│   ├── RfidWorker.cs         # Core service logic
│   ├── Program.cs            # Service host setup
│   └── RFMediaLinkService.csproj
├── RFMediaLink/              # Configuration tool (C# .NET 8.0)
│   ├── Program.cs            # Console UI
│   └── RFMediaLink.csproj
├── deployment/               # Installation scripts & binaries
│   ├── install-retrof.ps1
│   ├── update-service.ps1
│   ├── uninstall-retrof.bat
│   └── configure.bat
├── host_examples/            # Example RFID reader firmware
│   ├── esp32c3_rfid_reader_serial_only.ino
│   ├── esp32c3_rfid_reader_with_display.ino
│   └── rp2040_rfid_reader_micropython.py
├── BOM.md                    # Hardware bill of materials
└── README.md                 # This file
```

### Building from Source

**Requirements:**
- .NET 8.0 SDK
- Windows 10/11

**Build steps:**

```powershell
# Build service
cd RFMediaLinkService
dotnet publish -c Release

# Build configurator
cd ..\RFMediaLink
dotnet publish -c Release

# Copy to deployment
Copy-Item "RFMediaLinkService\bin\Release\net8.0-windows\publish\*" "deployment\build\" -Recurse -Force
```

### Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Test thoroughly
5. Submit a pull request

---

## Hardware

See [BOM.md](BOM.md) for hardware recommendations and wiring diagrams for building your own RFID reader.

Compatible RFID modules:
- PN532 (I2C/SPI/UART)
- RC522 (SPI)
- RDM6300 (UART)

Compatible microcontrollers:
- ESP32-C3
- RP2040 (Raspberry Pi Pico)
- Arduino compatible boards

Example firmware is provided in the `host_examples/` directory for ESP32-C3 and RP2040.

---

## License

This project is provided as-is for hobbyist and personal use.

---

## Credits

- **Author**: Aaron Smith
- **AI Assistant**: Claude Sonnet 4.5 via GitHub Copilot
- **Emulator Developers**: AppleWin, Stella, SNES9x, Classic99, VICE, TRS-80 GP teams

For questions, issues, or contributions, please open an issue on GitHub.
