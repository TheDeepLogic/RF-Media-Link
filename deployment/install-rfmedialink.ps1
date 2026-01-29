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

# CRITICAL: Stop all running processes BEFORE copying files
Write-Host "Stopping any running instances..."
for ($i = 0; $i -lt 3; $i++) {
    $serviceProcs = Get-Process -Name "RFMediaLinkService" -ErrorAction SilentlyContinue
    $configProcs = Get-Process -Name "RFMediaLink" -ErrorAction SilentlyContinue
    
    if ($serviceProcs) {
        Write-Host "  Stopping RFMediaLinkService processes..."
        $serviceProcs | Stop-Process -Force -ErrorAction SilentlyContinue
    }
    if ($configProcs) {
        Write-Host "  Stopping RFMediaLink configurator processes..."
        $configProcs | Stop-Process -Force -ErrorAction SilentlyContinue
    }
    
    Start-Sleep -Seconds 2
    
    # Verify they're really gone
    $stillRunning = @()
    $stillRunning += Get-Process -Name "RFMediaLinkService" -ErrorAction SilentlyContinue
    $stillRunning += Get-Process -Name "RFMediaLink" -ErrorAction SilentlyContinue
    
    if ($stillRunning.Count -eq 0) {
        Write-Host "  All processes stopped" -ForegroundColor Green
        break
    }
    
    if ($i -eq 2) {
        Write-Host "  WARNING: Some processes are still running!" -ForegroundColor Red
        Write-Host "  Installation may fail due to file locks." -ForegroundColor Red
        $continue = Read-Host "Continue anyway? (y/n)"
        if ($continue.ToLower() -ne 'y') {
            exit 1
        }
    }
}

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

# Determine if running from release package or development environment
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$parentDir = Split-Path -Parent $scriptDir

# Check if this is a release package (service/ and configurator/ folders in same directory as script)
$serviceReleaseDir = Join-Path $scriptDir "service"
$configuratorReleaseDir = Join-Path $scriptDir "configurator"

# Check if this is development environment (build folders in parent directory structure)
$serviceBuildDir = Join-Path $parentDir "RFMediaLinkService\bin\Release\net8.0-windows\publish"
$configuratorBuildDir = Join-Path $parentDir "RFMediaLink\bin\Release\net8.0-windows\publish"

# Determine source directories
if ((Test-Path $serviceReleaseDir) -and (Test-Path $configuratorReleaseDir)) {
    # Release package mode
    Write-Host "Detected release package mode"
    $servicePublishDir = $serviceReleaseDir
    $configurePublishDir = $configuratorReleaseDir
}
elseif ((Test-Path $serviceBuildDir) -and (Test-Path $configuratorBuildDir)) {
    # Development mode
    Write-Host "Detected development mode"
    $servicePublishDir = $serviceBuildDir
    $configurePublishDir = $configuratorBuildDir
}
else {
    Write-Error "Could not find service or configurator binaries. Checked:`n  - $serviceReleaseDir`n  - $configuratorReleaseDir`n  - $serviceBuildDir`n  - $configuratorBuildDir"
    pause
    exit 1
}

# Copy Service files
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

# Copy Configurator files
Write-Host "Copying configurator files from $configurePublishDir..."
Get-ChildItem -Path $configurePublishDir -Filter "RFMediaLink.*" | Copy-Item -Destination $InstallDir -Force
Write-Host "Configurator files copied"

# Copy JSON config files and other resources
# Determine source locations based on release vs dev mode
if (Test-Path (Join-Path $scriptDir "inc")) {
    # Release package - inc folder is in same directory as script
    $incDir = Join-Path $scriptDir "inc"
} else {
    # Development - inc folder is in parent directory
    $incDir = Join-Path $parentDir "inc"
}

$configFiles = @(
    @{ Name = "emulators.json"; Source = Join-Path $incDir "emulators.json" }
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
# Determine source based on release vs dev mode
if (Test-Path (Join-Path $scriptDir "scripts")) {
    # Release package - scripts are in scripts/ folder
    $scriptsSourceDir = Join-Path $scriptDir "scripts"
} else {
    # Development - scripts are in same directory as this script
    $scriptsSourceDir = $scriptDir
}

foreach ($file in @("start-service.ps1", "stop-service.ps1", "restart-service.ps1", "update-service.ps1", "uninstall.ps1", "open-catalog.bat", "open-emulators.bat")) {
    $sourcePath = Join-Path $scriptsSourceDir $file
    $destPath = "$InstallDir\$file"
    if (Test-Path $sourcePath) {
        Copy-Item -Path $sourcePath -Destination $destPath -Force
        Write-Host "Copied $file"
    } else {
        Write-Warning "Script not found: $sourcePath"
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

# Remove old startup shortcut if it exists
$StartupFolder = [Environment]::GetFolderPath("Startup")
$startupShortcutPath = Join-Path $StartupFolder "RF Media Link.lnk"
if (Test-Path $startupShortcutPath) {
    Remove-Item -Path $startupShortcutPath -Force -ErrorAction SilentlyContinue
    Write-Host "Removed old startup shortcut"
}

# Create a Scheduled Task instead of startup shortcut
# This allows running with elevated privileges without UAC prompts
Write-Host "Creating scheduled task for startup..."
$exePath = "$InstallDir\RFMediaLinkService.exe"
$taskName = "RF Media Link Service"

# Remove existing task if present
$existingTask = Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue
if ($existingTask) {
    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false
}

# Create the scheduled task action
$action = New-ScheduledTaskAction -Execute $exePath -WorkingDirectory $InstallDir

# Trigger at logon of current user
$trigger = New-ScheduledTaskTrigger -AtLogOn -User $env:USERNAME

# Settings for the task
$settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -StartWhenAvailable -DontStopOnIdleEnd -ExecutionTimeLimit (New-TimeSpan -Days 0)

# Principal with highest privileges (no UAC prompt)
$principal = New-ScheduledTaskPrincipal -UserId $env:USERNAME -LogonType Interactive -RunLevel Highest

# Register the task
Register-ScheduledTask -TaskName $taskName -Action $action -Trigger $trigger -Settings $settings -Principal $principal -Description "RF Media Link RFID Reader Service" | Out-Null

Write-Host "Scheduled task created (runs at login with elevated privileges)"

# Create shortcuts BEFORE starting service
Write-Host "Creating shortcuts..."

# Find icon file - check both release and dev locations
$iconPath = ""
if (Test-Path $incDir) {
    $testIconPath = Join-Path $incDir "RFMediaLink.ico"
    if (Test-Path $testIconPath) {
        $iconPath = $testIconPath
    }
}

if (-not $iconPath) {
    Write-Warning "Icon file not found in $incDir"
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
Create-Shortcut -TargetPath "powershell.exe" -ShortcutPath $startServiceShortcut -Description "Start RF Media Link Service" -IconLocation $iconPath -Arguments "-ExecutionPolicy Bypass -File `"$InstallDir\start-service.ps1`""

$stopServiceShortcut = Join-Path $StartMenuFolder "Stop Service.lnk"
Create-Shortcut -TargetPath "powershell.exe" -ShortcutPath $stopServiceShortcut -Description "Stop RF Media Link Service" -IconLocation $iconPath -Arguments "-ExecutionPolicy Bypass -File `"$InstallDir\stop-service.ps1`""

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
Write-Host "Service runs at login via Scheduled Task (with elevated privileges)"
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
