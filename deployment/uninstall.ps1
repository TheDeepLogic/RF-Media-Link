# RF Media Link Service Uninstaller
# Run as Administrator

$InstallDir = "$env:ProgramData\RFMediaLink"
$ServiceName = "RF Media Link"
$DesktopPath = [Environment]::GetFolderPath("Desktop")
$StartMenuPath = [Environment]::GetFolderPath("StartMenu")
$StartMenuFolder = Join-Path $StartMenuPath "Programs\RF Media Link"

# Check if running as admin
$currentUser = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = New-Object Security.Principal.WindowsPrincipal($currentUser)
$isAdmin = $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Write-Host "This script requires Administrator privileges." -ForegroundColor Red
    Write-Host "Relaunching as Administrator..." -ForegroundColor Yellow
    Start-Process powershell.exe -Verb RunAs -ArgumentList "-ExecutionPolicy Bypass -File `"$PSCommandPath`""
    exit
}

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  RF Media Link Service Uninstaller" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

$confirm = Read-Host "Are you sure you want to uninstall RF Media Link? (y/n)"
if ($confirm.ToLower() -ne "y") {
    Write-Host "Uninstall cancelled." -ForegroundColor Yellow
    pause
    exit 0
}

Write-Host ""
Write-Host "Uninstalling RF Media Link Service..."

# Kill any running RFMediaLink.exe configure processes
Get-Process -Name "RFMediaLink" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 1

# Kill running service process
Write-Host "Stopping service process..."
Get-Process -Name "RFMediaLinkService" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 1

# Remove old Windows Service if it exists
$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($service) {
    Write-Host "Removing old Windows Service..."
    Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 1
    sc.exe delete $ServiceName | Out-Null
    Start-Sleep -Seconds 1
    Write-Host "Old service removed" -ForegroundColor Green
}

# Remove Startup shortcut
$StartupFolder = [Environment]::GetFolderPath("Startup")
$startupShortcut = Join-Path $StartupFolder "RF Media Link.lnk"
if (Test-Path $startupShortcut) {
    Remove-Item -Path $startupShortcut -Force -ErrorAction SilentlyContinue
    Write-Host "Startup shortcut removed" -ForegroundColor Green
}

# Remove desktop shortcut
$desktopShortcut = Join-Path $DesktopPath "RF Media Link Configure.lnk"
if (Test-Path $desktopShortcut) {
    Remove-Item -Path $desktopShortcut -Force -ErrorAction SilentlyContinue
    Write-Host "Desktop shortcut removed" -ForegroundColor Green
}

# Remove Start Menu folder
if (Test-Path $StartMenuFolder) {
    Remove-Item -Path $StartMenuFolder -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "Start Menu folder removed" -ForegroundColor Green
}

# Remove files with retry
if (Test-Path $InstallDir) {
    Write-Host "Removing installation directory..."
    Start-Sleep -Seconds 1
    Remove-Item -Path $InstallDir -Recurse -Force -ErrorAction SilentlyContinue
    if (Test-Path $InstallDir) {
        Write-Host "Some files could not be removed. They may be in use." -ForegroundColor Yellow
    } else {
        Write-Host "Installation directory removed" -ForegroundColor Green
    }
}

Write-Host ""
Write-Host "============================================" -ForegroundColor Green
Write-Host "  Uninstall Complete!" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Green

pause
