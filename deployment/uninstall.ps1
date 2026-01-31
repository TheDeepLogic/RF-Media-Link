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

# Kill all running processes with multiple retries
Write-Host "Stopping all RF Media Link processes..."
for ($attempt = 1; $attempt -le 5; $attempt++) {
    $allProcs = @()
    $allProcs += Get-Process -Name "RFMediaLinkService" -ErrorAction SilentlyContinue
    $allProcs += Get-Process -Name "RFMediaLink" -ErrorAction SilentlyContinue
    
    if ($allProcs.Count -eq 0) {
        Write-Host "  All processes stopped" -ForegroundColor Green
        break
    }
    
    $procCount = $allProcs.Count
    Write-Host "  Attempt ${attempt}: Found $procCount running process(es), terminating..."
    foreach ($proc in $allProcs) {
        try {
            $proc | Stop-Process -Force -ErrorAction Stop
            Write-Host "    Killed: $($proc.ProcessName) (PID: $($proc.Id))" -ForegroundColor Yellow
        }
        catch {
            Write-Host "    Failed to kill: $($proc.ProcessName) (PID: $($proc.Id))" -ForegroundColor Red
        }
    }
    
    Start-Sleep -Seconds 3
    
    # Final attempt: use taskkill
    if ($attempt -eq 5) {
        Write-Host "  Using taskkill as last resort..."
        taskkill /F /IM RFMediaLinkService.exe 2>$null
        taskkill /F /IM RFMediaLink.exe 2>$null
        Start-Sleep -Seconds 2
    }
}

# Final verification
$remaining = @()
$remaining += Get-Process -Name "RFMediaLinkService" -ErrorAction SilentlyContinue
$remaining += Get-Process -Name "RFMediaLink" -ErrorAction SilentlyContinue
if ($remaining.Count -gt 0) {
    Write-Host "  ERROR: Could not stop all processes. Please close them manually and retry." -ForegroundColor Red
    pause
    exit 1
}

# Remove scheduled task
$taskName = "RF Media Link Service"
$task = Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue
if ($task) {
    Write-Host "Removing scheduled task..."
    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false
    Write-Host "Scheduled task removed" -ForegroundColor Green
}

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

# Remove old Startup shortcut if it exists
$StartupFolder = [Environment]::GetFolderPath("Startup")
$startupShortcut = Join-Path $StartupFolder "RF Media Link.lnk"
if (Test-Path $startupShortcut) {
    Remove-Item -Path $startupShortcut -Force -ErrorAction SilentlyContinue
    Write-Host "Old startup shortcut removed" -ForegroundColor Green
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

exit 0
