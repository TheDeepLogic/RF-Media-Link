@echo off
REM Build and Publish RF Media Link
REM Builds both the Service and Configurator for release

echo ============================================
echo   Building RF Media Link for Release
echo ============================================
echo.

REM Get parent directory (repo root)
set SCRIPT_DIR=%~dp0
cd /d "%SCRIPT_DIR%.."
set ROOT_DIR=%CD%\

REM Build RFMediaLinkService
echo [1/2] Building RFMediaLinkService...
cd "%ROOT_DIR%RFMediaLinkService"
dotnet publish -c Release
if %errorLevel% neq 0 (
    echo ERROR: RFMediaLinkService build failed!
    pause
    exit /b 1
)
echo Service build complete
echo.

REM Build RFMediaLink (Configurator)
echo [2/2] Building RFMediaLink Configurator...
cd "%ROOT_DIR%RFMediaLink"
dotnet publish -c Release
if %errorLevel% neq 0 (
    echo ERROR: RFMediaLink build failed!
    pause
    exit /b 1
)
echo Configurator build complete
echo.

echo ============================================
echo   Build Complete!
echo ============================================
echo.
echo Service binaries:
echo   %ROOT_DIR%RFMediaLinkService\bin\Release\net8.0-windows\publish\
echo.
echo Configurator binaries:
echo   %ROOT_DIR%RFMediaLink\bin\Release\net8.0-windows\publish\
echo.

pause
