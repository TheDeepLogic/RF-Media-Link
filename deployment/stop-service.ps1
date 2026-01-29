# Stop RF Media Link Service
# Requires Administrator privileges

$ServiceName = "RF Media Link"

# Check if running as admin
$currentUser = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = New-Object Security.Principal.WindowsPrincipal($currentUser)
$isAdmin = $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    # Relaunch as administrator
    Start-Process powershell.exe -Verb RunAs -ArgumentList "-ExecutionPolicy Bypass -File `"$PSCommandPath`""
    exit
}

Write-Host "Stopping RF Media Link Service..."
$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue

if (-not $service) {
    Write-Host "Error: Service not found!" -ForegroundColor Red
    pause
    exit 1
}

if ($service.Status -eq "Stopped") {
    Write-Host "Service is already stopped." -ForegroundColor Yellow
} else {
    Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
    $service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
    
    if ($service.Status -eq "Stopped") {
        Write-Host "Service stopped successfully!" -ForegroundColor Green
    } else {
        Write-Host "Failed to stop service. Status: $($service.Status)" -ForegroundColor Red
    }
}

pause
