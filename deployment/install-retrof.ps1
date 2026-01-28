# RetroNFC Service Installer
# Run as Administrator

param(
    [switch]$Uninstall
)

$InstallDir = "$env:LOCALAPPDATA\RetroNFC"
$ServiceName = "RetroNFC"

function Test-Administrator {
    $currentUser = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($currentUser)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

if (-not (Test-Administrator)) {
    Write-Error "This script must be run as Administrator!"
    exit 1
}

if ($Uninstall) {
    Write-Host "Uninstalling RetroNFC Service..."
    
    # Stop service
    $service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
    if ($service) {
        Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 2
        sc.exe delete $ServiceName
        Write-Host "Service uninstalled"
    }
    
    # Remove files
    if (Test-Path $InstallDir) {
        Remove-Item -Path $InstallDir -Recurse -Force
        Write-Host "Installation directory removed"
    }
    
    Write-Host "Uninstall complete"
    exit 0
}

# Install
Write-Host "Installing RetroNFC Service to $InstallDir..."

# Create installation directory
if (-not (Test-Path $InstallDir)) {
    New-Item -ItemType Directory -Path $InstallDir -Force | Out-Null
}

# Copy service files
$sourceDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Write-Host "Copying files from $sourceDir..."

# Copy all DLLs, EXE, and runtime files from build folder
$buildDir = Join-Path $sourceDir "build"
if (Test-Path $buildDir) {
    Get-ChildItem -Path "$buildDir\*" -Include "*.dll","*.exe","*.json" | Copy-Item -Destination $InstallDir -Force
    if (Test-Path "$buildDir\runtimes") {
        Copy-Item -Path "$buildDir\runtimes" -Destination "$InstallDir\runtimes" -Recurse -Force
    }
} else {
    # Fallback: copy from deployment folder directly
    Get-ChildItem -Path "$sourceDir\*" -Include "*.dll","*.exe" | Copy-Item -Destination $InstallDir -Force
    if (Test-Path "$sourceDir\runtimes") {
        Copy-Item -Path "$sourceDir\runtimes" -Destination "$InstallDir\runtimes" -Recurse -Force
    }
}

# Copy Console Configure tool (RetroNFCConfigure)
# This is built separately and published to its own directory
$configureDir = Join-Path (Split-Path -Parent $sourceDir) "RetroNFCConfigure\bin\Release\publish"
if (Test-Path $configureDir) {
    Get-ChildItem -Path "$configureDir\*" | Copy-Item -Destination $InstallDir -Force -ErrorAction SilentlyContinue
    Write-Host "Copied RetroNFC Configuration Tool"
} else {
    Write-Warning "RetroNFC Configuration Tool not found at $configureDir"
}

# Copy JSON config files (only if they don't exist)
$parentDir = Split-Path -Parent $sourceDir
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
    } else {
        Write-Warning "Config file not found: $sourcePath"
    }
}

Write-Host "Files copied to $InstallDir"

# Create Windows Service
Write-Host "Creating Windows Service..."
$exePath = "$InstallDir\RetroNFCService.exe"

# Check if service already exists
$existingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($existingService) {
    Write-Host "Service already exists, stopping it..."
    Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 1
    sc.exe delete $ServiceName | Out-Null
    Start-Sleep -Seconds 1
}

# Create service
sc.exe create $ServiceName binPath= "`"$exePath`"" start= auto | Out-Null
if ($LASTEXITCODE -eq 0) {
    Write-Host "Service created successfully"
} else {
    Write-Error "Failed to create service (error code: $LASTEXITCODE)"
    exit 1
}

# Start service
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
Write-Host "Installation complete!"
Write-Host "Service installed to: $InstallDir"
Write-Host "Config files: $InstallDir\config.json, catalog.json, emulators.json"
Write-Host ""
Write-Host "To configure tags and emulators, run: python $InstallDir\configure.py"
Write-Host ""
Write-Host "To uninstall, run: powershell -ExecutionPolicy Bypass -File install-retrof.ps1 -Uninstall"
