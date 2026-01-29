@echo off
REM RF Media Link Configuration Tool Launcher
setlocal enabledelayedexpansion

set RFMEDIALINK=%ProgramData%\RFMediaLink

if not exist "%RFMEDIALINK%\RFMediaLink.exe" (
    echo Error: RF Media Link Configure not found in ProgramData
    echo Expected location: %RFMEDIALINK%
    pause
    exit /b 1
)

"%RFMEDIALINK%\RFMediaLink.exe"
