; CloudMini Proxy Forwarder Installer
; NSIS Script

!include "MUI2.nsh"
!include "x64.nsh"

; Basic settings
Name "CloudMini Proxy Forwarder"
OutFile "CloudMiniProxyForwarder-Setup.exe"
InstallDir "$PROGRAMFILES\CloudMini\ProxyForwarder"
InstallDirRegKey HKLM "Software\CloudMini\ProxyForwarder" "InstallPath"

; Default section
RequestExecutionLevel admin

; MUI Settings
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_LANGUAGE "English"

; Installer sections
Section "Install"
    SetOutPath "$INSTDIR"
    
    ; Copy all files from publish directory
    File /r "publish\*.*"
    
    ; Create start menu shortcuts
    CreateDirectory "$SMPROGRAMS\CloudMini"
    CreateShortcut "$SMPROGRAMS\CloudMini\ProxyForwarder.lnk" "$INSTDIR\ProxyForwarder.App.exe"
    CreateShortcut "$SMPROGRAMS\CloudMini\Uninstall.lnk" "$INSTDIR\Uninstall.exe"
    
    ; Create desktop shortcut
    CreateShortcut "$DESKTOP\ProxyForwarder.lnk" "$INSTDIR\ProxyForwarder.App.exe"
    
    ; Register uninstaller
    CreateDirectory "$INSTDIR"
    CreateUninstaller "$INSTDIR\Uninstall.exe"
    
    ; Write registry keys
    WriteRegStr HKLM "Software\CloudMini\ProxyForwarder" "InstallPath" "$INSTDIR"
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\CloudMiniProxyForwarder" "DisplayName" "CloudMini Proxy Forwarder"
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\CloudMiniProxyForwarder" "UninstallString" "$INSTDIR\Uninstall.exe"
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\CloudMiniProxyForwarder" "DisplayIcon" "$INSTDIR\ProxyForwarder.App.exe"
SectionEnd

; Uninstaller section
Section "Uninstall"
    ; Remove shortcuts
    Delete "$SMPROGRAMS\CloudMini\ProxyForwarder.lnk"
    Delete "$SMPROGRAMS\CloudMini\Uninstall.lnk"
    Delete "$DESKTOP\ProxyForwarder.lnk"
    RMDir "$SMPROGRAMS\CloudMini"
    
    ; Remove installed files
    RMDir /r "$INSTDIR"
    
    ; Remove registry keys
    DeleteRegKey HKLM "Software\CloudMini\ProxyForwarder"
    DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\CloudMiniProxyForwarder"
SectionEnd
