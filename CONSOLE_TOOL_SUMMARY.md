# RetroNFC Configuration Tool - Implementation Summary

## What Was Built

A **standalone Windows console application** (no Python, no GUI framework dependencies) for configuring the RetroNFC service.

## Key Characteristics

✓ **Console-based** - DOS-style menus, text-only interface  
✓ **Zero dependencies** - Self-contained .NET executable  
✓ **Smart defaults** - Shows current value, press Enter to keep it  
✓ **Minimal keystrokes** - Only type when making changes  
✓ **Fast startup** - Single EXE file, no interpreters to load  

## What It Does

### Main Menu Options
1. **Manage Tags** - Add/delete RFID tag associations
2. **View Emulators** - Display configured emulator systems  
3. **Settings** - Configure serial port and baud rate
4. **Exit** - Close application

### Add Tag Flow
```
Enter UID (or blank to scan)
  └─ Scan: App waits 3 seconds with "place tag on reader" prompt
     
Enter Tag Name
     
Select Action Type (1=emulator, 2=file, 3=url, 4=command)
     
Enter Target (emulator name, file path, URL, or command)
     
Review & Press Y to Save or N to Cancel
```

### Settings Flow
```
Serial Port (current: COM9)
  └─ Press Enter to keep, or type new port
     
Baud Rate (current: 115200)
  └─ Press Enter to keep, or type new baud
     
Press Y to Save or N to Cancel
```

## Technical Implementation

**Language**: C# .NET 8.0  
**Project**: `RetroNFCConfigure.csproj` in workspace root  
**Entry Point**: `Program.cs` (single file, ~260 lines)  

**Dependencies**: Only .NET Framework libraries (System.IO, System.Text.Json, etc.)

**JSON Parsing**: Uses System.Text.Json with error handling - won't crash on malformed files

**File Locations**:
- Executable: `%LOCALAPPDATA%\RetroNFC\RetroNFCConfigure.exe`
- Config files: `%LOCALAPPDATA%\RetroNFC\*.json`
- Batch launcher: `deployment\configure.bat`

## Integration with Existing System

### How It Fits
- **Reads** from: `config.json`, `catalog.json`, `emulators.json` in AppData
- **Writes** to: Same JSON files (updates modified values)
- **Scans** from: `last_scan.txt` (file-based IPC with service)
- **No conflicts** with: RetroNFCService (doesn't access COM port directly)

### Deployment
The installer and update scripts were modified to:
1. Build RetroNFCConfigure project
2. Publish to `RetroNFCConfigure\bin\Release\publish\`
3. Copy all files to `%LOCALAPPDATA%\RetroNFC\`
4. Create/update `configure.bat` launcher

### How to Use It
1. Run `deployment\configure.bat` (double-click or command line)
2. App finds config files in AppData
3. Display menu options
4. User makes changes
5. Changes written directly to JSON files
6. No service restart needed

## Comparison to Previous Attempts

| Approach | Result | Issues |
|----------|--------|--------|
| Python tkinter GUI | Created | Ugly, user complained, crash on dict/list catalog mixing |
| VB.NET Windows Forms | Created | Project file broke designer, visual design problems |
| **Console app** | ✓ Working | Fast, clean, no external deps, DOS simplicity |

## Installation & Update

### First Installation
```powershell
cd deployment
.\install-retrof.bat  # (runs as admin)
```

The installer now:
1. Creates AppData RetroNFC folder
2. Copies service binaries
3. **Copies console configurator**
4. Copies/creates config JSON files
5. Registers Windows Service

### Updates
```powershell
cd deployment
.\update-service.bat  # (runs as admin)
```

Updates both:
1. Service binaries
2. **Console configurator**

## Files Modified/Created

**New Files**:
- `RetroNFCConfigure/RetroNFCConfigure.csproj` - Project file
- `RetroNFCConfigure/Program.cs` - Console app (261 lines)
- `CONFIGURE_README.md` - User documentation

**Modified Files**:
- `deployment/configure.bat` - Changed from Python launcher to .NET launcher
- `deployment/install-retrof.ps1` - Added console app copying
- `deployment/update-service.ps1` - Added console app update

## Design Decisions Explained

### Why Console, Not GUI?
- No visual design overhead (the user's stated problem with previous attempts)
- Instant startup (no framework loading)
- No dependencies beyond .NET 8.0 (already required for service)
- DOS-style interface matches stated preference
- Minimal keystrokes while maintaining usability

### Why Single File Program.cs?
- Console app doesn't need separation of concerns
- All related logic stays together
- Easy to modify or debug
- Fast compile/publish cycle

### Why Press Y/N Instead of Enter?
- Forces intentional confirmation
- Clear feedback (Y/N prompt visible)
- Matches DOS convention

### Why Show Defaults?
- User can see current state without opening files
- Press Enter = keep default (fewest keystrokes)
- Quick visual check before typing new value

## Testing the App

1. **Run locally**:
   ```powershell
   cd RetroNFCConfigure
   dotnet run
   ```

2. **Run from AppData**:
   ```powershell
   & "$env:LOCALAPPDATA\RetroNFC\RetroNFCConfigure.exe"
   ```

3. **Run via batch file**:
   ```batch
   deployment\configure.bat
   ```

## Next Steps (If Needed)

1. **Add tag deletion by name** - Currently requires UID
2. **Add config backup/restore** - Save/load settings
3. **Add logging** - Log all changes to file
4. **Add validation** - Check COM port availability
5. **Add batch import** - Load tags from CSV

But the current version handles all core configuration needs with minimal friction.

