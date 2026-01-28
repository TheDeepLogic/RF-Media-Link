# Clean uninstall and reinstall RF Media Link Service
# Run as Administrator

$serviceName = "RF Media Link"
$installDir = "$env:LOCALAPPDATA\RFMediaLink"
$sourceDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$buildDir = Join-Path $sourceDir "build"

Write-Host "=== Clean Installation of RF Media Link Service ===" -ForegroundColor Cyan

# Step 1: Stop the service if running
Write-Host "`n[1/5] Stopping service..." -ForegroundColor Yellow
$svc = Get-Service $serviceName -ErrorAction SilentlyContinue
if ($svc) {
    Stop-Service $serviceName -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
}

# Step 2: Uninstall the service
Write-Host "[2/5] Uninstalling service..." -ForegroundColor Yellow
if ($svc) {
    sc.exe delete $serviceName | Out-Null
    Start-Sleep -Seconds 2
}

# Step 3: Remove the old installation directory
Write-Host "[3/5] Removing old installation directory..." -ForegroundColor Yellow
if (Test-Path $installDir) {
    Remove-Item -Path $installDir -Recurse -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 1
}

# Step 4: Create directory and copy new binaries
Write-Host "[4/5] Installing new binaries..." -ForegroundColor Yellow
New-Item -ItemType Directory -Path $installDir -Force | Out-Null
Copy-Item -Path "$buildDir\*" -Destination $installDir -Recurse -Force
Write-Host "    Files copied to $installDir" -ForegroundColor Green

# Step 5: Recreate service
Write-Host "[5/5] Creating service..." -ForegroundColor Yellow
$exePath = Join-Path $installDir "RFMediaLinkService.exe"
sc.exe create $serviceName binPath= $exePath start= auto | Out-Null
Start-Service $serviceName

Write-Host "`n=== Installation Complete ===" -ForegroundColor Green
Write-Host "Service Status: $(Get-Service $serviceName | Select-Object -ExpandProperty Status)" -ForegroundColor Green
Write-Host "`nTry scanning now. Check Event Viewer for logs." -ForegroundColor Cyan
