@echo off
REM RF Media Link Configurator Update Script
REM Right-click and select "Run as administrator"

REM Check for admin
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo ERROR: This script must be run as Administrator!
    echo Please right-click this file and select "Run as administrator"
    pause
    exit /b 1
)

PowerShell -NoProfile -ExecutionPolicy Bypass -Command "& '%~dp0update-configurator.ps1'"
