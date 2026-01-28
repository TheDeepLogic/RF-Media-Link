@echo off
REM RF Media Link Configuration Tool Launcher
setlocal enabledelayedexpansion

set RFMEDIALINK=%LOCALAPPDATA%\RFMediaLink

if not exist "%RFMEDIALINK%\RFMediaLink.exe" (
    echo Error: RF Media Link Configure not found in AppData
    echo Expected location: %RFMEDIALINK%
    pause
    exit /b 1
)

"%RFMEDIALINK%\RFMediaLink.exe"
