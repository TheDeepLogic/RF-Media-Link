@echo off
REM Clean Install of RetroNFC Service - Run as Administrator
PowerShell -NoProfile -ExecutionPolicy Bypass -Command "& '%~dp0clean-install.ps1'"
pause
