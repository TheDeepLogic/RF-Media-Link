# Version Management

## Centralized Version Control

The project version is controlled by a single file: **`VERSION`**

This file contains just the version string (e.g., `1.0.0` or `1.1.0-beta`)

## Where Version is Used

1. **Configurator Title**: Displayed as "RF Media Link Configuration Tool v{VERSION}"
2. **Release Package Name**: `RFMediaLink-Installer-x64-v{VERSION}.zip`
3. **Release Notes**: Version header in RELEASE_NOTES.md

## How to Increment Version

### 1. Update the VERSION File

Edit `VERSION` in the root directory:
```
1.0.2-alpha
```

### 2. Update RELEASE_NOTES.md (Optional)

Edit `deployment/release/RELEASE_NOTES.md`:
- Update version in header
- Add "What's New in X.X.X" section
- Update build date

### 3. Build and Package

From `deployment` folder:
```batch
.\build-release.bat
.\package-release.ps1 -AutoZip
```

The scripts will automatically:
- Read version from VERSION file
- Include VERSION file in configurator build
- Display version in configurator window
- Create ZIP with correct version name: `RFMediaLink-Installer-x64-vX.X.X.zip`

## Version Numbering Convention

Use semantic versioning with optional pre-release tags:

- **Major.Minor.Patch** (e.g., `1.0.1`)
- **Major.Minor.Patch-prerelease** (e.g., `1.0.1-alpha`, `1.0.2-beta`, `2.0.0-rc1`)

### Examples:
- `1.0.1-alpha` - Alpha release (testing/unstable features)
- `1.0.1-beta` - Beta release (feature complete, testing)
- `1.0.1-rc1` - Release candidate
- `1.0.1` - Stable release
- `1.1.0` - Minor version with new features
- `2.0.0` - Major version with breaking changes

## No Manual Version Updates Needed

**You no longer need to manually update:**
- ❌ package-release.ps1 (reads from VERSION file)
- ❌ Program.cs (loads VERSION file at runtime)

**Only update:**
- ✅ VERSION file (one line, one place)
- ✅ RELEASE_NOTES.md (for documentation)
