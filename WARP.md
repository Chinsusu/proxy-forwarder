# WARP.md

This file provides guidance to WARP (warp.dev) when working with code in this repository.

## Build & Run Commands

**Build the entire solution:**
```powershell
dotnet build src/ProxyForwarder.sln
```

**Run the desktop application:**
```powershell
dotnet run --project src/ProxyForwarder.App/ProxyForwarder.App.csproj
```

**Build in Release mode:**
```powershell
dotnet build src/ProxyForwarder.sln -c Release
```

**Run tests:**
```powershell
dotnet test src/ProxyForwarder.Tests/ProxyForwarder.Tests.csproj
```

**Run a single test:**
```powershell
dotnet test src/ProxyForwarder.Tests/ProxyForwarder.Tests.csproj -k ProxyParserTests
```

## Architecture Overview

The solution follows a clean layered architecture with five main projects:

### **ProxyForwarder.Core**
Foundation library containing abstractions and entities.
- **Abstractions/**: Service interfaces (`IForwarderService`, `IProxyRepository`, `ISettingsProvider`, etc.)
- **Entities/**: Domain models (`ProxyRecord`, `ForwarderConfig`, `Region`)
- **Common/**: Utilities like `ProxyParser` (parses proxy strings in format `HOST:PORT:USER:PASS` or `HOST:ID:PORT:USER:PASS`) and `ProxyTypeClassifier`

### **ProxyForwarder.Forwarding**
HTTP proxy chaining engine implementing the core forwarding logic.
- **ForwarderService**: Manages lifecycle of proxy forwarders (start/stop/check status)
- **ChainedHttpProxy**: HTTP/HTTPS proxy chaining handler that chains connections through an upstream proxy
- **PortAllocator**: Allocates available local ports from a configurable range (default: 11000-14999)
- No SSL MITM, uses HTTP CONNECT tunneling for HTTPS to prevent DNS leaks

### **ProxyForwarder.Infrastructure**
External dependencies and services integration.
- **Api/**: `CloudMiniClient` for cloud API integration
- **Data/**: `ForwarderDbContext` for database operations
- **Security/**: `SecureStorage` for credential management
- **Services/**: `ProxyRepository`, `SettingsProvider`, `IpWhoisClient`, `LatencyProbe`, `UdpBlocker`

### **ProxyForwarder.App**
WPF desktop application (Windows-only, uses `net8.0-windows` target).
- **Views/**: XAML UI views (`ForwardersView`, `ProxiesView`, `SettingsView`, `LogsView`, `ImportView`)
- **ViewModels/**: MVVM ViewModels for each view
- **Converters/**: Value converters for XAML binding (`LocalPortConverter`, `StatusToButtonTextConverter`, etc.)

### **ProxyForwarder.Tests**
Test project using xUnit framework.
- Tests for `ProxyParser` (supports both standard and ID-based proxy formats)
- Tests for `PortAllocator` port allocation logic
- Framework: xUnit 2.7.0

## Key Patterns

- **Dependency Injection**: Likely configured via `Microsoft.Extensions.Hosting` (referenced in App.csproj)
- **Async/Await**: Heavy use of async patterns throughout (ForwarderService uses `CancellationToken`)
- **Thread-Safe Collections**: `ForwarderService` uses lock-based synchronization for `_running` dictionary
- **Debug Output**: Debug traces via `Debug.WriteLine()` for troubleshooting

## Important Implementation Details

- Proxy format parsing supports two formats: `HOST:PORT:USER:PASS` and `HOST:ID:PORT:USER:PASS` (ID is discarded)
- DNS resolution happens at forwarder startup with fallback to original hostname if resolution fails
- Local forwarders bind to `127.0.0.1` with allocated ports from the configured range
- All upstream proxy connections go through the `ChainedHttpProxy` handler (no direct client-to-proxy connections)
