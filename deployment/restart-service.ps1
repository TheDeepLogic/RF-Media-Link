# Restart RF Media Link Service
# Can run without admin privileges

$InstallDir = "$env:ProgramData\RFMediaLink"

Write-Host "Restarting RF Media Link Service..." -ForegroundColor Yellow

# Stop the service
Write-Host "Stopping service..." -ForegroundColor Yellow
Get-Process -Name "RFMediaLinkService" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2

# Start the service
Write-Host "Starting service..." -ForegroundColor Yellow
$exePath = "$InstallDir\RFMediaLinkService.exe"
if (Test-Path $exePath) {
    Start-Process -FilePath $exePath -WorkingDirectory $InstallDir -WindowStyle Minimized
    Start-Sleep -Seconds 2
    
    $process = Get-Process -Name "RFMediaLinkService" -ErrorAction SilentlyContinue
    if ($process) {
        Write-Host "Service restarted successfully (PID: $($process.Id))" -ForegroundColor Green
    } else {
        Write-Warning "Service may not have started. Check for errors."
    }
} else {
    Write-Error "Service executable not found: $exePath"
}

Write-Host ""
pause
