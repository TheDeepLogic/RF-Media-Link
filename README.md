# RetroNFC - RFID Emulator Launcher

**RetroNFC** is a universal RFID launcher system for retro computing and media. Scan RFID tags to launch emulators, open files, navigate to URLs, or execute system commands. Perfect for physical media collections (floppy disks, cartridges, VHS tapes) and retro enthusiasts.

## Features

- **RFID Tag Management**: Scan NFC/RFID tags and map them to actions
- **Emulator Support**: Launch any emulator with customizable arguments
- **Generic Emulation**: Comes with AppleWin preset, but works with any emulator (MAME, Atari, etc.)
- **Flexible Actions**: File execution, URLs, shell commands, hotkeys, system commands
- **Configurable Arguments**: Define argument types (file, text, choice, toggle) for emulator customization
- **Batch Adding**: Add multiple tags with the same command type
- **JSON-Based**: Easy to backup, version control, and customize

> **AI-Assisted Development Notice**
> 
> Hello, fellow human! My name is Aaron Smith. I've been in the IT field for nearly three decades and have extensive experience as both an engineer and architect. While I've had various projects in the past that have made their way into the public domain, I've always wanted to release more than I could. I write useful utilities all the time that aid me with my vintage computing and hobbyist electronic projects, but rarely publish them. I've had experience in both the public and private sectors and can unfortunately slip into treating each one of these as a fully polished cannonball ready for market. It leads to scope creep and never-ending updates to documentation.
> 
> With that in-mind, I've leveraged GitHub Copilot to create or enhance the code within this repository and, outside of this notice, all related documentation. While I'd love to tell you that I pore over it all and make revisions, that just isn't the case. To prevent my behavior from keeping these tools from seeing the light of day, I've decided to do as little of that as possible! My workflow involves simply stating the need to GitHub Copilot, providing reference material where helpful, running the resulting code, and, if there is an actionable output, validating that it's correct. If I find a change I'd like to make, I describe it to Copilot. I've been leveraging the Agent CLI and it takes care of the core debugging.
>
> With all that being said, please keep in-mind that what you read and execute was created by Claude Sonnet 4.5. There may be mistakes. If you find an error, please feel free to submit a pull request with a correction!

## Quick Start

### Installation

1. **Python 3.7+** is required
2. Install dependencies:
   ```bash
   pip install -r requirements.txt
   ```

### Setup

1. Connect your RFID reader to your computer via USB serial
2. Run the application:
   ```bash
   python main.py
   ```
3. On first launch, RetroNFC will create `config.json` with default serial port settings
4. If needed, update `serial_port` in `config.json` to match your RFID reader's COM port (e.g., `COM3`, `COM5`)

### Basic Commands

Once running, you can:

| Key | Action |
|-----|--------|
| `a` | Add a new NFC tag |
| `e` | Add/Edit emulator definitions |
| `q` | Quit |
| `done` | Exit batch mode |

## Usage Examples

### Adding an Emulator Tag

1. Press `a` to add a tag
2. Choose mode: Single or Batch
3. Choose action type: `emulator`, `file`, `url`, `command`, `hotkey`, or `shell`
4. If emulator: Select which emulator to use (e.g., AppleWin)
5. Scan your RFID tag
6. Configure emulator arguments (which floppies, hard drives, options, etc.)
7. Tag is now mapped in `catalog.json`

### Executing a Tag

Simply scan the RFID tag with your reader. RetroNFC will:
1. Recognize the UID
2. Load the action from `catalog.json`
3. Execute the appropriate action (launch emulator, open file, etc.)

### Creating a Custom Emulator

Press `e` to open the Add Emulator dialog:
1. Enter an emulator ID (e.g., `mame`)
2. Enter a display name (e.g., `MAME Arcade`)
3. Browse to the executable (e.g., `C:\mame\mame.exe`)
4. Click Create

Then edit `emulators.json` to add custom arguments:

```json
{
  "mame": {
    "name": "MAME Arcade",
    "executable": "C:\\mame\\mame.exe",
    "arguments": [
      {
        "name": "rom",
        "type": "file",
        "label": "ROM File",
        "flag": "",
        "required": true
      },
      {
        "name": "fullscreen",
        "type": "toggle",
        "label": "Fullscreen",
        "flag": "-f"
      }
    ]
  }
}
```

## Configuration Files

### catalog.json

Maps RFID UIDs to actions:

```json
{
  "041234567890ABC": {
    "emulator": "applewin",
    "config": {
      "s6d1": "C:\\disks\\game1.dsk",
      "fullscreen": true
    }
  }
}
```

### emulators.json

Defines available emulators and their configurable arguments:

```json
{
  "applewin": {
    "name": "AppleWin",
    "executable": "C:\\AppleWin\\AppleWin.exe",
    "arguments": [
      {
        "name": "model",
        "type": "choice",
        "label": "Apple II Model",
        "choices": ["apple2", "apple2p", "apple2e", "apple2ee"],
        "default": "apple2e"
      },
      {
        "name": "s6d1",
        "type": "file",
        "label": "Slot 6 Drive 1",
        "flag": "-d1"
      }
    ]
  }
}
```

### config.json

User preferences and serial configuration (auto-generated):

```json
{
  "serial_port": "COM3",
  "serial_baud": 115200,
  "last_browse_path": "C:\\disks",
  "last_command_type": "emulator",
  "last_mode": "single"
}
```

Edit `serial_port` to match your RFID reader's COM port. Default is `COM3`, but may vary depending on your hardware.

## Argument Types

When defining emulator arguments in `emulators.json`, use these types:

| Type | Description | Example |
|------|-------------|---------|
| `file` | File picker dialog | ROM file, disk image |
| `text` | Text input field | Model name, custom options |
| `choice` | Dropdown selector | AppleWin model (apple2/2e/2ee) |
| `toggle` | Checkbox (on/off) | Fullscreen, power-on flags |

## RFID Reader Setup

RetroNFC works with any RFID reader that outputs UIDs via serial port in the format: `UID: {hex_string}`

**Supported Hardware:**
- MFRC522 readers (most common, ~$5-15)
- Microcontrollers: ESP32-C3, Raspberry Pi Pico (RP2040), Arduino, etc.

**Getting Started:**

1. Choose your microcontroller platform
2. Use one of the example sketches/scripts in [host_examples/](host_examples/) directory
3. Upload to your microcontroller
4. Connect to your PC via USB
5. Update `serial_port` in `config.json` if needed

For detailed setup instructions and example code, see [host_examples/README.md](host_examples/README.md)

## Project Structure

```
_RFIDLAUNCHER/
├── main.py              # Main application
├── catalog.json         # UID → Action mappings (user data)
├── config.json          # User preferences (auto-generated)
├── emulators.json       # Emulator definitions
├── requirements.txt     # Python dependencies
├── .gitignore          # Git ignore patterns
└── README.md           # This file
```

## Architecture

### Command Flow

1. **Serial Listener**: Reads from RFID reader (COM9)
2. **UID Recognition**: When UID detected, lookup in catalog.json
3. **Action Execution**: Execute mapped action (emulator, file, URL, etc.)
4. **Dialogue System**: tkinter-based dialogs for configuration and user input

### Emulator Execution

When launching an emulator:
1. Load emulator definition from `emulators.json`
2. Show configuration dialog with argument fields
3. Build command-line arguments based on user input
4. Execute emulator with subprocess.Popen()

### Toggle Arguments

For toggle-type arguments (flags):
- If checked: Add `{flag} {value}` to command line (e.g., `-f true`)
- If unchecked: Omit the flag entirely

### Argument Order

Arguments are executed in the order they appear in `emulators.json`. This ensures positional arguments work correctly.

## Troubleshooting

### Serial Port Issues

**Problem**: "Opening COM3... [hangs]" or "Cannot open serial port"

**Solution**: Update `serial_port` in `config.json` to match your RFID reader's COM port. You can check available ports using:
- Windows: Device Manager → Ports (COM & LPT)
- Linux: `ls /dev/ttyUSB*` or `ls /dev/ttyACM*`
- macOS: `ls /dev/tty.usbserial*`

### Dialog Positioning

**Problem**: Dialogs appear off-screen

**Solution**: The code auto-centers on your primary monitor. For multi-monitor setups, you may need to adjust the geometry calculations.

### Emulator Not Found

**Problem**: "Error: No such file or directory"

**Solution**: Verify the executable path in `emulators.json` exists. Use Windows UNC paths for network shares (e.g., `\\\\server\\share\\app.exe`)

## Development

### Adding a New Action Type

1. Add case in `add_tag_interactive()` function
2. Add case in `execute_action()` function
3. Update `prompt_command_type()` to include new type

### Modifying Emulator Arguments

Edit `emulators.json` directly. The structure is:

```json
{
  "emulator_id": {
    "name": "Display Name",
    "executable": "path/to/executable.exe",
    "arguments": [
      {
        "name": "arg_name",
        "type": "file|text|choice|toggle",
        "label": "User-friendly label",
        "flag": "-flag",              // optional for positional args
        "choices": ["option1", "option2"],  // only for choice type
        "default": "value",           // optional
        "required": true              // optional
      }
    ]
  }
}
```

## License

RetroNFC is provided as-is for retro computing enthusiasts. Use at your own discretion.

---

## Changelog

### Version 2.0.0 (Current)

**Universal Emulator System** - Refactored from AppleWin-only launcher to support any emulator.

**Major Changes:**
- Generic emulator system with JSON-based configuration
- Flexible argument types: file, text, choice, toggle
- AppleWin preset with 14 configurable arguments
- Add/edit emulator definitions via GUI (`e` command)
- Batch tagging mode for efficient adding
- Fully JSON-based configuration (catalog, config, emulators)

**New Commands:**
- `e` - Add/Edit emulator definitions

---

## Architecture Overview

```
RFID Reader (USB CDC)
       ↓
    serial.Serial (config.json port)
       ↓
    main.py
       ├─ Reads emulators.json
       ├─ Reads/writes catalog.json
       └─ Reads/writes config.json
       ↓
    execute_action()
       ├─ File execution
       ├─ URL launch
       ├─ Hotkey execution
       └─ Emulator launch (subprocess.Popen)
```

### Data Flow

1. RFID reader scans tag → sends UID over serial
2. main.py looks up UID in catalog.json
3. If found: execute mapped action
4. If not found: ask user to add to catalog
5. For emulator actions:
   - Load emulator definition from emulators.json
   - Build command line from configuration
   - Execute with subprocess.Popen()

---

## Building the RFID Reader

See [BOM.md](BOM.md) for hardware requirements.

Included example sketches in `host_examples/`:
- **ESP32-C3 (Arduino)** - with or without display
- **Raspberry Pi Pico (MicroPython/CircuitPython)**
- **Raspberry Pi Pico (C SDK)**

See [host_examples/README.md](host_examples/README.md) for setup instructions.

---

## Project Files

| File | Purpose |
|------|---------|
| main.py | Main application (1,269 lines) |
| emulators.json | Emulator definitions and AppleWin preset |
| catalog.json | RFID UID → Action mappings (user data) |
| config.json | User preferences (auto-generated) |
| requirements.txt | Python dependencies (pyserial, pyautogui) |
| .gitignore | Git configuration |
| BOM.md | Hardware bill of materials |
| EMULATOR_GUIDE.md | Guide for adding custom emulators |
| ARCHITECTURE.md | Detailed system architecture and flow diagrams |

---

## Tips & Tricks

### Remember Last Selection
- RetroNFC remembers your last browse path, command type, and single/batch mode
- Settings saved in config.json

### File Arguments vs Positional
- **With flag** (`"flag": "-d1"`): produces `app.exe -d1 game.dsk`
- **No flag** (`"flag": ""`): produces `app.exe game.dsk` (positional)

### Batch Tagging
- Press `a`, choose **Batch** mode
- Select action type (e.g., emulator)
- Type `done` to exit batch mode and stop scanning

### Required vs Optional Arguments
- Required fields must be filled or error dialog appears
- Optional fields can be left blank and won't be added to command line

---

**RetroNFC** - Making physical media digital, one scan at a time.
