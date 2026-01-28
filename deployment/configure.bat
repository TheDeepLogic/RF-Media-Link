@echo off
REM RetroNFC Configuration Tool Launcher
setlocal enabledelayedexpansion

set RETRODIR=%LOCALAPPDATA%\RetroNFC

if not exist "%RETRODIR%\RetroNFCConfigure.exe" (
    echo Error: RetroNFC Configure not found in AppData
    echo Expected location: %RETRODIR%
    pause
    exit /b 1
)

"%RETRODIR%\RetroNFCConfigure.exe"
