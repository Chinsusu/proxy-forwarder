@echo off
REM CloudMini Proxy Forwarder Uninstall Script
REM Requires admin privileges

title CloudMini Proxy Forwarder Uninstaller

REM Check for admin privileges
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo Error: This script requires administrator privileges!
    echo Please run as Administrator.
    pause
    exit /b 1
)

echo.
echo ============================================
echo  CloudMini Proxy Forwarder Uninstaller
echo ============================================
echo.

set /p CONFIRM="Are you sure you want to uninstall CloudMini Proxy Forwarder? (Y/N): "
if /i not "%CONFIRM%"=="Y" (
    echo Uninstall cancelled.
    pause
    exit /b 0
)

REM Get install directory from registry
for /f "tokens=2*" %%a in ('reg query "HKLM\Software\Microsoft\Windows\CurrentVersion\Uninstall\CloudMiniProxyForwarder" /v "InstallPath" ^| findstr InstallPath') do set INSTALL_DIR=%%b

if not defined INSTALL_DIR (
    set INSTALL_DIR=%ProgramFiles%\CloudMini\ProxyForwarder
)

echo Uninstalling from: %INSTALL_DIR%
echo.

REM Remove shortcuts
echo Removing shortcuts...
del "%APPDATA%\Microsoft\Windows\Start Menu\Programs\CloudMini\ProxyForwarder.lnk" 2>nul
del "%USERPROFILE%\Desktop\ProxyForwarder.lnk" 2>nul

REM Remove installation directory
echo Removing application files...
rmdir /s /q "%INSTALL_DIR%"

REM Remove registry entries
echo Removing registry entries...
reg delete "HKLM\Software\Microsoft\Windows\CurrentVersion\Uninstall\CloudMiniProxyForwarder" /f >nul

echo.
echo ============================================
echo  Uninstall Complete!
echo ============================================
echo.
pause
