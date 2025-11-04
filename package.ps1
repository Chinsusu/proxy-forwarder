# CloudMini Proxy Forwarder Packaging Script
# Creates a distributable ZIP package

$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$publishDir = Join-Path $projectRoot \"publish\"
$packageDir = Join-Path $projectRoot \"CloudMiniProxyForwarder-Package\"
$zipFile = Join-Path $projectRoot \"CloudMiniProxyForwarder-Setup.zip\"

Write-Host "CloudMini Proxy Forwarder - Packaging" -ForegroundColor Green
Write-Host "====================================" -ForegroundColor Green
Write-Host ""

# Verify publish directory exists
if (-not (Test-Path $publishDir)) {
    Write-Host "Error: publish directory not found at $publishDir" -ForegroundColor Red
    Write-Host "Please run: dotnet publish src\ProxyForwarder.App -c Release -o publish" -ForegroundColor Yellow
    exit 1
}

Write-Host "Creating package directory..." -ForegroundColor Cyan
if (Test-Path $packageDir) {
    Remove-Item $packageDir -Recurse -Force
}
New-Item $packageDir -ItemType Directory | Out-Null

Write-Host "Copying application files..." -ForegroundColor Cyan
Copy-Item "$publishDir\*" $packageDir -Recurse -Force

Write-Host "Copying installation scripts..." -ForegroundColor Cyan
Copy-Item "$projectRoot\install.bat" $packageDir -Force
Copy-Item "$projectRoot\uninstall.bat" $packageDir -Force
Copy-Item "$projectRoot\INSTALL.md" $packageDir -Force
Copy-Item "$projectRoot\README.md" $packageDir -Force -ErrorAction SilentlyContinue

Write-Host "Creating distribution package..." -ForegroundColor Cyan
if (Test-Path $zipFile) {
    Remove-Item $zipFile -Force
}

# Create ZIP file
Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory($packageDir, $zipFile, [System.IO.Compression.CompressionLevel]::Optimal, $true)

Write-Host ""
Write-Host "====================================" -ForegroundColor Green
Write-Host "Package created successfully!" -ForegroundColor Green
Write-Host "====================================" -ForegroundColor Green
Write-Host ""
Write-Host "Package location: $zipFile" -ForegroundColor Yellow
$fileSizeMB = [math]::Round((Get-Item $zipFile).Length / 1MB, 2)
Write-Host "Package size: $fileSizeMB MB" -ForegroundColor Yellow
Write-Host ""
Write-Host "Distribution ready! You can now:" -ForegroundColor Cyan
Write-Host "1. Copy $((Get-Item $zipFile).Name) to another machine" -ForegroundColor Cyan
Write-Host "2. Extract the ZIP file" -ForegroundColor Cyan
Write-Host "3. Run install.bat as Administrator" -ForegroundColor Cyan
Write-Host ""

# Cleanup
Write-Host "Cleaning up temporary files..." -ForegroundColor Cyan
Remove-Item $packageDir -Recurse -Force

Write-Host "Done!" -ForegroundColor Green
