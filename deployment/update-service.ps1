# Update RF Media Link Service
# Run as Administrator

param(
    [switch]$ServiceOnly
)

$serviceName = "RF Media Link"
$installDir = "$env:ProgramData\RFMediaLink"
$parentDir = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)

# Check for admin
$currentUser = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = New-Object Security.Principal.WindowsPrincipal($currentUser)
$isAdmin = $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Write-Host "This script requires Administrator privileges." -ForegroundColor Red
    Write-Host "Relaunching as Administrator..." -ForegroundColor Yellow
    Start-Process powershell.exe -Verb RunAs -ArgumentList "-ExecutionPolicy Bypass -File `"$PSCommandPath`""
    exit
}

Write-Host "Stopping RF Media Link Service..." -ForegroundColor Yellow
# Stop old Windows Service if it exists
Stop-Service $serviceName -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 1

# Force kill any remaining processes
Get-Process -Name "RFMediaLinkService" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Get-Process -Name "RFMediaLink" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 1

Write-Host "Updating service binaries..." -ForegroundColor Yellow
$servicePublishDir = Join-Path $parentDir "RFMediaLinkService\bin\Release\net8.0-windows\publish"
if (Test-Path $servicePublishDir) {
    $filesCopied = 0
    Get-ChildItem -Path $servicePublishDir -Recurse | ForEach-Object {
        $targetPath = $_.FullName.Replace($servicePublishDir, $installDir)
        if ($_.PSIsContainer) {
            if (-not (Test-Path $targetPath)) {
                New-Item -ItemType Directory -Path $targetPath -Force | Out-Null
            }
        } else {
            Copy-Item -Path $_.FullName -Destination $targetPath -Force
            $filesCopied++
        }
    }
    Write-Host "Service files copied: $filesCopied files" -ForegroundColor Green
} else {
    Write-Error "Service build folder not found: $servicePublishDir"
    Write-Host "Make sure you've run: dotnet publish RFMediaLinkService\RFMediaLinkService.csproj -c Release"
    pause
    exit 1
}

if (-not $ServiceOnly) {
    Write-Host "Updating configurator..." -ForegroundColor Yellow
    $configurePublishDir = Join-Path $parentDir "RFMediaLink\bin\Release\net8.0-windows\publish"
    if (Test-Path $configurePublishDir) {
        Get-ChildItem -Path $configurePublishDir -Filter "RFMediaLink.*" | Copy-Item -Destination $installDir -Force
        Write-Host "Configurator files copied" -ForegroundColor Green
    }
}

Write-Host "Starting service..." -ForegroundColor Yellow
$exePath = "$installDir\RFMediaLinkService.exe"
Start-Process -FilePath $exePath -WorkingDirectory $installDir -WindowStyle Minimized
Start-Sleep -Seconds 2

$process = Get-Process -Name "RFMediaLinkService" -ErrorAction SilentlyContinue
if ($process) {
    Write-Host "Service is running (PID: $($process.Id))" -ForegroundColor Green
    
    # Verify the binary timestamp
    $fileInfo = Get-Item $exePath
    Write-Host "Service binary last modified: $($fileInfo.LastWriteTime)" -ForegroundColor Cyan
} else {
    Write-Warning "Service process may not have started. Try running manually."
}

Write-Host ""
Write-Host "Update complete!" -ForegroundColor Green
Write-Host "Use -ServiceOnly flag to update only the service (faster)"
pause
