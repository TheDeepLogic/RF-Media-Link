# RetroNFC Configuration Tool - Usage Examples

## Main Menu Example

```
═══════════════════════════════════════════════════════
  RetroNFC Configuration Tool
═══════════════════════════════════════════════════════
Config Location: C:\Users\Aaron\AppData\Local\RetroNFC

1. Manage Tags
2. View Emulators
3. Settings
4. Exit

Select option (1-4): 
```

## Example 1: Adding a Tag (Via Scan)

```
═══════════════════════════════════════════════════════
  Add RFID Tag
═══════════════════════════════════════════════════════

Tag UID (or blank to scan): 
  [User presses Enter without typing]

Waiting for scan (place tag on reader)...
Scanned: 5C 16 24 41

Tag Name: Super Mario Bros
     
Action Type (default: emulator):
  1. emulator
  2. file
  3. url
  4. command
Select (1-4, or press Enter for emulator): 
  [User presses Enter to accept emulator]

Target (emulator name, file path, URL, or command): nes

═══════════════════════════════════════════════════════
UID:    5C 16 24 41
Name:   Super Mario Bros
Action: emulator
Target: nes

Save? (Y/N): Y
Tag saved!
Press any key...
```

## Example 2: Adding a Tag (Manual UID)

```
═══════════════════════════════════════════════════════
  Add RFID Tag
═══════════════════════════════════════════════════════

Tag UID (or blank to scan): AA BB CC DD

Tag Name: Game Genie Code
     
Action Type (default: emulator):
  1. emulator
  2. file
  3. url
  4. command
Select (1-4, or press Enter for emulator): 3

Target (emulator name, file path, URL, or command): https://www.ggcodes.com

═══════════════════════════════════════════════════════
UID:    AA BB CC DD
Name:   Game Genie Code
Action: url
Target: https://www.ggcodes.com

Save? (Y/N): Y
Tag saved!
Press any key...
```

## Example 3: Managing Tags

```
═══════════════════════════════════════════════════════
  Manage RFID Tags
═══════════════════════════════════════════════════════

1. [5C 16 24 41] Super Mario Bros
2. [AA BB CC DD] Game Genie Code
3. [11 22 33 44] Tetris

A. Add Tag
D. Delete Tag
B. Back to Menu

Select option (A/D/B): A
  [Shows Add Tag flow above]
```

## Example 4: Configuring Settings

```
═══════════════════════════════════════════════════════
  Settings
═══════════════════════════════════════════════════════

Serial Port (current: COM9)
Enter new port or press Enter to keep: 
  [User presses Enter]
  (Keeps COM9)

Baud Rate (current: 115200)
Enter new baud or press Enter to keep: COM5
  [User types different port first]
  ERROR: Invalid input, expecting number
  
Baud Rate (current: 115200)
Enter new baud or press Enter to keep: 
  [User presses Enter]
  (Keeps 115200)

Save? (Y/N): Y
Settings saved!
Press any key...
```

## Example 5: Viewing Emulators

```
═══════════════════════════════════════════════════════
  Emulators
═══════════════════════════════════════════════════════

Nintendo Entertainment System
  ID: nes
  Path: C:\Emulators\fceux\fceux.exe

Super Nintendo Entertainment System
  ID: snes
  Path: C:\Emulators\snes9x\snes9x.exe

Genesis/Mega Drive
  ID: genesis
  Path: C:\Emulators\gens\gens.exe

Game Boy
  ID: gb
  Path: C:\Emulators\bgb\bgb.exe

Press any key...
```

## Example 6: Deleting a Tag

```
═══════════════════════════════════════════════════════
  Delete RFID Tag
═══════════════════════════════════════════════════════

Enter Tag UID to delete: AA BB CC DD

Confirm delete? (Y/N): Y
Tag deleted!
Press any key...
```

## Example 7: Quick Workflow - User is Fast

### Scenario: Add 3 tags, no mistakes

```
Main Menu
  1
    ↓
Manage Tags Screen
  A
    ↓
Add Tag Screen
  [Enter]        ← scan mode
  [Wait]         ← place tag
  [Shows UID]
  My Game [Enter]
  [Enter]        ← use emulator (default)
  nes
  [Y]            ← save

Back to Manage Tags
  A              ← add another
    ↓
[Repeat 2 more times]
    ↓
B              ← back to menu
  4             ← exit
```

**Total keystrokes**: About 30-40 keys for 3 complete tag additions  
**Total time**: ~2 minutes

## Example 8: Minimal Changes - Press Enter Twice

### Scenario: User only wants to verify settings

```
Main Menu
  3              ← Settings
    ↓
Settings Screen (shows current values)
  [Enter]        ← keep COM9
  [Enter]        ← keep 115200
  Y              ← save (even though nothing changed)
```

**Total keystrokes**: 3  
**Total time**: ~5 seconds

## Configuration File Changes

### Before Adding Tag
```json
{
  "5C 16 24 41": {
    "name": "Super Mario Bros",
    "action_type": "emulator",
    "action_target": "nes"
  }
}
```

### After Adding Tag  
```json
{
  "5C 16 24 41": {
    "name": "Super Mario Bros",
    "action_type": "emulator",
    "action_target": "nes"
  },
  "AA BB CC DD": {
    "name": "Game Genie Code",
    "action_type": "url",
    "action_target": "https://www.ggcodes.com"
  }
}
```

## Key Features Demonstrated

✓ **Smart Defaults** - Press Enter to keep existing values  
✓ **Minimal Typing** - Tab/scan instead of typing UIDs  
✓ **Clear Prompts** - Always shows what's current  
✓ **Confirmation** - Review before saving  
✓ **Error Handling** - Bad input just re-prompts  
✓ **Fast Navigation** - Letter keys for quick menu selection  
✓ **No Confusion** - One task per menu, clear flow

