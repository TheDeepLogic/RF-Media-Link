# Create RF Media Link Windows Service
# Run as Administrator

$ServiceName = "RF Media Link"
$DisplayName = "RF Media Link"
$ExePath = "$env:LOCALAPPDATA\RFMediaLink\RetroNFCService.exe"

function Test-Administrator {
    $currentUser = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($currentUser)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

if (-not (Test-Administrator)) {
    Write-Error "This script must be run as Administrator!"
    exit 1
}

# Stop old service if exists
$oldService = Get-Service -Name "RetroNFC" -ErrorAction SilentlyContinue
if ($oldService) {
    Write-Host "Stopping old RetroNFC service..."
    sc.exe stop "RetroNFC" | Out-Null
    Start-Sleep -Seconds 2
    sc.exe delete "RetroNFC" | Out-Null
    Write-Host "Old service removed"
}

# Check if service already exists
$existingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($existingService) {
    Write-Host "Service already exists, stopping it..."
    sc.exe stop $ServiceName | Out-Null
    Start-Sleep -Seconds 2
    sc.exe delete $ServiceName | Out-Null
    Write-Host "Old service deleted"
    Start-Sleep -Seconds 1
}

# Verify exe exists
if (-not (Test-Path $ExePath)) {
    Write-Error "Service executable not found: $ExePath"
    exit 1
}

# Create new service
Write-Host "Creating RF Media Link service..."
sc.exe create $ServiceName binPath= "`"$ExePath`"" start= auto DisplayName= "$DisplayName" | Out-Null

if ($LASTEXITCODE -eq 0) {
    Write-Host "Service created successfully"
    
    # Start service
    Write-Host "Starting service..."
    Start-Service -Name $ServiceName -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
    
    $service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
    if ($service.Status -eq "Running") {
        Write-Host "✓ Service is running"
    } else {
        Write-Host "⚠ Service status: $($service.Status)"
    }
    
    Write-Host ""
    Write-Host "RF Media Link service installation complete!"
    Write-Host "Service name: $ServiceName"
    Write-Host "Executable: $ExePath"
} else {
    Write-Error "Failed to create service (error code: $LASTEXITCODE)"
    exit 1
}
