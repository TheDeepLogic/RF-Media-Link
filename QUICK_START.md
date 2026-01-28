# RetroNFC Quick Start Guide

## Installation

1. **Run installer as Administrator**
   ```batch
   deployment\install-retrof.bat
   ```
   
   This:
   - Creates `%LOCALAPPDATA%\RetroNFC\` folder
   - Installs RetroNFC Windows Service
   - Copies configuration tool
   - Sets up config files

2. **Verify installation**
   ```powershell
   Get-Service RetroNFC  # Should show "Running"
   Get-Item $env:LOCALAPPDATA\RetroNFC\*.exe  # Should show 2 EXE files
   ```

## Configure Your System

Double-click **`deployment\configure.bat`** or run:
```
%LOCALAPPDATA%\RetroNFC\RetroNFCConfigure.exe
```

### First Run Checklist

1. **Check Settings** (Option 3)
   - Verify Serial Port matches your RFID reader (default: COM9)
   - Verify Baud Rate (default: 115200)
   - Press Enter twice if these are correct

2. **Add an Emulator Tag** (Option 1 → A)
   - Place tag on reader when prompted, or enter UID manually
   - Give it a friendly name (e.g., "My NES Game")
   - Select action type 1 (emulator)
   - Enter target emulator name (e.g., "nes", "snes", etc.)
   - Press Y to save

3. **View Available Emulators** (Option 2)
   - See what emulator names are available
   - Use these names when adding tags

## Daily Use

```
Main Menu
├─ 1. Manage Tags
│  ├─ List current tags
│  ├─ A = Add new tag
│  └─ D = Delete tag
│
├─ 2. View Emulators
│  └─ See available emulator systems
│
├─ 3. Settings
│  └─ Change serial port or baud rate
│
└─ 4. Exit
```

### Adding Tags Quickly

- **Enter UID or press Enter to scan**: Just leave it blank and place tag on reader within 3 seconds
- **Press Enter to keep defaults**: When in Settings, skip unchanged values
- **Y/N confirms**: Always review the summary before pressing Y

## Testing

1. **Service is running**:
   ```powershell
   Get-Service RetroNFC | Select-Object Status
   ```

2. **Console tool launches**:
   ```batch
   %LOCALAPPDATA%\RetroNFC\RetroNFCConfigure.exe
   ```

3. **Scan a tag**:
   - Place tag on reader
   - Check if `last_scan.txt` appears in `%LOCALAPPDATA%\RetroNFC\`

## Troubleshooting

### Service won't start
```powershell
# Check Event Viewer for errors
Get-EventLog -LogName Application -Source RetroNFC -Newest 10
```

### Can't find config files
- Check: `%LOCALAPPDATA%\RetroNFC\` (usually `C:\Users\YourName\AppData\Local\RetroNFC\`)
- Run installer again if missing

### Console tool won't run
- Make sure .NET 8.0 runtime is installed
- Check that console tool files are in AppData folder

### Serial port errors
- Verify COM port number (check Device Manager)
- Make sure RFID reader is powered on and connected
- Try unplugging and replugging the reader

## Configuration Files (AppData)

Located in: `%LOCALAPPDATA%\RetroNFC\`

| File | Purpose |
|------|---------|
| `config.json` | Serial port and baud settings |
| `catalog.json` | Your RFID tags and their actions |
| `emulators.json` | Available emulator systems |
| `last_scan.txt` | Last scanned tag (temporary) |
| `RetroNFCConfigure.exe` | Configuration tool |
| `RetroNFCService.exe` | Windows Service (don't run directly) |

## Update Service

```powershell
cd deployment
.\update-service.bat
```

This updates both the service and configuration tool in one command.

## Uninstall

```powershell
cd deployment
.\install-retrof.ps1 -Uninstall  # As Administrator
```

This removes the service and all files from AppData.

