# RetroNFC Architecture & Flow Diagrams

## System Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        RetroNFC System                          │
└─────────────────────────────────────────────────────────────────┘

┌──────────────────┐
│  RFID Reader     │ (USB CDC serial; port from config.json, 115200 baud)
│  (Hardware)      │
└────────┬─────────┘
         │ UID: 041234567...
         │
         ▼
┌──────────────────────────────────────────────────────────────────┐
│                      main.py                                    │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │ Serial Listener (Main Loop)                               │ │
│  │ - Reads from COM9                                         │ │
│  │ - Checks for keyboard input                               │ │
│  │ - Handles UID → Action lookup                             │ │
│  └──┬─────────────────────────────────────────────────────┬──┘ │
│     │                                                      │     │
│  ┌──▼─────────────────────────┐  ┌─────────────────────┬─▼────┐
│  │ Dialogue System (tkinter)  │  │ File System (JSON)  │      │
│  │                            │  │                     │      │
│  │ - Emulator Selection       │  │ - catalog.json      │      │
│  │ - Argument Configuration   │  │ - config.json       │      │
│  │ - Mode Selection (S/B)     │  │ - emulators.json    │      │
│  │ - File Browser             │  │                     │      │
│  │ - Command Type Selection   │  │                     │      │
│  │ - Add Emulator             │  │                     │      │
│  └────────────────────────────┘  └─────────────────────┘      │
└──────────────────────────────────────────────────────────────────┘
         │
         ├─────────────────────┬──────────────────────┐
         │                     │                      │
         ▼                     ▼                      ▼
    ┌────────┐         ┌──────────┐         ┌──────────────┐
    │ File   │         │ Hotkey   │         │ Emulator     │
    │ Exec   │         │ Command  │         │ Launcher     │
    │        │         │          │         │              │
    │ pyau-  │         │ pyautogui│         │ subprocess   │
    │ togui  │         │          │         │ .Popen()     │
    └────────┘         └──────────┘         └──────────────┘
```

## Tag Adding Flow

```
User presses 'a'
       │
       ▼
    ┌──────────────┐
    │ Prompt Mode  │
    │ Single/Batch │
    └──────────────┘
       │
       ├─── Single Mode ─────┐
       │                     │
       │              Batch Mode
       │                     │
       ▼                     ▼
   Prompt           Remember Type
   Command Type     Clear for next
       │            scan
       │
       ▼
   Prompt Action Type
   (emulator, file, url,
    command, hotkey, shell)
       │
       ├─── "emulator" ─────┐
       │                    │
       │              (other types
       │               handled)
       │
       ▼
   Prompt Emulator Selection
   [RadioButton Dialog]
       │
       ├─ AppleWin
       ├─ MAME
       ├─ Stella
       └─ (others)
       │
       ▼
   Prompt Emulator Config
   [Scrollable Form]
       │
       ├─ file: Browse Button
       ├─ text: Text Entry
       ├─ choice: Dropdown
       └─ toggle: Checkbox
       │
       ▼
   "Ready to scan..."
   [Wait for serial UID]
       │
       ▼
   UID Received
       │
       ├─ Ask overwrite?
       │
       ▼
   Save to catalog.json
   {UID: {emulator, config}}
       │
       ▼
   [Continue batch if enabled]
```

## Tag Execution Flow

```
User scans RFID tag
       │
       ▼
   Serial Reader gets UID
       │
       ▼
   lookup_in_catalog(UID)
       │
       ├─ Found ───────────┐
       │                   │
       └─ Not found ──────┐│
                          ││
                    Ask add?
                          ││
                    Yes ───┘│
                          │ │
                    Goto   │ │
                    Add Tag │ │
                    Flow    │ │
                          │ │
                          └┼┘
                           │
                           ▼
                    execute_action()
                           │
                    ┌──────┼──────┐
                    │      │      │
                ┌───▼─┐  ┌─▼──┐  │
                │File │  │URL │  │
                │Exec │  │Open│  │
                └─────┘  └────┘  │
                                 │
                        ┌────────▼────────┐
                        │ Check emulator? │
                        └────────┬────────┘
                                 │ Yes
                                 ▼
                    Load from emulators.json
                           │
                           ▼
                    Build command line:
                    exe + flags + args
                           │
                           ▼
                    subprocess.Popen()
                           │
                           ▼
                    Emulator Launches
                    (AppleWin, MAME, etc.)
```

## Command-Line Building (Emulator)

```
emulators.json:
┌─────────────────────────────────────────┐
│ applewin: {                             │
│   executable: "AppleWin-x64.exe"        │
│   arguments: [                          │
│     {name: "model", type: choice,       │
│      flag: "-model", ...}               │
│     {name: "s6d1", type: file,          │
│      flag: "-d1", ...}                  │
│     {name: "fullscreen", type: toggle,  │
│      flag: "-f", ...}                   │
│   ]                                     │
│ }                                       │
└─────────────────────────────────────────┘

User selects:
- model: "apple2e"
- s6d1: "C:\game.dsk"
- fullscreen: checked

Built command line:
┌─────────────────────────────────────────────────┐
│ AppleWin-x64.exe -model apple2e -d1 C:\game.dsk │
                                      -f           │
└─────────────────────────────────────────────────┘

Execution:
subprocess.Popen([
  "AppleWin-x64.exe",
  "-model", "apple2e",
  "-d1", "C:\game.dsk",
  "-f"
])
```

## File Structure & Data Flow

```
File System:
┌─────────────────────────────────────────┐

catalog.json (User Data)
├─ UID1: {emulator: "applewin", config: {...}}
├─ UID2: {file: "C:\game.exe"}
└─ UID3: {url: "https://..."}

config.json (User Preferences)
├─ last_browse_path: "C:\disks"
├─ last_command_type: "emulator"
└─ last_mode: "single"

emulators.json (Definitions)
├─ applewin: {
│   ├─ name: "AppleWin"
│   ├─ executable: "..."
│   └─ arguments: [...]
├─ mame: {...}
└─ stella: {...}

└─────────────────────────────────────────┘

Data Flow:
┌──────────────────────────────────────────┐

1. Load emulators.json
   → populate emulator selection list

2. User selects "applewin"
   → show argument config dialog
   → populate from last config if available

3. User fills in arguments
   → save to config.json (last_browse_path)

4. User scans tag
   → save to catalog.json as:
     {UID: {emulator: "applewin", config: {...}}}

5. Tag read during execution
   → load from catalog.json
   → look up emulator in emulators.json
   → build command line from config
   → execute emulator

└──────────────────────────────────────────┘
```

## Dialog System Architecture

```
Dialog Classes:
┌──────────────────────────────────────────┐
│ All dialogs use independent tk.Tk()      │
│ (not Toplevel with transient root)       │
│                                          │
│ Advantages:                              │
│ - Proper event handling                  │
│ - Correct screen centering               │
│ - Cancel works reliably                  │
│ - No modal focus issues                  │
└──────────────────────────────────────────┘

Geometry Calculation:
┌──────────────────────────────────────────┐
│ 1. Create dialog: tk.Tk()                │
│ 2. Pack all widgets                      │
│ 3. Call dialog.update() to get size      │
│ 4. Calculate center position:            │
│    x = (screen_width // 2) - (w // 2)   │
│    y = (screen_height // 2) - (h // 2)  │
│ 5. Set geometry: "WxH+X+Y"              │
│ 6. Focus and wait_window()               │
└──────────────────────────────────────────┘
```

## Argument Widget Selection

```
Argument Type Mapping:
┌──────────────┬─────────────────────────┐
│ Type         │ Widget                  │
├──────────────┼─────────────────────────┤
│ "file"       │ Entry + Browse Button   │
│              │ Opens file dialog       │
├──────────────┼─────────────────────────┤
│ "text"       │ Entry Box               │
│              │ Free-form text input    │
├──────────────┼─────────────────────────┤
│ "choice"     │ OptionMenu (Dropdown)   │
│              │ Pre-defined options     │
├──────────────┼─────────────────────────┤
│ "toggle"     │ Checkbutton             │
│              │ On/off flag             │
└──────────────┴─────────────────────────┘

Canvas with Scrollbar:
┌──────────────────────────┐
│ Top:                     │
│ Argument labels          │
│ (scrollable)             │
│ ┌──────────────┐         │
│ │ [Widget 1]   │ ◄──────┐│
│ │ [Widget 2]   │   Row  ││
│ │ [Widget 3]   │        ││
│ │ ...          │ ──────►││
│ └──────────────┘◄──────┐│
│                Scrollbar││
│ Bottom:                 │
│ [OK] [Cancel]           │
└──────────────────────────┘
```

## State Machine (Simplified)

```
States:
┌─────────────────────────────────────────┐
│ IDLE                                    │
│ ├─ waiting for serial UID               │
│ ├─ waiting for keyboard cmd              │
│ └─ displaying current status            │
│                                         │
│ ADD_TAG_MODE                            │
│ ├─ mode selected (single/batch)         │
│ ├─ action type selected                 │
│ ├─ (if emulator) emulator selected      │
│ ├─ configuration complete               │
│ └─ awaiting UID scan                    │
│                                         │
│ BATCH_MODE                              │
│ ├─ action type locked                   │
│ ├─ awaiting UID for each tag            │
│ └─ ready for "done" command             │
│                                         │
│ EXECUTE_ACTION                          │
│ ├─ UID looked up                        │
│ ├─ action determined                    │
│ └─ process launched                     │
└─────────────────────────────────────────┘

Transitions:
IDLE + 'a' → ADD_TAG_MODE
IDLE + 'e' → ADD_EMULATOR_MODE
IDLE + UID → EXECUTE_ACTION → IDLE

ADD_TAG_MODE + all prompts complete → awaiting UID
awaiting UID + scan → save + IDLE (or continue batch)

BATCH_MODE + 'done' → IDLE
BATCH_MODE + UID → save + awaiting next UID
```

## Integration Points

```
Main Components:
┌────────────────────────────────────────────────────────┐
│                                                        │
│  serial.Serial                                         │
│  └─ reads UID from RFID hardware                       │
│                                                        │
│  tkinter (tk, simpledialog, filedialog, messagebox)   │
│  └─ all user interface dialogs                         │
│                                                        │
│  json (load/dump)                                      │
│  └─ catalog, config, emulators                         │
│                                                        │
│  subprocess.Popen()                                    │
│  └─ launch emulators and other applications            │
│                                                        │
│  pyautogui                                             │
│  └─ hotkey execution (alt+f4, etc.)                    │
│                                                        │
│  threading                                             │
│  └─ background keyboard input listener                 │
│                                                        │
│  msvcrt (Windows only)                                 │
│  └─ non-blocking keyboard input                        │
│                                                        │
└────────────────────────────────────────────────────────┘
```

## Deployment Diagram

```
User Installation:
┌─────────────────────────────────────┐
│ 1. Clone from GitHub                │
│    git clone logicnfc               │
│                                     │
│ 2. Install Python 3.7+              │
│                                     │
│ 3. Install dependencies             │
│    pip install -r requirements.txt  │
│                                     │
│ 4. Connect RFID reader to COM9      │
│    (or update PORT in main.py)      │
│                                     │
│ 5. Run application                  │
│    python main.py                   │
│                                     │
│ 6. Start scanning RFID tags!        │
└─────────────────────────────────────┘

Portable Package (Optional):
┌─────────────────────────────────────┐
│ - Include Python interpreter        │
│ - Bundle all dependencies            │
│ - Create standalone .exe             │
│ - Add launcher script                │
│ - Zero configuration for users       │
└─────────────────────────────────────┘
```

---

This architecture supports:
- ✅ Easy emulator addition
- ✅ Extensible action types
- ✅ JSON-based configuration
- ✅ User-friendly dialogs
- ✅ Reliable serial communication
- ✅ Cross-platform (Windows/Linux/Mac)
