# Convert RF Media Link from Windows Service to Startup Application
# Run as Administrator

$ServiceName = "RF Media Link"
$InstallDir = "$env:ProgramData\RFMediaLink"
$StartupFolder = [Environment]::GetFolderPath("Startup")

function Test-Administrator {
    $currentUser = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($currentUser)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

if (-not (Test-Administrator)) {
    Write-Error "This script must be run as Administrator!"
    pause
    exit 1
}

Write-Host "============================================"
Write-Host "  Convert to Startup Application"
Write-Host "============================================"
Write-Host ""
Write-Host "This will:"
Write-Host "  1. Remove the Windows Service"
Write-Host "  2. Add RFMediaLinkService.exe to your Startup folder"
Write-Host "  3. The service will run in your session and can launch GUIs"
Write-Host ""
$confirm = Read-Host "Continue? (Y/N)"
if ($confirm -ne "Y" -and $confirm -ne "y") {
    Write-Host "Cancelled"
    pause
    exit 0
}

# Stop and remove Windows Service
$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($service) {
    Write-Host ""
    Write-Host "Stopping and removing Windows Service..."
    Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
    
    # Kill any remaining processes
    Get-Process -Name "RFMediaLinkService" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 1
    
    sc.exe delete $ServiceName | Out-Null
    Write-Host "Windows Service removed"
} else {
    Write-Host "No Windows Service found to remove"
}

# Create startup shortcut
Write-Host ""
Write-Host "Creating startup shortcut..."
$exePath = "$InstallDir\RFMediaLinkService.exe"
$shortcutPath = Join-Path $StartupFolder "RF Media Link.lnk"

$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = $exePath
$shortcut.WorkingDirectory = $InstallDir
$shortcut.Description = "RF Media Link RFID Service"
$shortcut.WindowStyle = 7  # Minimized
$shortcut.Save()

Write-Host "Startup shortcut created: $shortcutPath"
Write-Host ""
Write-Host "Starting the service..."
Start-Process -FilePath $exePath -WorkingDirectory $InstallDir -WindowStyle Minimized

Start-Sleep -Seconds 2
$process = Get-Process -Name "RFMediaLinkService" -ErrorAction SilentlyContinue
if ($process) {
    Write-Host "Service is running (PID: $($process.Id))"
} else {
    Write-Warning "Service may not have started. Try running manually or check for errors."
}

Write-Host ""
Write-Host "============================================"
Write-Host "  Conversion Complete!"
Write-Host "============================================"
Write-Host ""
Write-Host "The service will now:"
Write-Host "  - Start automatically when you log in"
Write-Host "  - Run in your user session (can launch GUIs)"
Write-Host "  - Run minimized in the background"
Write-Host ""
Write-Host "Startup shortcut location:"
Write-Host "  $shortcutPath"
Write-Host ""
Write-Host "To disable auto-start:"
Write-Host "  - Open Task Manager > Startup tab"
Write-Host "  - Disable 'RF Media Link'"
Write-Host ""
Write-Host "Or delete the shortcut from:"
Write-Host "  $StartupFolder"
Write-Host ""
pause
