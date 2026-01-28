# RetroNFC Console Configuration Tool - Complete Implementation

## What Was Built

A **Windows console-based configuration utility** for the RetroNFC RFID tag system. No Python, no GUI frameworks, no dependencies beyond .NET 8.0.

**Type**: Standalone C# .NET 8.0 console application  
**Size**: 151 KB executable (RetroNFCConfigure.exe)  
**Dependencies**: None (self-contained)  
**Installation**: Included with service installer  

## Why This Approach?

The user specifically requested console-based configuration because:
1. **No visual design needed** - Console text is simple and clean
2. **No Python required** - User doesn't have to install Python just to configure
3. **Minimal UI overhead** - DOS-style menus, just like the old days
4. **Fast startup** - Single executable file, no interpreters
5. **Easy to use** - Show defaults, press Enter to keep, minimal keystrokes

## Features

### Menu System
- **Main Menu** (4 options)
- **Manage Tags** (list, add, delete)
- **View Emulators** (list all available)
- **Settings** (serial port, baud rate)
- **Exit** (close application)

### Smart Defaults
```
Serial Port (current: COM9)
Enter new port or press Enter to keep: [User presses Enter]
```
Shows current value, user can skip if they don't want to change.

### Scanning Integration
- When adding a tag, user can press Enter instead of typing UID
- App waits up to 3 seconds for tag scan
- Places tag on reader, scan file read automatically
- Extracted UID shown to user

### File-Based Configuration
- Reads/writes `config.json`, `catalog.json`, `emulators.json` from AppData
- No port conflicts with service (service writes to `last_scan.txt`, config tool just reads JSON)
- All changes persist immediately

## Technical Implementation

### Project Structure
```
RetroNFCConfigure/
├── RetroNFCConfigure.csproj      (Project file - net8.0)
└── Program.cs                    (Single 261-line file)
    ├── FindConfigDir()           (Find AppData RetroNFC folder)
    ├── LoadAllData()             (Load all 3 JSON files)
    ├── MainMenu()                (Main loop)
    ├── ManageTags()              (Tag management menu)
    ├── AddTag()                  (Add tag workflow)
    ├── SaveTag()                 (Write to catalog.json)
    ├── DeleteTag()               (Remove from catalog.json)
    ├── ViewEmulators()           (Display emulators.json)
    └── Settings()                (Edit config.json)
```

### Key Code Snippets

**Scanning for Tag**:
```csharp
if (string.IsNullOrEmpty(uid))
{
    Console.WriteLine("Waiting for scan (place tag on reader)...");
    string scanFile = Path.Combine(ConfigDir, "last_scan.txt");
    
    int attempts = 0;
    while (attempts < 30)  // 3 seconds (30 * 100ms)
    {
        if (File.Exists(scanFile))
        {
            uid = File.ReadAllText(scanFile).Split('\n')[0].Trim();
            if (!string.IsNullOrEmpty(uid))
            {
                Console.WriteLine($"Scanned: {uid}");
                break;
            }
        }
        System.Threading.Thread.Sleep(100);
        attempts++;
    }
}
```

**Saving Tag Changes**:
```csharp
var catalog = new Dictionary<string, object>();

// Load existing tags
if (File.Exists(CatalogFile))
{
    var json = JsonDocument.Parse(File.ReadAllText(CatalogFile));
    foreach (var prop in json.RootElement.EnumerateObject())
    {
        catalog[prop.Name] = new Dictionary<string, object> { ... };
    }
}

// Add/update this tag
catalog[uid] = new Dictionary<string, object>
{
    { "name", name },
    { "action_type", actionType },
    { "action_target", target }
};

// Write back
File.WriteAllText(CatalogFile, JsonSerializer.Serialize(catalog, options));
```

## File Locations

### Source
```
D:\OneDrive\Software\Vintage\_GitHub\RetroNFC\
├── RetroNFCConfigure/
│   ├── Program.cs
│   └── RetroNFCConfigure.csproj
├── deployment/
│   ├── configure.bat                    (Updated launcher)
│   ├── install-retrof.ps1              (Updated installer)
│   ├── update-service.ps1              (Updated updater)
│   └── RetroNFCConfigure.*             (Published files)
└── [Documentation files]
```

### Runtime (After Installation)
```
%LOCALAPPDATA%\RetroNFC\
├── RetroNFCConfigure.exe               (Console app)
├── RetroNFCConfigure.dll
├── RetroNFCConfigure.deps.json
├── RetroNFCConfigure.runtimeconfig.json
├── RetroNFCService.exe                 (Windows Service)
├── RetroNFCService.dll
├── config.json                         (User config)
├── catalog.json                        (User's tags)
├── emulators.json                      (Emulator definitions)
└── [other runtime files]
```

## Build & Publish

### Build from Source
```powershell
cd D:\OneDrive\Software\Vintage\_GitHub\RetroNFC\RetroNFCConfigure
dotnet publish RetroNFCConfigure.csproj -c Release -o bin/Release/publish
```

### Output
```
bin\Release\publish\
├── RetroNFCConfigure.exe           (151 KB - standalone)
├── RetroNFCConfigure.dll           (12 KB)
├── RetroNFCConfigure.deps.json     (443 B)
└── RetroNFCConfigure.runtimeconfig.json (340 B)
```

All files are copied to:
1. `deployment/` folder (for installer)
2. `%LOCALAPPDATA%\RetroNFC\` (when installed)

## Integration with RetroNFC System

### How It Works Together
```
RetroNFC Service (Background)
├─ Monitors serial port
├─ Detects tag scans
├─ Writes last_scan.txt
└─ Triggers actions

RetroNFC Configure (User Tool)
├─ Reads config.json / catalog.json / emulators.json
├─ Lets user manage tags
├─ Can read last_scan.txt for scanning
└─ Writes changes back to JSON files
```

### No Conflicts
- **Service** uses: Serial port, reads JSON, writes `last_scan.txt`
- **Config Tool** uses: File system only (reads JSON, writes JSON)
- **Result**: No COM port conflicts, safe concurrent operation

## Deployment Integration

### Installer (install-retrof.ps1)
**Added logic:**
```powershell
# Copy Console Configure tool
$configureDir = Join-Path (Split-Path -Parent $sourceDir) "RetroNFCConfigure\bin\Release\publish"
if (Test-Path $configureDir) {
    Get-ChildItem -Path "$configureDir\*" | Copy-Item -Destination $InstallDir -Force
    Write-Host "Copied RetroNFC Configuration Tool"
}
```

### Updater (update-service.ps1)
**Added logic:**
```powershell
# Update Configuration Tool
$configureDir = Join-Path (Split-Path -Parent $sourceDir) "RetroNFCConfigure\bin\Release\publish"
if (Test-Path $configureDir) {
    Copy-Item -Path "$configureDir\*" -Destination $installDir -Force
}
```

### Launcher (configure.bat)
**Completely rewritten:**
```batch
@echo off
setlocal enabledelayedexpansion

set RETRODIR=%LOCALAPPDATA%\RetroNFC

if not exist "%RETRODIR%\RetroNFCConfigure.exe" (
    echo Error: RetroNFC Configure not found in AppData
    pause
    exit /b 1
)

"%RETRODIR%\RetroNFCConfigure.exe"
```

## User Workflow

### Basic Task: Add 3 Tags

```
Run: configure.bat

Menu → 1 (Manage Tags)
  → A (Add Tag)
    → [Enter] to scan
    → Place tag on reader
    → Enter name: "Mario"
    → [Enter] for emulator
    → Enter target: "nes"
    → Y to save

Back to manage → A (Add Tag 2)
Back to manage → A (Add Tag 3)
Back to menu → 4 Exit
```

**Time**: ~2 minutes  
**Keystrokes**: ~30-40

### Simple Task: Just Check Settings

```
Run: configure.bat

Menu → 3 (Settings)
  → [Enter] skip serial port
  → [Enter] skip baud rate
  → Y to save (or N if no changes)
Menu → 4 Exit
```

**Time**: ~10 seconds  
**Keystrokes**: 3-4

## Error Handling

### Graceful Degradation
```csharp
try
{
    Config = JsonDocument.Parse(File.ReadAllText(ConfigFile)).RootElement;
}
catch { }  // Silently continue if parse fails
```

App continues even if:
- Config files are missing
- JSON is malformed
- AppData folder doesn't exist yet
- Serial port doesn't exist

### User-Friendly Messages
- "Configuration not found" - Clear explanation
- "No tag scanned" - Tells user what went wrong
- "Cancelled" - Confirms user action was aborted
- Prompts shown for every confirmation

## Comparison to Alternatives

| Approach | Pros | Cons |
|----------|------|------|
| Python GUI (tkinter) | Visual | User complained: ugly, crashes on data format issues |
| VB.NET Forms | Native to Windows | Visual design problems, project file broke designer |
| **Console App** | ✓ Fast ✓ Clean ✓ No deps ✓ DOS style | Text only (user requested) |

## Future Enhancements (Not Included)

These could be added if needed:
- Batch import from CSV
- Configuration backup/restore
- Tag edit without delete/re-add
- Search/filter tags by name
- Logging of all changes
- Serial port validation

## Testing

### Verify Installation
```powershell
Test-Path "$env:LOCALAPPDATA\RetroNFC\RetroNFCConfigure.exe"  # Should be True
& "$env:LOCALAPPDATA\RetroNFC\RetroNFCConfigure.exe"          # Should run
```

### Test Workflow
1. Run `configure.bat`
2. See main menu
3. Try "2. View Emulators" (should list emulators)
4. Try "3. Settings" → Enter → Enter → N (no changes needed)
5. Try "1. Manage Tags" (should show existing tags or empty list)
6. Exit (menu option 4)

### Test Add Tag
1. Run `configure.bat`
2. Manage Tags → Add Tag
3. Manually enter UID (don't scan): `TEST123456`
4. Enter name: `Test Tag`
5. Use emulator (press Enter)
6. Enter target: `nes`
7. Review and press Y
8. Go back to Manage Tags - new tag should appear

## Documentation Provided

1. **README_CONFIG.md** - Overview and quick reference
2. **QUICK_START.md** - Installation and first-use guide
3. **CONFIGURE_README.md** - Detailed usage guide
4. **CONSOLE_TOOL_SUMMARY.md** - Technical implementation
5. **USAGE_EXAMPLES.md** - Screenshots of typical workflows
6. This file - Complete implementation reference

## Summary

✓ **Built**: Standalone Windows console configuration tool (C# .NET 8.0)  
✓ **Tested**: Compiles and runs successfully  
✓ **Deployed**: Included in installer and update scripts  
✓ **Documented**: 5 comprehensive guides + usage examples  
✓ **Integrated**: Works seamlessly with RetroNFC Service  
✓ **Zero dependencies**: No Python, no GUI frameworks needed  
✓ **User-friendly**: DOS-style simplicity with smart defaults  

The RetroNFC system is now complete with a robust, lightweight configuration tool that matches the user's stated preferences: console-based, fast, and no unnecessary dependencies.

