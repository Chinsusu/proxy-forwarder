@echo off
REM CloudMini Proxy Forwarder Install Script
REM Requires admin privileges

title CloudMini Proxy Forwarder Installer

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
echo  CloudMini Proxy Forwarder Installer
echo ============================================
echo.

REM Set install directory
set INSTALL_DIR=%ProgramFiles%\CloudMini\ProxyForwarder

echo Installing to: %INSTALL_DIR%
echo.

REM Create installation directory
if not exist "%INSTALL_DIR%" (
    mkdir "%INSTALL_DIR%"
    echo Created installation directory.
)

REM Copy files
echo Copying files...
xcopy /E /I /Y "publish\*" "%INSTALL_DIR%"

if %errorlevel% neq 0 (
    echo Error: Failed to copy files!
    pause
    exit /b 1
)

REM Create shortcuts
echo Creating shortcuts...
set TARGET=%INSTALL_DIR%\ProxyForwarder.App.exe
set SHORTCUT_DIR=%APPDATA%\Microsoft\Windows\Start Menu\Programs\CloudMini

if not exist "%SHORTCUT_DIR%" mkdir "%SHORTCUT_DIR%"

REM Create Windows Start Menu shortcut using PowerShell
powershell -Command ^
    "$WshShell = New-Object -ComObject WScript.Shell; " ^
    "$Shortcut = $WshShell.CreateShortcut('%SHORTCUT_DIR%\ProxyForwarder.lnk'); " ^
    "$Shortcut.TargetPath = '%TARGET%'; " ^
    "$Shortcut.WorkingDirectory = '%INSTALL_DIR%'; " ^
    "$Shortcut.Save()"

REM Create Desktop shortcut using PowerShell
powershell -Command ^
    "$WshShell = New-Object -ComObject WScript.Shell; " ^
    "$Shortcut = $WshShell.CreateShortcut('%USERPROFILE%\Desktop\ProxyForwarder.lnk'); " ^
    "$Shortcut.TargetPath = '%TARGET%'; " ^
    "$Shortcut.WorkingDirectory = '%INSTALL_DIR%'; " ^
    "$Shortcut.Save()"

REM Add to registry for uninstall
reg add "HKLM\Software\Microsoft\Windows\CurrentVersion\Uninstall\CloudMiniProxyForwarder" /v "DisplayName" /d "CloudMini Proxy Forwarder" /f >nul
reg add "HKLM\Software\Microsoft\Windows\CurrentVersion\Uninstall\CloudMiniProxyForwarder" /v "UninstallString" /d "%INSTALL_DIR%\uninstall.bat" /f >nul
reg add "HKLM\Software\Microsoft\Windows\CurrentVersion\Uninstall\CloudMiniProxyForwarder" /v "DisplayIcon" /d "%TARGET%" /f >nul
reg add "HKLM\Software\Microsoft\Windows\CurrentVersion\Uninstall\CloudMiniProxyForwarder" /v "InstallPath" /d "%INSTALL_DIR%" /f >nul

echo.
echo ============================================
echo  Installation Complete!
echo ============================================
echo.
echo You can now launch ProxyForwarder from:
echo - Start Menu: CloudMini > ProxyForwarder
echo - Desktop: ProxyForwarder shortcut
echo.
pause
