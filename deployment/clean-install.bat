@echo off
REM Clean Install of RF Media Link Service - Run as Administrator
PowerShell -NoProfile -ExecutionPolicy Bypass -Command "& '%~dp0clean-install.ps1'"
pause
