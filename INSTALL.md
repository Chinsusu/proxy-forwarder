# CloudMini Proxy Forwarder - Installation Guide

## Prerequisites

- Windows 7 or later (64-bit recommended)
- .NET Runtime 8.0 or later
- Administrator privileges (required for installation)

## Installation Steps

### Option 1: Using install.bat (Recommended)

1. Extract the zip file to a temporary location
2. Right-click on `install.bat` and select \"Run as Administrator\"
3. Follow the on-screen prompts
4. The application will be installed to: `C:\Program Files\CloudMini\ProxyForwarder`
5. Shortcuts will be created on your Desktop and Start Menu

### Option 2: Manual Installation

1. Extract the zip file
2. Copy all contents to desired installation location (e.g., `C:\Program Files\CloudMini\ProxyForwarder`)
3. Run `ProxyForwarder.App.exe`

## First Launch

After installation:

1. Open ProxyForwarder from Start Menu or Desktop shortcut
2. Go to **Settings** tab and configure:
   - Port Range (default: 12000-14999)
   - UDP blocking (optional)
3. Go to **Import** tab to add proxies using CloudMini API token
4. Go to **Proxies** tab - proxies will auto-fetch ISP/Ping info
5. Go to **Forwarders** tab and toggle proxy status to start forwarding

## Uninstallation

### Using uninstall.bat

1. Right-click on `uninstall.bat` and select \"Run as Administrator\"
2. Confirm when prompted
3. The application will be removed, including shortcuts and registry entries

### Manual Uninstallation

1. Delete the installation folder
2. Delete shortcuts from Start Menu and Desktop
3. Remove from Control Panel > Programs and Features (CloudMini Proxy Forwarder)

## System Requirements

- Minimum: 2GB RAM
- Recommended: 4GB+ RAM
- .NET Runtime 8.0 (automatically required)

## Troubleshooting

### Application won't start
- Ensure .NET Runtime 8.0 is installed
- Check that the installation folder has read/write permissions
- Try running as Administrator

### Install script fails
- Ensure you're running as Administrator
- Check that the `publish` folder is in the same directory as `install.bat`
- Disable antivirus temporarily (some may block installation)

### Firewall warnings
- When first launched, allow the app through Windows Defender Firewall
- Accept prompts for localhost forwarding

## Support

For issues or questions, please visit the project repository on GitHub.

## License

Proprietary software. All rights reserved.
