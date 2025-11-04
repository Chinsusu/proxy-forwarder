# Versioning & Release Guide

This document explains how to manage versions and create releases for Proxy Forwarder using two methods: **automatic CI/CD** and **manual tagging**.

## Overview

- **Version Format**: Semantic Versioning (`MAJOR.MINOR.PATCH`, e.g., `0.3.2`)
- **Version Source**: Determined by git tags (format: `vX.Y.Z`)
- **Version Display**: Shown in app title (e.g., `Proxy Forwarder  v0.3.2`)
- **Release Artifacts**: Self-contained Windows installer ZIP

## Method A: Automatic via GitHub Actions

### How It Works

When you push a commit to `main`/`master` with `[release]` in the commit message, GitHub Actions automatically:

1. Detects the trigger
2. Calculates the new version (auto-bumps PATCH if not specified)
3. Builds and publishes the app
4. Creates a GitHub Release with tag `vX.Y.Z`
5. Attaches the installer ZIP

### Usage

**Option 1: Auto-bump patch version**

```bash
git add .
git commit -m "fix: improve proxy parser [release]"
git push origin main
```

Example progression:
- Current tag: `v0.3.1`
- New tag: `v0.3.2` ✓ (patch auto-bumped)

**Option 2: Manual version input via UI**

1. Go to GitHub repo → **Actions** tab
2. Select **"Release & Tag"** workflow
3. Click **"Run workflow"**
4. Enter version (e.g., `0.4.0`) or leave empty for auto-bump
5. Click **"Run workflow"**

The workflow will:
- Use your specified version if provided
- Otherwise, auto-bump patch from the latest tag
- Create release + tag automatically

### First Release

When there are no tags yet:
- First auto-triggered release will be `v0.1.1` (starting from `0.1.0`, then +1)
- Or you can manually specify `0.1.0` on first workflow run

## Method B: Manual Tagging (Local)

Use this if you prefer to tag locally before pushing.

### Quick Version Bump Script

```bash
# Auto-bump patch from latest tag
LAST=$(git describe --tags --abbrev=0 2>/dev/null | sed 's/^v//')
[ -z "$LAST" ] && LAST="0.1.0"
IFS='.' read -r MA MI PA <<<"$LAST"
PA=$((PA+1))
V="${MA}.${MI}.${PA}"

# Tag and push
git tag -a "v$V" -m "Release v$V"
git push origin "v$V"
```

### Manual Tag Example

```bash
# Bump minor version manually
git tag -a "v0.4.0" -m "Release v0.4.0"
git push origin "v0.4.0"
```

### Build with Custom Version

Build locally with a specific version:

```powershell
dotnet publish src/ProxyForwarder.App/ProxyForwarder.App.csproj `
  -c Release -r win-x64 --self-contained true `
  /p:Version=0.4.1 `
  -o out/win-x64
```

The `Directory.Build.props` will embed this version into the app.

## Configuration Files

### Directory.Build.props

Defines the base version (mutable by CI):

```xml
<Version>0.1.0</Version>
<AssemblyVersion>$(Version)</AssemblyVersion>
<FileVersion>$(Version)</FileVersion>
<InformationalVersion>$(Version)</InformationalVersion>
```

When CI runs with `/p:Version=0.3.2`, all these are set to `0.3.2`.

### .github/workflows/release-tag.yml

GitHub Actions workflow that:
- Listens to `workflow_dispatch` (manual trigger) and push with `[release]`
- Calculates version
- Publishes self-contained app
- Creates Release + Tag

## Version Display in App

The `MainWindow.xaml.cs` reads the `InformationalVersion` attribute at runtime:

```csharp
var version = Assembly.GetExecutingAssembly()
    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
    .InformationalVersion;
// Title becomes: "Proxy Forwarder  v0.3.2"
```

This happens automatically; no code changes needed per release.

## Typical Workflow

### For regular updates:

1. **Make changes** and test locally
2. **Commit** with or without `[release]`:
   ```bash
   git commit -m "feat: add SOCKS5 support [release]"
   ```
3. **Push**:
   ```bash
   git push origin main
   ```
4. **CI automatically** tags and creates Release

### For hotfixes or specific versions:

1. **Push normally** (no `[release]` tag)
2. **Go to Actions → Release & Tag → Run workflow**
3. **Enter version** (e.g., `0.3.3`) or leave empty
4. **CI creates tag + Release**

## Checking Current Version

```bash
# Latest tag
git describe --tags --abbrev=0

# All tags (newest first)
git tag -l --sort=-version:refname | head -10

# View released versions
# GitHub Releases tab shows all tagged releases with artifacts
```

## Notes

- Tags must follow format `vX.Y.Z` (e.g., `v0.3.2`)
- CI workflow ignores commits without `[release]` keyword
- Manual workflow always runs (even without tag in message)
- Release artifacts are auto-attached to GitHub Releases
- No external actions/secrets needed; uses default `GITHUB_TOKEN`

## Troubleshooting

**Q: Workflow ran but didn't create release**
- A: Check commit message has `[release]` or manually trigger workflow

**Q: Version not showing in app**
- A: Rebuild with correct `/p:Version` or check MainWindow is loading correctly

**Q: Can't push tags**
- A: Ensure you have push permissions: `git push origin <tag-name>`

**Q: Workflow failed - permission denied**
- A: Repo settings → Actions → General → Workflow permissions → set to "Read and write permissions"
