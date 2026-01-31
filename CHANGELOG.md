# Changelog

All notable changes to RF Media Link will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.9.0-dev] - 2026-01-31

### Added
- **ANSI Color Console Interface**: Modern, colorful configurator with automatic fallback for legacy terminals
- **Log Viewer**: Built-in Event Log viewer with filtering options (Recent, Errors & Warnings, Today) and detailed inspection
- **Toast Notifications**: Windows toast notifications for scan errors and issues (no spam on successful scans)
- **Service Control from Configurator**: Start, stop, and restart service directly from the configuration tool
- **Numbered Tag Selection**: Select tags by number instead of typing full UIDs for faster workflow
- **Backup & Restore System**: Automatic and manual backups with smart change detection to Documents\RFMediaLink\Backups
- **Auto-Reconnect**: Serial port automatically reconnects when RFID reader is unplugged and replugged
- **Centralized Version Management**: Single VERSION file with semantic versioning support
- **Update Scripts**: Separate update scripts for service and configurator (update-service.bat/ps1, update-configurator.bat/ps1)

### Changed
- **Log Levels**: Corrected log levels - normal operations now use LogLevel.Information instead of Warning
- **Event Log Provider**: Added explicit Microsoft.Extensions.Logging.EventLog configuration for proper Event Viewer integration
- **Log Viewer Filter**: "View Errors Only" renamed to "View Errors & Warnings" to include warning-level logs (Level 1-3)

### Fixed
- **Event Log Visibility**: Logs now properly appear in Windows Event Viewer with correct source "RFMediaLinkService"
- **PowerShell XPath Queries**: Fixed quote escaping in Event Log queries to prevent syntax errors
- **ANSI Color Support**: Added virtual terminal processing detection to prevent control characters on legacy terminals

### Technical
- Target Framework: .NET 8.0 Windows
- Event Log Provider: Microsoft.Extensions.Logging.EventLog
- Toast Notifications: Windows.UI.Notifications via PowerShell
- Console: Virtual Terminal Processing (ANSI escape codes)

---

## [0.8.0] - Previous Release

### Core Features
- Background service via scheduled task running at login with elevated privileges
- RFID tag to application launching (emulators, files, URLs, commands)
- Serial RFID reader support (custom firmware for ESP32-C3, RP2040)
- JSON configuration for catalog and emulator definitions
- Hot reload of configuration changes (no service restart needed)
- Foreground window activation using ALT key simulation
- Console configuration tool for tag and emulator management
- Backup and restore functionality
