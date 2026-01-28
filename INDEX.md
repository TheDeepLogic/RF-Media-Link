# RetroNFC Documentation Index

## 🚀 Getting Started (Start Here)

**New to RetroNFC?** Read these in order:

1. **[QUICK_START.md](QUICK_START.md)** - 5-minute installation and first use
2. **[README_CONFIG.md](README_CONFIG.md)** - What is RetroNFC and how it works
3. **[CONFIGURE_README.md](CONFIGURE_README.md)** - How to use the configuration tool

## 📚 User Guides

### Configuration Tool Usage
- **[CONFIGURE_README.md](CONFIGURE_README.md)** - Complete user manual for the config tool
- **[MENU_REFERENCE.md](MENU_REFERENCE.md)** - Quick reference for all menu options
- **[USAGE_EXAMPLES.md](USAGE_EXAMPLES.md)** - Real-world usage examples with screenshots

### Installation & Deployment
- **[QUICK_START.md](QUICK_START.md)** - Installation guide and setup
- **[INSTALL.md](INSTALL.md)** - Detailed installation instructions (if applicable)

### Emulator Configuration
- **[EMULATOR_GUIDE.md](EMULATOR_GUIDE.md)** - How to set up emulators

## 🔧 Technical Documentation

### Architecture & Design
- **[ARCHITECTURE.md](ARCHITECTURE.md)** - System design and component overview
- **[CONSOLE_TOOL_SUMMARY.md](CONSOLE_TOOL_SUMMARY.md)** - Console app technical details
- **[IMPLEMENTATION_COMPLETE.md](IMPLEMENTATION_COMPLETE.md)** - Full implementation reference

### Source Code
- **[RetroNFCConfigure/Program.cs](RetroNFCConfigure/Program.cs)** - Console app source (261 lines)
- **[RetroNFCService/](RetroNFCService/)** - Windows Service source code
- **[deployment/](deployment/)** - Installation and update scripts

## 📋 Quick Reference

### File Structure
```
%LOCALAPPDATA%\RetroNFC\
├── RetroNFCService.exe     (Windows Service)
├── RetroNFCConfigure.exe   (Configuration Tool)
├── config.json             (Settings)
├── catalog.json            (Your Tags)
├── emulators.json          (Emulator Definitions)
└── last_scan.txt           (Current Scan)
```

### Common Tasks

| Task | Guide | Steps |
|------|-------|-------|
| **First Install** | [QUICK_START.md](QUICK_START.md) | Run installer, configure tool |
| **Add a Tag** | [CONFIGURE_README.md](CONFIGURE_README.md) | Run configure.bat → 1 → A |
| **Change Serial Port** | [MENU_REFERENCE.md](MENU_REFERENCE.md) | Run configure.bat → 3 |
| **View Available Emulators** | [EMULATOR_GUIDE.md](EMULATOR_GUIDE.md) | Run configure.bat → 2 |
| **Update Service** | [QUICK_START.md](QUICK_START.md) | Run update-service.bat |
| **Uninstall** | [QUICK_START.md](QUICK_START.md) | Run installer with -Uninstall |

### Configuration Tool Commands

**Main Menu**:
```
1 = Manage Tags      (add/delete/list your tags)
2 = View Emulators   (see available systems)
3 = Settings         (serial port, baud rate)
4 = Exit             (close app)
```

**Manage Tags**:
```
A = Add Tag          (new tag)
D = Delete Tag       (remove tag)
B = Back to Menu     (return)
```

## 🐛 Troubleshooting

### Common Issues
- **Service won't start** → See Event Viewer: `Get-EventLog -LogName Application -Source RetroNFC`
- **Config tool won't run** → Check %LOCALAPPDATA%\RetroNFC\ exists
- **No tags scan** → Verify serial port in Settings (run configure.bat → 3)
- **Wrong emulator launches** → Check target name in config tool

More help: See [CONFIGURE_README.md](CONFIGURE_README.md) troubleshooting section

## 📖 Documentation Guide

| Document | Purpose | Audience |
|----------|---------|----------|
| **README_CONFIG.md** | Overview and features | Everyone |
| **QUICK_START.md** | Installation guide | New users |
| **CONFIGURE_README.md** | Complete manual | Daily users |
| **MENU_REFERENCE.md** | Command reference | Daily users |
| **USAGE_EXAMPLES.md** | Real examples | Visual learners |
| **ARCHITECTURE.md** | System design | Developers |
| **CONSOLE_TOOL_SUMMARY.md** | Implementation | Developers |
| **IMPLEMENTATION_COMPLETE.md** | Full technical details | Developers |
| **EMULATOR_GUIDE.md** | Emulator setup | Gaming users |

## 🔗 Important Files

### Executables
- `deployment\configure.bat` - Run configuration tool
- `deployment\install-retrof.bat` - Install (as Administrator)
- `deployment\update-service.bat` - Update service and tool
- `deployment\uninstall-retrof.bat` - Uninstall (as Administrator)

### Configuration (in %LOCALAPPDATA%\RetroNFC\)
- `config.json` - Serial port, baud rate
- `catalog.json` - Your RFID tag mappings
- `emulators.json` - Available emulator definitions

### Source Code
- `RetroNFCConfigure/Program.cs` - Configuration tool source
- `RetroNFCService/Program.cs` - Windows Service source
- `RetroNFCService/RfidWorker.cs` - RFID handling logic

## 📞 Support

**For help:**
1. Check [CONFIGURE_README.md](CONFIGURE_README.md) troubleshooting
2. Search [MENU_REFERENCE.md](MENU_REFERENCE.md) for your command
3. See [USAGE_EXAMPLES.md](USAGE_EXAMPLES.md) for similar tasks
4. Check Event Viewer for service errors

## ✅ Checklist

**First Time Setup:**
- [ ] Read [QUICK_START.md](QUICK_START.md)
- [ ] Run installer as Administrator
- [ ] Run `deployment\configure.bat`
- [ ] Add your first tag
- [ ] Test by scanning

**Ongoing:**
- [ ] Use `deployment\configure.bat` to manage tags
- [ ] Check [MENU_REFERENCE.md](MENU_REFERENCE.md) for options
- [ ] Run `deployment\update-service.bat` when updating

## 📞 Quick Links

- **Installation** → [QUICK_START.md](QUICK_START.md)
- **Usage** → [CONFIGURE_README.md](CONFIGURE_README.md)
- **Commands** → [MENU_REFERENCE.md](MENU_REFERENCE.md)
- **Examples** → [USAGE_EXAMPLES.md](USAGE_EXAMPLES.md)
- **Technical** → [ARCHITECTURE.md](ARCHITECTURE.md)
- **Emulators** → [EMULATOR_GUIDE.md](EMULATOR_GUIDE.md)

---

**Last Updated**: January 27, 2026  
**Version**: RetroNFC Console Configuration Tool v1.0  
**Status**: Ready for Production
