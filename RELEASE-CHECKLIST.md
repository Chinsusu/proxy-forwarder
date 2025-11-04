# Pre-Release Checklist

Use this checklist before creating a new release to ensure quality and consistency.

## Code Quality

- [ ] Code builds without errors: `dotnet build src/ProxyForwarder.sln -c Release`
- [ ] All tests pass: `dotnet test src/ProxyForwarder.Tests/ProxyForwarder.Tests.csproj`
- [ ] No new compiler warnings introduced (or documented)
- [ ] Code follows project style (StyleCop, Roslynator)

## Testing

- [ ] Tested locally on Windows 10/11 (64-bit)
- [ ] Application launches without errors
- [ ] Core features verified:
  - [ ] Proxy list displays correctly
  - [ ] Can add/edit proxy configurations
  - [ ] Proxy forwarder starts and stops properly
  - [ ] Settings persist across restarts
  - [ ] Log output shows expected entries

## Documentation

- [ ] Changes documented in commit message
- [ ] If breaking changes: update README.md or relevant docs
- [ ] Test any new or modified functionality documented in comments

## Version & Release

### Automatic Release (Recommended)

```bash
# Commit with [release] keyword
git add .
git commit -m "feat: describe your changes [release]"
git push origin main
```

CI will automatically:
- Bump patch version (v0.1.0 → v0.1.1)
- Build and publish to GitHub Releases
- Create installer ZIP

### Manual Release (If preferred)

1. **Check current version:**
   ```bash
   git describe --tags --abbrev=0
   ```

2. **Tag manually:**
   ```bash
   git tag -a "v0.2.0" -m "Release v0.2.0"
   git push origin "v0.2.0"
   ```

3. **Publish locally:**
   ```powershell
   dotnet publish src/ProxyForwarder.App/ProxyForwarder.App.csproj `
     -c Release -r win-x64 --self-contained true `
     /p:Version=0.2.0 `
     -o publish/win-x64
   ```

## GitHub Release

After CI completes or manual tag push:

- [ ] GitHub Release created automatically with tag
- [ ] Release notes auto-generated (from commits)
- [ ] Installer ZIP attached to release
- [ ] Version in release name matches tag (e.g., "Proxy Forwarder v0.2.0")

## Post-Release

- [ ] Verify version displays in app: Title should show `Proxy Forwarder  v0.2.0`
- [ ] Download installer from GitHub Release and test on clean system (if possible)
- [ ] Announce release (if applicable)
- [ ] Update any documentation that references the version

## Rollback (if issues found)

If a release needs to be recalled:

1. **Delete the tag:**
   ```bash
   git tag -d v0.2.0
   git push origin --delete v0.2.0
   ```

2. **Delete the GitHub Release:**
   - GitHub repo → Releases → Delete release

3. **Make fixes and re-release** with same or new version

## Notes

- Versions follow semantic versioning: `MAJOR.MINOR.PATCH`
- Current version: read from git tag with `v` prefix
- First release (no prior tags) will be `v0.1.1` (auto-bump from default `0.1.0`)
- No manual version bumping needed; CI handles it automatically
