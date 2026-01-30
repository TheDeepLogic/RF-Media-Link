# Update RF Media Link Service
# Run as Administrator

param(
    [switch]$ServiceOnly,
    [switch]$SkipBuild
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

# Build first unless SkipBuild is specified
if (-not $SkipBuild) {
    Write-Host "Building latest version..." -ForegroundColor Yellow
    
    # Build service
    $serviceProject = Join-Path $parentDir "RFMediaLinkService\RFMediaLinkService.csproj"
    if (Test-Path $serviceProject) {
        Write-Host "Building RFMediaLinkService..." -ForegroundColor Cyan
        Push-Location (Split-Path $serviceProject)
        dotnet publish -c Release
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Service build failed!"
            Pop-Location
            pause
            exit 1
        }
        Pop-Location
        Write-Host "Service build complete" -ForegroundColor Green
    } else {
        Write-Error "Service project not found: $serviceProject"
        pause
        exit 1
    }
    
    # Build configurator if not ServiceOnly
    if (-not $ServiceOnly) {
        $configProject = Join-Path $parentDir "RFMediaLink\RFMediaLink.csproj"
        if (Test-Path $configProject) {
            Write-Host "Building RFMediaLink configurator..." -ForegroundColor Cyan
            Push-Location (Split-Path $configProject)
            dotnet publish -c Release
            if ($LASTEXITCODE -ne 0) {
                Write-Error "Configurator build failed!"
                Pop-Location
                pause
                exit 1
            }
            Pop-Location
            Write-Host "Configurator build complete" -ForegroundColor Green
        }
    }
    Write-Host ""
}

Write-Host "Stopping RF Media Link Service..." -ForegroundColor Yellow
# Stop old Windows Service if it exists
Stop-Service $serviceName -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2

# Force kill any remaining processes
Get-Process -Name "RFMediaLinkService" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Get-Process -Name "RFMediaLink" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2

# Wait for files to be released
$maxWait = 10
$waited = 0
while ($waited -lt $maxWait) {
    $serviceExe = Join-Path $installDir "RFMediaLinkService.exe"
    if (Test-Path $serviceExe) {
        try {
            $stream = [System.IO.File]::Open($serviceExe, 'Open', 'ReadWrite', 'None')
            $stream.Close()
            break
        } catch {
            Start-Sleep -Seconds 1
            $waited++
        }
    } else {
        break
    }
}

Write-Host "Updating service binaries..." -ForegroundColor Yellow
$servicePublishDir = Join-Path $parentDir "RFMediaLinkService\bin\Release\net8.0-windows\publish"
if (Test-Path $servicePublishDir) {
    $filesCopied = 0
    $errors = 0
    Get-ChildItem -Path $servicePublishDir -Recurse | ForEach-Object {
        $targetPath = $_.FullName.Replace($servicePublishDir, $installDir)
        if ($_.PSIsContainer) {
            if (-not (Test-Path $targetPath)) {
                New-Item -ItemType Directory -Path $targetPath -Force | Out-Null
            }
        } else {
            try {
                Copy-Item -Path $_.FullName -Destination $targetPath -Force -ErrorAction Stop
                $filesCopied++
            } catch {
                Write-Warning "Failed to copy $($_.Name): $($_.Exception.Message)"
                $errors++
            }
        }
    }
    if ($errors -gt 0) {
        Write-Warning "Failed to copy $errors file(s). Some files may be in use."
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
Write-Host "Usage: update-service.ps1 [-ServiceOnly] [-SkipBuild]" -ForegroundColor Cyan
Write-Host "  -ServiceOnly: Update only the service (skip configurator)"
Write-Host "  -SkipBuild:   Skip rebuilding (use existing binaries)"
pause
