# RetroNFC Configuration Tool

A simple console-based configuration utility for RetroNFC. No GUI bloat, no external dependencies needed - just straightforward DOS-style menus for managing your RFID tags and emulator settings.

## Features

- **Tag Management**: Add, view, and delete RFID tags with one or two keystrokes
- **Emulator Viewing**: Display all configured emulator systems
- **Settings**: Configure serial port and baud rate (defaults shown for quick access)
- **Live Scanning**: Place tag on reader and app reads directly from scan file
- **Minimal Keystrokes**: Press Y/N to confirm, Enter to keep defaults - only type when you need to change something

## Usage

### From Command Line
```batch
RetroNFCConfigure.exe
```

### From Batch File
Double-click `configure.bat` from the deployment folder.

## Menu Options

### Main Menu
```
1. Manage Tags       - Add, view, delete RFID tags
2. View Emulators    - List all configured emulators
3. Settings          - Serial port and baud rate config
4. Exit              - Close application
```

### Adding a Tag

1. Select "1. Manage Tags"
2. Choose "A. Add Tag"
3. Enter Tag UID or press Enter to scan a tag (places tag on reader within 3 seconds)
4. Enter a tag name (for reference)
5. Select action type (1=emulator, 2=file, 3=url, 4=command)
6. Enter target (emulator name, file path, URL, or command)
7. Review the summary and press Y to save or N to cancel

The app shows all your information before saving so you can verify it's correct.

### Deleting a Tag

1. Select "1. Manage Tags"  
2. Choose "D. Delete Tag"
3. Enter the Tag UID you want to remove
4. Confirm with Y

### Viewing Emulators

Select "2. View Emulators" to see all configured emulator systems, their IDs, and executable paths.

### Adjusting Settings

Select "3. Settings" to change:
- **Serial Port**: The COM port your RFID reader is connected to (default: COM9)
- **Baud Rate**: Serial communication speed (default: 115200)

Just press Enter to keep the current value, or type a new one to change it.

## Configuration Files

All configuration is stored in:
- **Windows**: `%LOCALAPPDATA%\RetroNFC\` (e.g., `C:\Users\YourName\AppData\Local\RetroNFC\`)

Files managed:
- `config.json` - Serial port and baud settings
- `catalog.json` - Your RFID tags and their actions
- `emulators.json` - Available emulator systems

## Design Philosophy

This tool intentionally avoids:
- ✗ Complex GUI elements
- ✗ Excessive clicking or navigation
- ✗ External Python/interpreter requirements
- ✗ Bloated visual styling

Instead it provides:
- ✓ Fast console menus (DOS-style)
- ✓ Smart defaults (press Enter to keep existing values)
- ✓ Minimal typing (only change what you need)
- ✓ Single executable (no dependencies)
- ✓ Clear information display

## Tag Action Types

| Type | Description | Example |
|------|-------------|---------|
| emulator | Launch a retro gaming emulator | nes, snes, genesis |
| file | Open a file in default application | game.iso, rom.nes |
| url | Open a URL in default browser | https://example.com |
| command | Execute a Windows command | `explorer.exe C:\Games` |

## Tips

- **Quick scanning**: When adding a tag, just press Enter at the UID prompt and place your tag on the reader within 3 seconds
- **Keep defaults**: When in Settings, press Enter twice to save without changing anything
- **No backups**: Before deleting tags, make note of them - there's no undo, but you can always add them again
- **Multiple actions**: A single UID can only have one action, but you can edit existing tags by adding them again

## Troubleshooting

**"Configuration not found"**
- The service hasn't been installed yet. Run the installer first.

**"No tag scanned"**
- Tag wasn't placed on reader within 3 seconds. Try again and hold the tag near the reader longer.

**Serial port errors in service**
- Check Settings in the config tool and verify the COM port is correct and not in use by another application.

