@echo off
REM RF Media Link Release Packager
REM Creates installer package for distribution

PowerShell -NoProfile -ExecutionPolicy Bypass -File "%~dp0package-release.ps1" -AutoZip

if %errorLevel% equ 0 (
    echo.
    echo Package created successfully!
) else (
    echo.
    echo Package creation failed!
    pause
    exit /b 1
)
