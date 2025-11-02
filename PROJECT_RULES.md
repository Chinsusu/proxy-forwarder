# ProxyForwarder Project Rules

## Build Output Paths

### Primary Output Directory
- **Build Output Path**: `bin/`
- **All compiled binaries and DLLs**: `bin/{Configuration}/{TargetFramework}/`
  - Example: `bin/Release/net6.0/`
  - Example: `bin/Debug/net6.0/`

### Configuration Variants
- **Debug Build**: `bin/Debug/`
- **Release Build**: `bin/Release/`

### Project-Specific Output
Each project outputs to its own `bin` directory:
- `src/ProxyForwarder.App/bin/` - WPF Application
- `src/ProxyForwarder.Core/bin/` - Core Library
- `src/ProxyForwarder.Forwarding/bin/` - Forwarding Service
- `src/ProxyForwarder.Infrastructure/bin/` - Infrastructure Library
- `src/ProxyForwarder.Tests/bin/` - Test Assemblies

### Intermediate Files
- **Object Files**: `obj/{Configuration}/{TargetFramework}/`
- **Example**: `src/ProxyForwarder.App/obj/Debug/net6.0/`

## Build Guidelines

### Build Commands
```powershell
# Build entire solution
dotnet build

# Build specific project
dotnet build src/ProxyForwarder.App/ProxyForwarder.App.csproj

# Release build
dotnet build -c Release

# Clean build artifacts
dotnet clean
```

### Publish Output
- **Publish Path**: `publish/`
- **Release Artifacts**: `publish/{ProjectName}/`
  - Example: `publish/ProxyForwarder.App/`

### Test Execution
```powershell
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true
```

## Directory Structure Rules

```
proxy-forwarder-skeleton/
├── src/
│   ├── ProxyForwarder.App/          # WPF UI Application
│   ├── ProxyForwarder.Core/         # Core business logic
│   ├── ProxyForwarder.Forwarding/   # Forwarding service
│   ├── ProxyForwarder.Infrastructure/ # Data & API access
│   └── ProxyForwarder.Tests/        # Unit tests
├── bin/                              # Build outputs (generated)
├── obj/                              # Intermediate files (generated)
├── build/                            # CI/CD configuration
├── publish/                          # Published artifacts (generated)
└── .gitignore                        # Should exclude bin/, obj/, publish/
```

## Git Rules

### Ignored Files
The `.gitignore` should exclude:
- `bin/`
- `obj/`
- `publish/`
- `.vs/` (Visual Studio cache)
- `*.user` (Project user settings)
- `.DS_Store` (macOS)

### Commit Guidelines
- **Initial commits**: Use "Initial commit"
- **Feature commits**: "feat: description"
- **Bug fixes**: "fix: description"
- **Documentation**: "docs: description"

## Code Style & Conventions

### Required File Header
All C# files must start with:
```csharp
// <copyright file="FileName.cs" company="Your Company">
// Copyright (c) Your Company. All rights reserved.
// </copyright>

namespace ProxyForwarder.Core;

using System;
using System.Collections.Generic;
```

### XML Documentation (Required)
All public types and members must have XML documentation:
```csharp
/// <summary>
/// Provides functionality for forwarding proxy connections.
/// </summary>
public interface IForwarderService
{
    /// <summary>
    /// Gets or sets the forwarder configuration.
    /// </summary>
    /// <value>The configuration settings for the forwarder.</value>
    ForwarderConfig? Config { get; set; }

    /// <summary>
    /// Starts the proxy forwarding service asynchronously.
    /// </summary>
    /// <param name="config">The configuration to use for forwarding.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StartAsync(ForwarderConfig config);
}
```

### Naming Conventions
- **Namespaces**: `ProxyForwarder.*`
- **Classes**: PascalCase (e.g., `ForwarderService`)
- **Interfaces**: IPascalCase (e.g., `IForwarderService`)
- **Methods**: PascalCase (e.g., `GetProxyRecords()`)
- **Properties**: PascalCase (e.g., `ProxyRecords`)
- **Fields**: camelCase with underscore prefix (e.g., `_repository`)
- **Parameters**: camelCase (e.g., `proxyConfig`)
- **Constants**: UPPER_CASE (e.g., `MAX_CONNECTIONS`)

### Brace Style (Allman)
- All braces on new lines
- Always use braces, even for single statements
```csharp
if (condition)
{
    DoSomething();
}
else
{
    DoSomethingElse();
}

for (int i = 0; i < count; i++)
{
    Process(i);
}
```

### Using Directives
- Place using statements INSIDE the namespace
- System namespaces first, then third-party, then project namespaces
- Sort alphabetically within each group
```csharp
namespace ProxyForwarder.Core;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ProxyForwarder.Core.Abstractions;
```

### File Organization
- One public class/interface per file
- File name exactly matches type name
- Max 120 characters per line
- 4-space indentation
- UTF-8 with BOM encoding

## CI/CD

### GitHub Actions
- Configuration: `build/ci.yml`
- Triggers on: push to `main`, pull requests
- Runs: build, test, code analysis

## Version Management

- **Framework Target**: .NET 6.0 or later
- **Language Version**: Latest C# (C# 10+)
- **Package Management**: NuGet via `.csproj` files

## Review Checklist

Before committing:
- [ ] Code compiles without warnings
- [ ] All tests pass
- [ ] Code follows naming conventions
- [ ] No hardcoded secrets or sensitive data
- [ ] Comments for complex logic

## Troubleshooting

### Build Issues

**Problem**: `TreatWarningsAsErrors` blocks build  
**Solution**: Set `TreatWarningsAsErrors=false` in `Directory.Build.props`

**Problem**: Missing XML documentation (CS1591)  
**Solution**: Add to `NoWarn`: `CS1591`

**Problem**: StyleCop warnings (SA1633, SA1636, etc.)  
**Solution**: Add to `NoWarn`: `SA1633,SA1636,SA1200,SA1513,SA1516,SA1309,SA1101`

### Runtime Issues

**Problem**: GUI doesn't appear after build  
**Cause**: DI container not initialized before ViewModel creation  
**Solution**:
- Don't use `StartupUri` in App.xaml
- Build DI container in `OnStartup()` BEFORE creating MainWindow
- Add global exception handlers to see errors:
  ```csharp
  DispatcherUnhandledException += (s, ex) => MessageBox.Show(ex.Exception.ToString());
  AppDomain.CurrentDomain.UnhandledException += (s, ex) => MessageBox.Show(ex.ExceptionObject?.ToString());
  ```

**Problem**: ViewModel can't access DI services  
**Solution**: Ensure `App.HostInstance` is set before ViewModel constructor runs

### XAML Issues

**Problem**: `'ColumnDefinitions' property is read-only`  
**Solution**: Use `<Grid.ColumnDefinitions>` element instead of attribute

**Problem**: `'Spacing' property does not exist` in WPF  
**Solution**: `Spacing` is WinUI only. Use `Margin` on child elements instead

## Recommended Improvements (Optional)

For production, consider:
1. Add Serilog logging to App.xaml.cs OnStartup/OnExit
2. Implement `IDisposable` in ViewModels for cleanup
3. Add error recovery in exception handlers (retry logic)
4. Use `Task.Run()` for long-running operations to prevent UI freeze
