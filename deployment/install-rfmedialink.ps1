# RF Media Link Service Installer
# Run as Administrator

param(
    [switch]$Uninstall
)

$InstallDir = "$env:LOCALAPPDATA\RFMediaLink"
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
        [string]$IconLocation = ""
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
    $desktopShortcut = Join-Path $DesktopPath "RF Media Link.lnk"
    if (Test-Path $desktopShortcut) {
        Remove-Item -Path $desktopShortcut -Force -ErrorAction SilentlyContinue
        Write-Host "Desktop shortcut removed"
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

# Copy from actual build folders - no guessing
$parentDir = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)

# Copy Service files from service publish folder
$servicePublishDir = Join-Path $parentDir "RFMediaLinkService\bin\Release\net8.0-windows\publish"
if (Test-Path $servicePublishDir) {
    Write-Host "Copying service files from $servicePublishDir..."
    Get-ChildItem -Path $servicePublishDir -Recurse | Copy-Item -Destination $InstallDir -Recurse -Force
    Write-Host "Service files copied"
} else {
    Write-Error "Service build folder not found: $servicePublishDir"
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
foreach ($file in @("config.json", "catalog.json", "emulators.json")) {
    $sourcePath = Join-Path $parentDir $file
    $destPath = "$InstallDir\$file"
    if (Test-Path $sourcePath) {
        if (-not (Test-Path $destPath)) {
            Copy-Item -Path $sourcePath -Destination $destPath
            Write-Host "Copied $file"
        } else {
            Write-Host "Skipped $file (already exists)"
        }
    }
}

Write-Host "Files copied to $InstallDir"

# Create Windows Service
Write-Host "Creating Windows Service..."
$exePath = "$InstallDir\RFMediaLinkService.exe"

# Check if service already exists
$existingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($existingService) {
    Write-Host "Stopping existing service..."
    Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
    
    # Force kill any remaining processes
    Get-Process -Name "RFMediaLinkService" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 1
    
    Write-Host "Deleting old service..."
    sc.exe delete $ServiceName | Out-Null
    Start-Sleep -Seconds 2
}

# Create service with new display name
sc.exe create $ServiceName binPath= "`"$exePath`"" start= auto DisplayName= "RF Media Link" | Out-Null
if ($LASTEXITCODE -eq 0) {
    Write-Host "Service created successfully"
} else {
    Write-Error "Failed to create service (error code: $LASTEXITCODE)"
    exit 1
}

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
$desktopShortcut = Join-Path $DesktopPath "RF Media Link.lnk"
Create-Shortcut -TargetPath $configuratorPath -ShortcutPath $desktopShortcut -Description "RF Media Link Configurator" -WorkingDirectory $InstallDir -IconLocation $iconPath

# Create Start Menu folder
if (-not (Test-Path $StartMenuFolder)) {
    New-Item -ItemType Directory -Path $StartMenuFolder -Force | Out-Null
}

# Start Menu shortcut
$startMenuShortcut = Join-Path $StartMenuFolder "RF Media Link.lnk"
Create-Shortcut -TargetPath $configuratorPath -ShortcutPath $startMenuShortcut -Description "RF Media Link Configurator" -WorkingDirectory $InstallDir -IconLocation $iconPath

# Start Menu - Service Management shortcut
$serviceManagementShortcut = Join-Path $StartMenuFolder "Manage Service.lnk"
Create-Shortcut -TargetPath "services.msc" -ShortcutPath $serviceManagementShortcut -Description "Manage RF Media Link Service" -IconLocation $iconPath

# Start service AFTER shortcuts are created
Write-Host "Starting service..."
Start-Service -Name $ServiceName -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2

$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($service.Status -eq "Running") {
    Write-Host "Service is running"
} else {
    Write-Warning "Service status: $($service.Status)"
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
Write-Host "Shortcuts created:"
Write-Host "  - Desktop: RF Media Link.lnk"
Write-Host "  - Start Menu: Programs\RF Media Link\RF Media Link.lnk"
Write-Host "  - Start Menu: Programs\RF Media Link\Manage Service.lnk"
Write-Host ""
Write-Host "To uninstall, run:"
Write-Host "  powershell -ExecutionPolicy Bypass -File install-rfmedialink.ps1 -Uninstall"
