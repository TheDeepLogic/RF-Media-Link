@echo off
REM RF Media Link Service Uninstaller
REM Right-click and select "Run as administrator"

setlocal enabledelayedexpansion

REM Check for admin
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo ERROR: This uninstaller must be run as Administrator!
    echo Please right-click this file and select "Run as administrator"
    pause
    exit /b 1
)

REM Get the directory where this script is located
set SCRIPT_DIR=%~dp0

REM Run PowerShell uninstaller
powershell -ExecutionPolicy Bypass -File "%SCRIPT_DIR%install-rfmedialink.ps1" -Uninstall

if %errorLevel% equ 0 (
    echo.
    echo Uninstall successful!
    pause
) else (
    echo.
    echo Uninstall failed!
    pause
    exit /b 1
)
