# RF Media Link Service Installer
# Run as Administrator

param(
    [switch]$Uninstall
)

$InstallDir = "$env:ProgramData\RFMediaLink"
$ServiceName = "RF Media Link"
$DesktopPath = [Environment]::GetFolderPath("Desktop")
$StartMenuPath = [Environment]::GetFolderPath("StartMenu")
$StartMenuFolder = Join-Path $StartMenuPath "Programs\RF Media Link"

function Test-Administrator {
    $currentUser = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($currentUser)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Create-Shortcut {
    param(
        [string]$TargetPath,
        [string]$ShortcutPath,
        [string]$Description,
        [string]$WorkingDirectory = "",
        [string]$IconLocation = "",
        [string]$Arguments = ""
    )
    
    try {
        $shell = New-Object -ComObject WScript.Shell
        $shortcut = $shell.CreateShortcut($ShortcutPath)
        $shortcut.TargetPath = $TargetPath
        $shortcut.Description = $Description
        if ($WorkingDirectory) {
            $shortcut.WorkingDirectory = $WorkingDirectory
        }
        if ($IconLocation) {
            $shortcut.IconLocation = $IconLocation
        }
        if ($Arguments) {
            $shortcut.Arguments = $Arguments
        }
        $shortcut.Save()
        Write-Host "Created shortcut: $ShortcutPath"
    }
    catch {
        Write-Warning "Failed to create shortcut $ShortcutPath : $_"
    }
}

if (-not (Test-Administrator)) {
    Write-Error "This script must be run as Administrator!"
    exit 1
}

if ($Uninstall) {
    Write-Host "Uninstalling RF Media Link Service..."
    
    # Stop service with force
    $service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
    if ($service) {
        Write-Host "Stopping service..."
        Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 2
        
        # Force kill any remaining processes
        Get-Process -Name "RFMediaLinkService" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 1
        
        Write-Host "Deleting service..."
        sc.exe delete $ServiceName | Out-Null
        Start-Sleep -Seconds 2
        Write-Host "Service uninstalled"
    }
    
    # Remove desktop shortcut
    $desktopShortcut = Join-Path $DesktopPath "RF Media Link Configure.lnk"
    if (Test-Path $desktopShortcut) {
        Remove-Item -Path $desktopShortcut -Force -ErrorAction SilentlyContinue
        Write-Host "Desktop shortcut removed"
    }
    
    # Also try old shortcut name for backward compatibility
    $oldDesktopShortcut = Join-Path $DesktopPath "RF Media Link.lnk"
    if (Test-Path $oldDesktopShortcut) {
        Remove-Item -Path $oldDesktopShortcut -Force -ErrorAction SilentlyContinue
    }
    
    # Remove Start Menu folder
    if (Test-Path $StartMenuFolder) {
        Remove-Item -Path $StartMenuFolder -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "Start Menu folder removed"
    }
    
    # Remove files with retry
    if (Test-Path $InstallDir) {
        Write-Host "Removing installation directory..."
        Start-Sleep -Seconds 1
        Remove-Item -Path $InstallDir -Recurse -Force -ErrorAction SilentlyContinue
        if (Test-Path $InstallDir) {
            Write-Warning "Some files could not be removed. They may be in use."
        } else {
            Write-Host "Installation directory removed"
        }
    }
    
    Write-Host "Uninstall complete"
    exit 0
}

# Install
Write-Host "Installing RF Media Link to $InstallDir..."

# Create installation directory
if (-not (Test-Path $InstallDir)) {
    New-Item -ItemType Directory -Path $InstallDir -Force | Out-Null
}

# Set permissions on the directory so regular users can read/write
try {
    $acl = Get-Acl $InstallDir
    $rule = New-Object System.Security.AccessControl.FileSystemAccessRule("Users", "Modify", "ContainerInherit,ObjectInherit", "None", "Allow")
    $acl.SetAccessRule($rule)
    Set-Acl $InstallDir $acl
    Write-Host "Permissions set on $InstallDir"
}
catch {
    Write-Warning "Failed to set permissions on $InstallDir : $_"
}

# Copy from actual build folders - no guessing
$parentDir = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)

# Copy Service files from service publish folder
$servicePublishDir = Join-Path $parentDir "RFMediaLinkService\bin\Release\net8.0-windows\publish"
if (Test-Path $servicePublishDir) {
    Write-Host "Copying service files from $servicePublishDir..."
    $filesCopied = 0
    Get-ChildItem -Path $servicePublishDir -Recurse | ForEach-Object {
        $targetPath = $_.FullName.Replace($servicePublishDir, $InstallDir)
        if ($_.PSIsContainer) {
            if (-not (Test-Path $targetPath)) {
                New-Item -ItemType Directory -Path $targetPath -Force | Out-Null
            }
        } else {
            Copy-Item -Path $_.FullName -Destination $targetPath -Force
            $filesCopied++
        }
    }
    Write-Host "Service files copied: $filesCopied files"
} else {
    Write-Error "Service build folder not found: $servicePublishDir"
    pause
    exit 1
}

# Copy Configurator files from configurator publish folder  
$configurePublishDir = Join-Path $parentDir "RFMediaLink\bin\Release\net8.0-windows\publish"
if (Test-Path $configurePublishDir) {
    Write-Host "Copying configurator files from $configurePublishDir..."
    Get-ChildItem -Path $configurePublishDir -Filter "RFMediaLink.*" | Copy-Item -Destination $InstallDir -Force
    Write-Host "Configurator files copied"
} else {
    Write-Error "Configurator build folder not found: $configurePublishDir"
    exit 1
}

# Copy JSON config files (only if they don't exist)
$sourceDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$configFiles = @(
    @{ Name = "config.json"; Source = Join-Path $parentDir "config.json" }
    @{ Name = "catalog.json"; Source = Join-Path $parentDir "catalog.json" }
    @{ Name = "emulators.json"; Source = Join-Path $parentDir "inc\emulators.json" }
)

foreach ($file in $configFiles) {
    $destPath = "$InstallDir\$($file.Name)"
    if (Test-Path $file.Source) {
        if (-not (Test-Path $destPath)) {
            Copy-Item -Path $file.Source -Destination $destPath
            Write-Host "Copied $($file.Name)"
        } else {
            Write-Host "Skipped $($file.Name) (already exists)"
        }
    }
}

# Copy service management scripts
$deploymentDir = Split-Path -Parent $MyInvocation.MyCommand.Path
foreach ($file in @("start-service.ps1", "stop-service.ps1", "restart-service.ps1", "update-service.ps1", "uninstall.ps1", "open-catalog.bat", "open-emulators.bat")) {
    $sourcePath = Join-Path $deploymentDir $file
    $destPath = "$InstallDir\$file"
    if (Test-Path $sourcePath) {
        Copy-Item -Path $sourcePath -Destination $destPath -Force
        Write-Host "Copied $file"
    }
}

Write-Host "Files copied to $InstallDir"

# Remove old Windows Service if it exists
$existingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($existingService) {
    Write-Host "Removing old Windows Service..."
    Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
    
    # Force kill any remaining processes
    for ($i = 0; $i -lt 3; $i++) {
        Get-Process -Name "RFMediaLinkService" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 1
    }
    
    sc.exe delete $ServiceName | Out-Null
    Start-Sleep -Seconds 2
    Write-Host "Old service removed"
}

# Kill any running instances
Write-Host "Stopping any running instances..."
Get-Process -Name "RFMediaLinkService" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 1

# Add to startup folder instead of Windows Service
Write-Host "Adding to Windows Startup..."
$exePath = "$InstallDir\RFMediaLinkService.exe"
$StartupFolder = [Environment]::GetFolderPath("Startup")
$startupShortcutPath = Join-Path $StartupFolder "RF Media Link.lnk"

$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut($startupShortcutPath)
$shortcut.TargetPath = $exePath
$shortcut.WorkingDirectory = $InstallDir
$shortcut.Description = "RF Media Link RFID Service"
$shortcut.WindowStyle = 7  # Minimized
$shortcut.Save()

Write-Host "Added to Startup folder (will run at login)"

# Create shortcuts BEFORE starting service
Write-Host "Creating shortcuts..."

# Find icon file
$iconPath = Join-Path (Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)) "inc\RFMediaLink.ico"
if (-not (Test-Path $iconPath)) {
    $iconPath = ""
    Write-Warning "Icon file not found at $iconPath"
}

# Desktop shortcut
$configuratorPath = "$InstallDir\RFMediaLink.exe"
$desktopShortcut = Join-Path $DesktopPath "RF Media Link Configure.lnk"
Create-Shortcut -TargetPath $configuratorPath -ShortcutPath $desktopShortcut -Description "RF Media Link Configurator" -WorkingDirectory $InstallDir -IconLocation $iconPath

# Create Start Menu folder
if (-not (Test-Path $StartMenuFolder)) {
    New-Item -ItemType Directory -Path $StartMenuFolder -Force | Out-Null
}

# Start Menu shortcut - Configurator
$startMenuShortcut = Join-Path $StartMenuFolder "RF Media Link Configure.lnk"
Create-Shortcut -TargetPath $configuratorPath -ShortcutPath $startMenuShortcut -Description "RF Media Link Configurator" -WorkingDirectory $InstallDir -IconLocation $iconPath

# Start Menu - Service Management shortcuts
$startServiceShortcut = Join-Path $StartMenuFolder "Start Service.lnk"
Create-Shortcut -TargetPath $exePath -ShortcutPath $startServiceShortcut -Description "Start RF Media Link Service" -IconLocation $iconPath -WorkingDirectory $InstallDir

$stopServiceShortcut = Join-Path $StartMenuFolder "Stop Service.lnk"
Create-Shortcut -TargetPath "taskkill.exe" -ShortcutPath $stopServiceShortcut -Description "Stop RF Media Link Service" -IconLocation $iconPath -Arguments "/IM RFMediaLinkService.exe /F"

$restartServiceShortcut = Join-Path $StartMenuFolder "Restart Service.lnk"
Create-Shortcut -TargetPath "powershell.exe" -ShortcutPath $restartServiceShortcut -Description "Restart RF Media Link Service" -IconLocation $iconPath -Arguments "-ExecutionPolicy Bypass -File `"$InstallDir\restart-service.ps1`""

# Start Menu - Configuration file shortcuts
$catalogShortcut = Join-Path $StartMenuFolder "Edit Catalog.lnk"
Create-Shortcut -TargetPath "$InstallDir\open-catalog.bat" -ShortcutPath $catalogShortcut -Description "Edit catalog.json" -WorkingDirectory $InstallDir -IconLocation $iconPath

$emulatorsShortcut = Join-Path $StartMenuFolder "Edit Emulators.lnk"
Create-Shortcut -TargetPath "$InstallDir\open-emulators.bat" -ShortcutPath $emulatorsShortcut -Description "Edit emulators.json" -WorkingDirectory $InstallDir -IconLocation $iconPath

# Start Menu - Uninstall shortcut
$uninstallShortcut = Join-Path $StartMenuFolder "Uninstall.lnk"
Create-Shortcut -TargetPath "powershell.exe" -ShortcutPath $uninstallShortcut -Description "Uninstall RF Media Link" -IconLocation $iconPath -Arguments "-ExecutionPolicy Bypass -File `"$InstallDir\uninstall.ps1`""

# Start the service now
Write-Host "Starting RF Media Link service..."
Start-Process -FilePath $exePath -WorkingDirectory $InstallDir -WindowStyle Minimized

Start-Sleep -Seconds 2

$process = Get-Process -Name "RFMediaLinkService" -ErrorAction SilentlyContinue
if ($process) {
    Write-Host "Service is running (PID: $($process.Id))"
    
    # Verify the binary timestamp
    $exePath = "$InstallDir\RFMediaLinkService.exe"
    $fileInfo = Get-Item $exePath
    Write-Host "Service binary last modified: $($fileInfo.LastWriteTime)"
} else {
    Write-Warning "Service may not have started. Try running manually."
}

Write-Host ""
Write-Host "============================================"
Write-Host "  RF Media Link Installation Complete!"
Write-Host "============================================"
Write-Host "Installation directory: $InstallDir"
Write-Host ""
Write-Host "Configuration files:"
Write-Host "  - $InstallDir\config.json"
Write-Host "  - $InstallDir\catalog.json"
Write-Host "  - $InstallDir\emulators.json"
Write-Host ""
Write-Host "Service runs at login from: $StartupFolder\RF Media Link.lnk"
Write-Host ""
Write-Host "Shortcuts created:"
Write-Host "  - Desktop: RF Media Link Configure.lnk"
Write-Host "  - Start Menu: Programs\RF Media Link\RF Media Link Configure.lnk"
Write-Host "  - Start Menu: Programs\RF Media Link\Start Service.lnk"
Write-Host "  - Start Menu: Programs\RF Media Link\Stop Service.lnk"
Write-Host "  - Start Menu: Programs\RF Media Link\Restart Service.lnk"
Write-Host "  - Start Menu: Programs\RF Media Link\Edit Catalog.lnk"
Write-Host "  - Start Menu: Programs\RF Media Link\Edit Emulators.lnk"
Write-Host "  - Start Menu: Programs\RF Media Link\Uninstall.lnk"
Write-Host ""
Write-Host "To uninstall, run the Uninstall shortcut from Start Menu or:"
Write-Host "  powershell -ExecutionPolicy Bypass -File `"$InstallDir\uninstall.ps1`""
