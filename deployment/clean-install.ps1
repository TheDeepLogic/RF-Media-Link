# Clean uninstall and reinstall RF Media Link
# Run as Administrator

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  RF Media Link Clean Install" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$uninstallScript = Join-Path $scriptDir "uninstall.ps1"
$installScript = Join-Path $scriptDir "install-rfmedialink.ps1"

# Step 1: Uninstall
Write-Host "[1/2] Uninstalling existing installation..." -ForegroundColor Yellow
Write-Host ""
if (Test-Path $uninstallScript) {
    & $uninstallScript
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Uninstall failed or was cancelled" -ForegroundColor Red
        pause
        exit 1
    }
} else {
    Write-Host "Uninstall script not found, skipping..." -ForegroundColor Yellow
}

Write-Host ""
Write-Host ""

# Step 2: Install
Write-Host "[2/2] Installing RF Media Link..." -ForegroundColor Yellow
Write-Host ""
if (Test-Path $installScript) {
    & $installScript
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Installation failed" -ForegroundColor Red
        pause
        exit 1
    }
} else {
    Write-Host "Install script not found!" -ForegroundColor Red
    pause
    exit 1
}

Write-Host ""
Write-Host "============================================" -ForegroundColor Green
Write-Host "  Clean Install Complete!" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Green
