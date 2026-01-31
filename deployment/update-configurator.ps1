# Update RF Media Link Configurator
# Run as Administrator

param(
    [switch]$SkipBuild
)

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
    Write-Host "Building configurator..." -ForegroundColor Yellow
    
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
    } else {
        Write-Error "Configurator project not found: $configProject"
        pause
        exit 1
    }
    Write-Host ""
}

# Stop any running configurator processes
Write-Host "Stopping any running configurator instances..." -ForegroundColor Yellow
Get-Process -Name "RFMediaLink" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2

# Wait for files to be released
$maxWait = 10
$waited = 0
while ($waited -lt $maxWait) {
    $configExe = Join-Path $installDir "RFMediaLink.exe"
    if (Test-Path $configExe) {
        try {
            $stream = [System.IO.File]::Open($configExe, 'Open', 'ReadWrite', 'None')
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

Write-Host "Updating configurator binaries..." -ForegroundColor Yellow
$configurePublishDir = Join-Path $parentDir "RFMediaLink\bin\Release\net8.0-windows\publish"
if (Test-Path $configurePublishDir) {
    $filesCopied = 0
    $errors = 0
    Get-ChildItem -Path $configurePublishDir -Filter "RFMediaLink.*" | ForEach-Object {
        try {
            Copy-Item -Path $_.FullName -Destination $installDir -Force -ErrorAction Stop
            $filesCopied++
        } catch {
            Write-Warning "Failed to copy $($_.Name): $($_.Exception.Message)"
            $errors++
        }
    }
    
    # Also copy VERSION file if it exists
    $versionFile = Join-Path $configurePublishDir "VERSION"
    if (Test-Path $versionFile) {
        try {
            Copy-Item -Path $versionFile -Destination $installDir -Force -ErrorAction Stop
            $filesCopied++
        } catch {
            Write-Warning "Failed to copy VERSION file"
        }
    }
    
    if ($errors -gt 0) {
        Write-Warning "Failed to copy $errors file(s). Some files may be in use."
    }
    Write-Host "Configurator files copied: $filesCopied files" -ForegroundColor Green
} else {
    Write-Error "Configurator build folder not found: $configurePublishDir"
    Write-Host "Make sure you've run: dotnet publish RFMediaLink\RFMediaLink.csproj -c Release"
    pause
    exit 1
}

# Verify the binary timestamp
$exePath = "$installDir\RFMediaLink.exe"
if (Test-Path $exePath) {
    $fileInfo = Get-Item $exePath
    Write-Host "Configurator binary last modified: $($fileInfo.LastWriteTime)" -ForegroundColor Cyan
}

Write-Host ""
Write-Host "Update complete!" -ForegroundColor Green
Write-Host "Usage: update-configurator.ps1 [-SkipBuild]" -ForegroundColor Cyan
Write-Host "  -SkipBuild: Skip rebuilding (use existing binaries)"
pause
