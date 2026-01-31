# Changelog

All notable changes to RF Media Link will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-01-31

**First Official Release**

### Core Features
- Background service via scheduled task running at login with elevated privileges
- RFID tag to application launching (emulators, files, URLs, commands)
- Serial RFID reader support (custom firmware for ESP32-C3, RP2040)
- JSON configuration for catalog and emulator definitions
- Hot reload of configuration changes (no service restart needed)
- Foreground window activation using ALT key simulation
- Console configuration tool for tag and emulator management
- Comprehensive emulator support (14 pre-configured emulators)

### Enhanced Management
- **ANSI Color Console Interface**: Modern, colorful configurator with automatic fallback for legacy terminals
- **Log Viewer**: Built-in Event Log viewer with filtering options (Recent, Errors & Warnings, Today) and detailed inspection
- **Interactive Notifications**: MessageBox prompts for scan errors with option to launch configurator
- **Service Control from Configurator**: Start, stop, and restart service directly from the configuration tool
- **Launch Tag Testing**: Test configured tags directly from configurator without scanning
- **Numbered Tag Selection**: Select tags by number instead of typing full UIDs for faster workflow
- **Backup & Restore System**: Automatic and manual backups with smart change detection to Documents\RFMediaLink\Backups
- **Auto-Reconnect**: Serial port automatically reconnects when RFID reader is unplugged and replugged
- **Centralized Version Management**: Single VERSION file with semantic versioning support
- **Emulator Management**: View and edit emulator definitions from configurator

### Changed
### Improvements
- **Log Levels**: Corrected log levels - normal operations use LogLevel.Information instead of Warning
- **Event Log Provider**: Explicit Microsoft.Extensions.Logging.EventLog configuration for proper Windows Event Viewer integration
- **Log Viewer Filter**: "View Errors Only" renamed to "View Errors & Warnings" to include warning-level logs (Level 1-3)
- **ANSI Color Support**: Virtual terminal processing detection to prevent control characters on legacy terminals
- **Serial Port Reconnection**: Automatic retry every 5 seconds with disconnect detection
- **Process Termination**: Extended wait times to ensure clean process shutdowns

### Bug Fixes
- **Event Log Visibility**: Logs now properly appear in Windows Event Viewer with correct source "RFMediaLinkService"
- **PowerShell XPath Queries**: Fixed quote escaping in Event Log queries
- **Close Other Instances**: Fixed `close_other_instances` and `close_other_emulators` flags not being observed
- **ROM File Loading**: Empty flag fields now work correctly as positional arguments
- **File Launch Focus**: Executable files launched via "file" action now receive focus automatically
- **Notification Suppression**: Service now suppresses notifications when configurator is already running

### Technical Details
- **Target Framework**: .NET 8.0 Windows
- **Event Log Provider**: Microsoft.Extensions.Logging.EventLog
- **Notifications**: Windows MessageBox via PowerShell (Session 0 compatible)
- **Console**: Virtual Terminal Processing (ANSI escape codes)
- **Version Control**: Centralized VERSION file with semantic versioning

### Development History
- **0.9.0-dev**: Development version with enhanced management features
- **0.8.0**: Initial development version with core functionality

---

## Version History

### [1.0.0] - 2026-01-31
First official release with complete feature set

### [0.9.0] - Development Phase
Enhanced management and user experience features

### [0.8.0] - Initial Development
Core functionality and architecture
