# Package RF Media Link for Release Distribution
# Creates a complete installer package for GitHub releases

param(
    [switch]$AutoZip
)

$ErrorActionPreference = "Stop"

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  RF Media Link Release Packager" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# Get paths
$deploymentDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootDir = Split-Path -Parent $deploymentDir
$releaseDir = Join-Path $deploymentDir "release"
$packageDir = Join-Path $releaseDir "RFMediaLink-Installer-x64"

# Get version from VERSION file
$versionFile = Join-Path $rootDir "VERSION"
if (Test-Path $versionFile) {
    $version = (Get-Content $versionFile -Raw).Trim()
} else {
    Write-Error "VERSION file not found at: $versionFile"
    exit 1
}

Write-Host "Packaging version: $version" -ForegroundColor Yellow
Write-Host ""

# Clean and create package directory
if (Test-Path $packageDir) {
    Write-Host "Removing old package..." -ForegroundColor Yellow
    Remove-Item -Path $packageDir -Recurse -Force
}
New-Item -ItemType Directory -Path $packageDir -Force | Out-Null

# Create subdirectories
$servicePublishDir = Join-Path $packageDir "service"
$configuratorPublishDir = Join-Path $packageDir "configurator"
$scriptsDir = Join-Path $packageDir "scripts"
$incDir = Join-Path $packageDir "inc"

New-Item -ItemType Directory -Path $servicePublishDir -Force | Out-Null
New-Item -ItemType Directory -Path $configuratorPublishDir -Force | Out-Null
New-Item -ItemType Directory -Path $scriptsDir -Force | Out-Null
New-Item -ItemType Directory -Path $incDir -Force | Out-Null

# Copy Service binaries
Write-Host "Copying service binaries..." -ForegroundColor Green
$serviceSrc = Join-Path $rootDir "RFMediaLinkService\bin\Release\net8.0-windows\publish"
if (-not (Test-Path $serviceSrc)) {
    Write-Host "ERROR: Service not built! Run build-release.bat first." -ForegroundColor Red
    pause
    exit 1
}
Copy-Item -Path "$serviceSrc\*" -Destination $servicePublishDir -Recurse -Force

# Copy Configurator binaries
Write-Host "Copying configurator binaries..." -ForegroundColor Green
$configuratorSrc = Join-Path $rootDir "RFMediaLink\bin\Release\net8.0-windows\publish"
if (-not (Test-Path $configuratorSrc)) {
    Write-Host "ERROR: Configurator not built! Run build-release.bat first." -ForegroundColor Red
    pause
    exit 1
}
Copy-Item -Path "$configuratorSrc\RFMediaLink.*" -Destination $configuratorPublishDir -Force

# Copy VERSION file to configurator directory
$versionFileSrc = Join-Path $rootDir "VERSION"
if (Test-Path $versionFileSrc) {
    Copy-Item -Path $versionFileSrc -Destination $configuratorPublishDir -Force
}

# Copy deployment scripts
Write-Host "Copying installation scripts..." -ForegroundColor Green
$deploymentSrc = Join-Path $rootDir "deployment"
Copy-Item -Path "$deploymentSrc\install.bat" -Destination $packageDir -Force
Copy-Item -Path "$deploymentSrc\install-rfmedialink.ps1" -Destination $packageDir -Force
Copy-Item -Path "$deploymentSrc\uninstall.bat" -Destination $scriptsDir -Force
Copy-Item -Path "$deploymentSrc\uninstall.ps1" -Destination $scriptsDir -Force
Copy-Item -Path "$deploymentSrc\start-service.ps1" -Destination $scriptsDir -Force
Copy-Item -Path "$deploymentSrc\stop-service.ps1" -Destination $scriptsDir -Force
Copy-Item -Path "$deploymentSrc\restart-service.ps1" -Destination $scriptsDir -Force
Copy-Item -Path "$deploymentSrc\update-service.ps1" -Destination $scriptsDir -Force
Copy-Item -Path "$deploymentSrc\open-catalog.bat" -Destination $scriptsDir -Force
Copy-Item -Path "$deploymentSrc\open-emulators.bat" -Destination $scriptsDir -Force
Copy-Item -Path "$deploymentSrc\configure.bat" -Destination $scriptsDir -Force

# Copy inc files
Write-Host "Copying configuration files..." -ForegroundColor Green
$incSrc = Join-Path $rootDir "inc"
Copy-Item -Path "$incSrc\emulators.json" -Destination $incDir -Force
if (Test-Path "$incSrc\RFMediaLink.ico") {
    Copy-Item -Path "$incSrc\RFMediaLink.ico" -Destination $incDir -Force
}

# Copy documentation
Write-Host "Copying documentation..." -ForegroundColor Green
Copy-Item -Path "$rootDir\README.md" -Destination $packageDir -Force
Copy-Item -Path "$rootDir\BOM.md" -Destination $packageDir -Force
$deploymentReadme = Join-Path $deploymentSrc "README.md"
if (Test-Path $deploymentReadme) {
    Copy-Item -Path $deploymentReadme -Destination "$packageDir\INSTALLATION.md" -Force
}

# Copy host examples
Write-Host "Copying host examples..." -ForegroundColor Green
$hostExamplesSrc = Join-Path $rootDir "host_examples"
$hostExamplesDest = Join-Path $packageDir "host_examples"
Copy-Item -Path $hostExamplesSrc -Destination $hostExamplesDest -Recurse -Force

# Create README for the package
Write-Host "Creating package README..." -ForegroundColor Green
$packageReadme = @"
# RF Media Link - Installation Package

Version: $version
Platform: Windows x64
.NET Runtime: .NET 8.0

## What's Included

- **service/**: RF Media Link Service binaries (background RFID monitoring service)
- **configurator/**: RF Media Link Configurator (catalog management tool)
- **scripts/**: Installation and management scripts
- **inc/**: Default configuration files and assets
- **host_examples/**: Example Arduino/microcontroller code for RFID readers
- **README.md**: Full documentation
- **INSTALLATION.md**: Installation guide

## Quick Install

1. **Run as Administrator**: Right-click ``install.bat`` and select "Run as administrator"
2. The installer will:
   - Install files to ``C:\ProgramData\RFMediaLink``
   - Create a scheduled task to run at login with elevated privileges
   - Create Start Menu shortcuts
   - Start the service

3. **Configure**: Use the desktop shortcut "RF Media Link Configure" to set up your catalog

## System Requirements

- Windows 10 or Windows 11 (x64)
- .NET 8.0 Runtime (included in installer)
- Administrator privileges for installation
- Serial port or USB for RFID reader connection

## Quick Start

1. Connect your RFID reader to a COM port
2. Run the Configurator and set the COM port
3. Add items to your catalog by scanning RFID tags
4. Assign emulators or actions to each tag
5. Scan tags to launch!

## Uninstall

Run ``scripts\uninstall.bat`` as administrator or use the Uninstall shortcut from the Start Menu.

## Support

See README.md for full documentation and troubleshooting.
GitHub: https://github.com/yourusername/RF-Media-Link

"@

$packageReadme | Out-File -FilePath (Join-Path $packageDir "PACKAGE_README.txt") -Encoding UTF8

Write-Host ""
Write-Host "============================================" -ForegroundColor Green
Write-Host "  Package Created Successfully!" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Green
Write-Host ""
Write-Host "Package location:" -ForegroundColor Yellow
Write-Host "  $packageDir" -ForegroundColor White
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Test the installer" -ForegroundColor White
Write-Host "  2. Create a ZIP archive of the package directory" -ForegroundColor White
Write-Host "  3. Upload to GitHub Releases" -ForegroundColor White
Write-Host ""

# Offer to create ZIP
$createZip = 'n'
if ($AutoZip) {
    $createZip = 'y'
} else {
    $createZip = Read-Host "Create ZIP archive now? (y/n)"
}

if ($createZip -eq 'y' -or $createZip -eq 'Y') {
    $zipPath = Join-Path $releaseDir "RFMediaLink-Installer-x64-v$version.zip"
    if (Test-Path $zipPath) {
        Remove-Item $zipPath -Force
    }
    Write-Host "Creating ZIP archive..." -ForegroundColor Green
    Compress-Archive -Path $packageDir -DestinationPath $zipPath -Force
    Write-Host "ZIP created: $zipPath" -ForegroundColor Green
}

if (-not $AutoZip) {
    pause
}
