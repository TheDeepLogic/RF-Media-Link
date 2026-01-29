# Configure RF Media Link Service Account
# Run as Administrator
# This script changes the service logon account without reinstalling

$ServiceName = "RF Media Link"

function Test-Administrator {
    $currentUser = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($currentUser)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

if (-not (Test-Administrator)) {
    Write-Error "This script must be run as Administrator!"
    pause
    exit 1
}

# Check if service exists
$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if (-not $service) {
    Write-Error "Service '$ServiceName' not found. Please install the service first."
    pause
    exit 1
}

Write-Host "============================================"
Write-Host "  Configure RF Media Link Service Account"
Write-Host "============================================"
Write-Host ""
Write-Host "Current service configuration:"
$serviceInfo = Get-CimInstance Win32_Service -Filter "Name='$ServiceName'"
Write-Host "  Account: $($serviceInfo.StartName)"
Write-Host "  Status: $($service.Status)"
Write-Host ""
Write-Host "To launch GUI applications, the service must run under your user account."
Write-Host ""
Write-Host "Options:"
Write-Host "  1. Run as $env:USERDOMAIN\$env:USERNAME (recommended)"
Write-Host "  2. Run as LocalSystem (cannot launch GUIs)"
Write-Host "  3. Run as custom account"
Write-Host "  0. Cancel"
Write-Host ""
$choice = Read-Host "Select option (1-3, 0=cancel)"

switch ($choice) {
    "1" {
        $accountName = "$env:USERDOMAIN\$env:USERNAME"
        Write-Host ""
        Write-Host "Enter password for $accountName"
        $securePassword = Read-Host "Password" -AsSecureString
        $password = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword))
    }
    "2" {
        $accountName = "LocalSystem"
        $password = $null
        Write-Warning "LocalSystem account cannot launch GUI applications!"
    }
    "3" {
        Write-Host ""
        $accountName = Read-Host "Enter account name (DOMAIN\Username or .\Username for local)"
        Write-Host "Enter password for $accountName"
        $securePassword = Read-Host "Password" -AsSecureSign
        $password = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword))
    }
    "0" {
        Write-Host "Cancelled"
        pause
        exit 0
    }
    default {
        Write-Error "Invalid choice"
        pause
        exit 1
    }
}

Write-Host ""
Write-Host "Stopping service..."
Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2

Write-Host "Configuring service account..."
if ($accountName -eq "LocalSystem") {
    sc.exe config $ServiceName obj= "LocalSystem" | Out-Null
} else {
    sc.exe config $ServiceName obj= $accountName password= $password | Out-Null
}

if ($LASTEXITCODE -eq 0) {
    Write-Host "Service account configured successfully!"
    Write-Host ""
    Write-Host "Starting service..."
    Start-Service -Name $ServiceName -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
    
    $service = Get-Service -Name $ServiceName
    if ($service.Status -eq "Running") {
        Write-Host "Service is running"
    } else {
        Write-Warning "Service failed to start. Status: $($service.Status)"
        Write-Host "Check Event Viewer for errors (Application log)"
        Write-Host "Common issues:"
        Write-Host "  - Incorrect password"
        Write-Host "  - Account doesn't have 'Log on as a service' rights"
        Write-Host "  - Serial port permissions"
    }
} else {
    Write-Error "Failed to configure service account (error code: $LASTEXITCODE)"
    Write-Host "Common causes:"
    Write-Host "  - Incorrect password"
    Write-Host "  - Account name format wrong (use DOMAIN\Username)"
}

Write-Host ""
pause
