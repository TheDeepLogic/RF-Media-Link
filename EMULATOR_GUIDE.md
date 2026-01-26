# RetroNFC Emulator System - Customization Guide

## Adding Custom Emulators

### Method 1: GUI (Simple)

1. Run RetroNFC
2. Press `e` to open "Add Emulator" dialog
3. Enter emulator name and executable path
4. Click Create

This creates a basic emulator entry. You'll need to edit `emulators.json` for advanced configuration.

### Method 2: Direct JSON Editing (Advanced)

Edit `emulators.json` directly:

```json
{
  "your_emulator_id": {
    "name": "Display Name",
    "executable": "path/to/executable.exe",
    "arguments": [
      // Define your arguments here
    ]
  }
}
```

## Emulator Configuration Examples

### MAME Arcade

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

### Atari 2600

```json
{
  "stella": {
    "name": "Stella (Atari 2600)",
    "executable": "C:\\stella\\stella.exe",
    "arguments": [
      {
        "name": "rom",
        "type": "file",
        "label": "Game ROM",
        "flag": "",
        "required": true
      },
      {
        "name": "console",
        "type": "choice",
        "label": "Console Type",
        "choices": ["2600", "2600p", "2600jr"],
        "default": "2600"
      }
    ]
  }
}
```

### NES (FCEUX)

```json
{
  "fceux": {
    "name": "FCEUX (NES)",
    "executable": "C:\\fceux\\fceux.exe",
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
        "flag": "--fullscreen"
      }
    ]
  }
}
```

### DOSBox

```json
{
  "dosbox": {
    "name": "DOSBox",
    "executable": "C:\\dosbox\\dosbox.exe",
    "arguments": [
      {
        "name": "config",
        "type": "file",
        "label": "Config File",
        "flag": "-conf"
      },
      {
        "name": "autostart_game",
        "type": "text",
        "label": "Game to Run",
        "flag": "-c"
      }
    ]
  }
}
```

## Argument Type Reference

### file

File picker dialog. Shows Windows file browser.

```json
{
  "name": "rom",
  "type": "file",
  "label": "Game ROM",
  "flag": "",
  "required": true
}
```

When user selects file: `{executable} {path_to_file}`

### text

Text input field.

```json
{
  "name": "player_name",
  "type": "text",
  "label": "Player Name",
  "flag": "-player",
  "default": "Player 1"
}
```

When user enters text: `{executable} -player "Player 1"`

### choice

Dropdown selector.

```json
{
  "name": "model",
  "type": "choice",
  "label": "Console Type",
  "flag": "-model",
  "choices": ["2600", "2600p", "2600jr"],
  "default": "2600"
}
```

When user selects option: `{executable} -model 2600`

### toggle

Checkbox (on/off).

```json
{
  "name": "fullscreen",
  "type": "toggle",
  "label": "Fullscreen",
  "flag": "-f"
}
```

When checked: `{executable} -f`
When unchecked: `` (flag omitted entirely)

## Command-Line Building

RetroNFC builds command lines by:

1. Start with executable path
2. For each argument in order:
   - If type is "toggle":
     - If checked: add `{flag}`
     - If unchecked: skip
   - If type is "file", "text", or "choice":
     - If value provided: add `{flag} {value}`
     - If no value and not required: skip
     - If no value and required: error dialog

### Example: AppleWin with Full Configuration

```
C:\AppleWin\AppleWin-x64.exe -model apple2e -d1 game.dsk -d2 system.dsk -s7 hdc -s7h1 harddisk.hd -f
```

Breaks down as:
- Executable: `C:\AppleWin\AppleWin-x64.exe`
- Model flag: `-model apple2e`
- Drive 1: `-d1 game.dsk`
- Drive 2: `-d2 system.dsk`
- S7 controller: `-s7 hdc`
- Hard drive: `-s7h1 harddisk.hd`
- Fullscreen: `-f` (toggle, checked)

## Tips & Best Practices

### Flag vs. Positional Arguments

If an emulator uses positional arguments (first argument is filename):

```json
{
  "name": "rom",
  "type": "file",
  "label": "ROM File",
  "flag": "",  // Empty flag = positional argument
  "required": true
}
```

This will produce: `emulator.exe C:\path\to\game.rom`

If an emulator uses flagged arguments:

```json
{
  "name": "rom",
  "type": "file",
  "label": "ROM File",
  "flag": "-rom",  // Flag name
  "required": true
}
```

This will produce: `emulator.exe -rom C:\path\to\game.rom`

### Required vs. Optional Arguments

- `"required": true` - User must provide value, or error dialog appears
- `"required": false` - User can leave blank, argument is omitted

### Default Values

```json
{
  "name": "model",
  "type": "choice",
  "label": "Model",
  "choices": ["v1", "v2", "v3"],
  "default": "v2"  // Pre-selected in dialog
}
```

### Relative Paths

Emulator paths are stored absolute, but relative paths work too:

```json
"executable": ".\\emulators\\stella.exe"
```

## Troubleshooting

### "Command not found" Error

**Problem**: Emulator not launching, file not found error

**Solution**: 
- Verify the `executable` path is correct
- Use full paths, not relative ones
- Try with .exe extension included
- Check file actually exists with Windows Explorer

### Argument Not Being Passed

**Problem**: Emulator launches but argument/ROM not loaded

**Possible Causes**:
- Wrong flag name for your emulator (check docs)
- Flag expects a specific format (e.g., `-fullscreen yes` not just `-fullscreen`)
- Positional argument in wrong position

**Solution**:
- Check your emulator's documentation for exact flag syntax
- Test command line manually: `emulator.exe -flag value`
- Adjust flag string to match exactly what emulator expects

### Dialog Showing Unexpected Options

**Problem**: Choices dropdown shows wrong options

**Solution**: 
- Check `emulators.json` → `arguments` → `choices` array
- Verify JSON syntax (missing commas, quotes, etc.)
- Reload application after editing JSON

## Advanced: Custom Argument Processing

For complex emulators, you may need to:

1. Create script wrapper
2. Wrapper parses RetroNFC arguments
3. Wrapper converts to emulator's format
4. Wrapper launches actual emulator

Example:

```bash
#!/bin/bash
# mame-wrapper.sh
# Usage: mame-wrapper.sh -rom game.zip -fullscreen -speed 150

while [[ $# -gt 0 ]]; do
  case $1 in
    -rom) ROM="$2"; shift 2 ;;
    -fullscreen) MAME_ARGS+=("-fullscreen"); shift ;;
    -speed) MAME_ARGS+=("-speed" "$2"); shift 2 ;;
    *) shift ;;
  esac
done

/path/to/mame "${MAME_ARGS[@]}" "$ROM"
```

Then in `emulators.json`:

```json
{
  "mame": {
    "executable": "C:\\scripts\\mame-wrapper.sh",
    // ... define arguments as expected by wrapper
  }
}
```

## Getting Help

- Check this guide first
- Review existing emulator configs in `emulators.json`
- Look at similar emulators for patterns
- Test command manually in Command Prompt/PowerShell first

---

**Happy emulating!** 🎮
