# Start RF Media Link Service
# Runs the scheduled task with elevated privileges

$TaskName = "RF Media Link Service"
$InstallDir = "$env:ProgramData\RFMediaLink"
$ExePath = "$InstallDir\RFMediaLinkService.exe"

Write-Host "Starting RF Media Link Service..."

# Check if already running
$running = Get-Process -Name "RFMediaLinkService" -ErrorAction SilentlyContinue
if ($running) {
    Write-Host "Service is already running (PID: $($running.Id))" -ForegroundColor Green
    pause
    exit 0
}

# Try to start via scheduled task first
$task = Get-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue
if ($task) {
    Start-ScheduledTask -TaskName $TaskName
    Start-Sleep -Seconds 2
    
    $running = Get-Process -Name "RFMediaLinkService" -ErrorAction SilentlyContinue
    if ($running) {
        Write-Host "Service started successfully (PID: $($running.Id))" -ForegroundColor Green
    } else {
        Write-Host "Failed to start service via scheduled task" -ForegroundColor Red
    }
} else {
    # Fallback: start directly (won't have elevated privileges)
    Write-Host "Scheduled task not found, starting directly..." -ForegroundColor Yellow
    Start-Process -FilePath $ExePath -WorkingDirectory $InstallDir -WindowStyle Minimized
    Start-Sleep -Seconds 2
    
    $running = Get-Process -Name "RFMediaLinkService" -ErrorAction SilentlyContinue
    if ($running) {
        Write-Host "Service started (PID: $($running.Id))" -ForegroundColor Green
        Write-Host "WARNING: Running without elevated privileges - window focus may not work" -ForegroundColor Yellow
    } else {
        Write-Host "Failed to start service" -ForegroundColor Red
    }
}

pause
