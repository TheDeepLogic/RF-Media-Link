@echo off
REM RetroNFC Service Installer Batch Wrapper
REM Right-click and select "Run as administrator"

setlocal enabledelayedexpansion

REM Check for admin
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo ERROR: This installer must be run as Administrator!
    echo Please right-click this file and select "Run as administrator"
    pause
    exit /b 1
)

REM Get the directory where this script is located
set SCRIPT_DIR=%~dp0

REM Run PowerShell installer
powershell -ExecutionPolicy Bypass -File "%SCRIPT_DIR%install-retrof.ps1"

if %errorLevel% equ 0 (
    echo.
    echo Installation successful!
    pause
) else (
    echo.
    echo Installation failed!
    pause
    exit /b 1
)
