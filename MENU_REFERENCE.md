# RetroNFC Configuration Tool - Menu Reference

## Quick Navigation Map

```
RetroNFCConfigure.exe
│
├─ 1. MANAGE TAGS
│  ├─ [List all tags]
│  ├─ A. ADD TAG
│  │  ├─ Enter UID (or blank to scan)
│  │  ├─ Enter Tag Name
│  │  ├─ Select Action Type (1=emulator, 2=file, 3=url, 4=command)
│  │  ├─ Enter Target
│  │  ├─ Review Summary
│  │  └─ Save? (Y/N)
│  │
│  └─ D. DELETE TAG
│     ├─ Enter Tag UID
│     ├─ Confirm Delete? (Y/N)
│     └─ [Remove from catalog]
│
├─ 2. VIEW EMULATORS
│  └─ [Display all configured emulators with paths]
│
├─ 3. SETTINGS
│  ├─ Serial Port (current: shown)
│  ├─ Baud Rate (current: shown)
│  └─ Save? (Y/N)
│
└─ 4. EXIT
   └─ [Close application]
```

## Keyboard Shortcuts

### Main Menu
```
1  → Manage Tags
2  → View Emulators  
3  → Settings
4  → Exit
```

### Manage Tags Submenu
```
A  → Add Tag
D  → Delete Tag
B  → Back to Menu
```

### Any Screen
```
Any Key → Continue after confirmation
Enter   → Accept default / Skip field
Y      → Confirm/Save
N      → Cancel/Don't save
```

## Action Type Codes

When adding a tag, select action type:

```
1 → emulator     (Launch game in emulator)
              Target example: "nes", "snes", "genesis"
              
2 → file        (Open file with default program)
              Target example: "C:\Games\game.rom"
              
3 → url         (Open URL in default browser)
              Target example: "https://example.com"
              
4 → command     (Execute Windows command)
              Target example: "explorer.exe C:\Games"
```

## Default Actions

```
UID Input         → Press Enter to scan tag
Action Type       → Press Enter for "emulator" (most common)
Serial Port       → Press Enter to keep current value
Baud Rate         → Press Enter to keep current value
Confirmation      → Press Y to save, N to cancel
```

## Field Descriptions

### UID (Unique Identifier)
- **What it is**: Hex string from RFID tag (e.g., "5C 16 24 41")
- **How to enter**: Manually type or leave blank to scan
- **Scanning**: Place tag on reader within 3 seconds
- **Format**: Can include spaces or not (both work)

### Tag Name
- **What it is**: Human-readable label for the tag
- **Example**: "Mario Bros", "Zelda", "Game Genie Code"
- **Required**: Yes (helps identify tag later)
- **Display**: Shown in tag list

### Action Type
- **What it is**: What happens when tag is scanned
- **Options**: emulator (1), file (2), url (3), command (4)
- **Default**: emulator (press Enter to use)
- **Common**: Usually emulator for gaming tags

### Target
- **What it is**: Where the action points to
- **For emulator**: Name from emulators.json (e.g., "nes")
- **For file**: Full path (e.g., "C:\Games\game.iso")
- **For url**: Web address (e.g., "https://site.com")
- **For command**: Windows command (e.g., "notepad.exe")

### Serial Port
- **What it is**: COM port your RFID reader is on
- **Default**: COM9
- **Find**: Check Device Manager
- **Format**: COM# (where # is 1-20)
- **Update**: Only if changing hardware

### Baud Rate
- **What it is**: Serial communication speed
- **Default**: 115200
- **Common rates**: 9600, 19200, 38400, 57600, 115200
- **Most readers**: 115200 (leave as-is)

## Common Workflows

### Workflow 1: Add One Tag
```
Main Menu
  1
Manage Tags → Shows current tags
  A
Add Tag
  [Enter to scan]
  Place tag on reader
  [Scanned: xxxxxxxx]
  Enter name: My Game
  [Enter for emulator]
  nes
  Review and Y
Back to Manage Tags → B
Main Menu → 4
```

### Workflow 2: Add Multiple Tags
```
Main Menu
  1
Add Tag → [Complete as above]
Back → A
Add Tag → [Complete as above]
Back → A
Add Tag → [Complete as above]
Back → B
Main Menu → 4
```

### Workflow 3: Remove a Tag
```
Main Menu
  1
Manage Tags → Shows tags
  D
Enter UID: [xxxxxxxx]
Confirm: Y
Removed → B
Main Menu → 4
```

### Workflow 4: Check System
```
Main Menu
  2
View Emulators → See all configured
  [any key]
  3
Settings → Check COM port
  [Enter] [Enter] N
  4
Exit
```

## Screen Layouts

### Main Menu Layout
```
═════════════════════════════════════
  RetroNFC Configuration Tool
═════════════════════════════════════
Config Location: C:\Users\Name\AppData\Local\RetroNFC

1. Manage Tags
2. View Emulators
3. Settings
4. Exit

Select option (1-4): █
```

### Manage Tags Layout
```
═════════════════════════════════════
  Manage RFID Tags
═════════════════════════════════════

1. [5C 16 24 41] Super Mario Bros
2. [AA BB CC DD] Tetris
3. [11 22 33 44] Zelda

A. Add Tag
D. Delete Tag
B. Back to Menu

Select option (A/D/B): █
```

### Add Tag Layout
```
═════════════════════════════════════
  Add RFID Tag
═════════════════════════════════════

Tag UID (or blank to scan): █

Tag Name: █

Action Type (default: emulator):
  1. emulator
  2. file
  3. url
  4. command
Select (1-4, or press Enter for emulator): █

Target (emulator name, file path, URL, or command): █

═════════════════════════════════════
UID:    xxxxxxxx
Name:   My Game
Action: emulator
Target: nes

Save? (Y/N): █
```

### Settings Layout
```
═════════════════════════════════════
  Settings
═════════════════════════════════════

Serial Port (current: COM9)
Enter new port or press Enter to keep: █

Baud Rate (current: 115200)
Enter new baud or press Enter to keep: █

Save? (Y/N): █
```

## Error Messages You Might See

```
"No tag scanned"              → Didn't place tag on reader in time
"Cancelled"                   → User pressed N without entering anything
"Configuration not found"     → Service not installed or AppData missing
"Invalid input"               → Typed something unexpected
[blank after prompt]          → Waiting for user input
```

## Tips for Keyboard Efficiency

1. **Tab doesn't work** - Use Enter to move to next field
2. **Backspace works** - Delete characters if you mistype
3. **Caps lock** - Optional (most fields case-insensitive)
4. **Spaces in UID** - Allowed either way (keeps or removes)
5. **Escape** - Doesn't cancel (N key does)
6. **Ctrl+C** - Forcefully closes (not recommended)

## Configuration File Reference

These files are read/written by the config tool:

### config.json
```json
{
  "serial_port": "COM9",
  "baud_rate": 115200
}
```

### catalog.json
```json
{
  "5C 16 24 41": {
    "name": "Super Mario Bros",
    "action_type": "emulator",
    "action_target": "nes"
  },
  "AA BB CC DD": {
    "name": "Game Genie Codes",
    "action_type": "url",
    "action_target": "https://www.ggcodes.com"
  }
}
```

### emulators.json (read-only from config tool)
```json
{
  "nes": {
    "name": "Nintendo Entertainment System",
    "executable": "C:\\Emulators\\fceux\\fceux.exe"
  },
  "snes": {
    "name": "Super Nintendo Entertainment System",
    "executable": "C:\\Emulators\\snes9x\\snes9x.exe"
  }
}
```

