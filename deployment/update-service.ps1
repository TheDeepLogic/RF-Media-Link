# Update RetroNFC Service
# Run as Administrator

$serviceName = "RetroNFC"
$installDir = "$env:LOCALAPPDATA\RetroNFC"
$sourceDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$buildDir = Join-Path $sourceDir "build"

Write-Host "Stopping service..." -ForegroundColor Yellow
Stop-Service $serviceName -Force

Write-Host "Waiting for service to stop..." -ForegroundColor Yellow
Start-Sleep -Seconds 3

Write-Host "Updating binaries..." -ForegroundColor Yellow
Copy-Item -Path "$buildDir\*" -Destination $installDir -Recurse -Force

Write-Host "Updating Configuration Tool..." -ForegroundColor Yellow
$configureDir = Join-Path (Split-Path -Parent $sourceDir) "RetroNFCConfigure\bin\Release\publish"
if (Test-Path $configureDir) {
    Copy-Item -Path "$configureDir\*" -Destination $installDir -Force -ErrorAction SilentlyContinue
}

Write-Host "Starting service..." -ForegroundColor Yellow
Start-Service $serviceName

Write-Host "Service updated successfully!" -ForegroundColor Green
