# Stop RF Media Link Service
# Stops the RF Media Link background process

$ProcessName = "RFMediaLinkService"

Write-Host "Stopping RF Media Link Service..." -ForegroundColor Yellow

# Check if process is running
$process = Get-Process -Name $ProcessName -ErrorAction SilentlyContinue

if (-not $process) {
    Write-Host "Service is not running." -ForegroundColor Yellow
    pause
    exit 0
}

Write-Host "Found running service (PID: $($process.Id))" -ForegroundColor Cyan

try {
    # Stop the process
    Stop-Process -Name $ProcessName -Force -ErrorAction Stop
    Start-Sleep -Seconds 2
    
    # Verify it stopped
    $stillRunning = Get-Process -Name $ProcessName -ErrorAction SilentlyContinue
    
    if ($stillRunning) {
        Write-Host "Failed to stop service. Process still running." -ForegroundColor Red
    } else {
        Write-Host "Service stopped successfully!" -ForegroundColor Green
    }
}
catch {
    Write-Host "Error stopping service: $($_.Exception.Message)" -ForegroundColor Red
}

pause
