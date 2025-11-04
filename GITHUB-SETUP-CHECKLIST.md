# GitHub Setup Checklist

Complete these steps to enable the automatic versioning workflow.

## ✓ Step 1: Repository Settings

1. Go to your GitHub repository
2. Click **Settings** (top right)
3. Go to **Actions** → **General**
4. Find **Workflow permissions** section
5. Select **"Read and write permissions"**
6. ✓ Check: **"Allow GitHub Actions to create and approve pull requests"** (if available)
7. Click **Save**

## ✓ Step 2: Verify Files Are Committed

Ensure these files are in your repository:

```
✓ .github/workflows/release-tag.yml
✓ Directory.Build.props (modified)
✓ src/ProxyForwarder.App/MainWindow.xaml.cs (modified)
✓ VERSIONING.md
✓ VERSIONING-QUICK-REF.txt
✓ RELEASE-CHECKLIST.md
✓ VERSIONING-SETUP-SUMMARY.txt
✓ ci-auto-tag-release.patch
```

Verify with git:
```bash
git status
git log --oneline | head -5
```

All files should be committed and pushed to `main` branch.

## ✓ Step 3: Trigger First Release (Method A)

**Option A: Via commit message**

```bash
# Make a test change
echo "# Test commit" >> README.md

# Commit with [release] keyword
git add .
git commit -m "test: first release trigger [release]"
git push origin main
```

**Option B: Via GitHub Actions UI**

1. Go to GitHub repo → **Actions** tab
2. Find **"Release & Tag"** workflow
3. Click **"Run workflow"**
4. Leave **version** blank (will auto-bump)
5. Click **"Run workflow"**

## ✓ Step 4: Watch the Workflow

1. Go to **Actions** tab
2. Select **"Release & Tag"** workflow run
3. Watch the build progress
4. Expected steps:
   - ✓ Checkout
   - ✓ Determine version
   - ✓ Setup .NET
   - ✓ Publish (win-x64, self-contained)
   - ✓ Create GitHub Release
   - ✓ Done

## ✓ Step 5: Verify Release Created

1. Go to **Releases** tab
2. Should see new release (e.g., "Proxy Forwarder v0.1.1")
3. Check for attached file: `ProxyForwarder-win-x64-v0.1.1.zip`
4. Check tag: should match version (e.g., `v0.1.1`)

## ✓ Step 6: Verify Git Tag

```bash
# Fetch latest tags
git fetch --tags

# List tags
git tag -l --sort=-version:refname

# Should show: v0.1.1 (or higher)
```

## ✓ Step 7: Test App Version Display

1. Download the installer ZIP from the Release
2. Extract it on another machine (or same machine)
3. Run `INSTALL.bat` as Administrator
4. Launch the app
5. Check title bar shows: **"Proxy Forwarder  v0.1.1"**

## Troubleshooting

### Workflow didn't trigger

**Problem:** Pushed commit but workflow didn't run

**Solution:**
- Check: Does commit message contain `[release]`?
- Check: Was it pushed to `main` or `master` branch?
- Check: GitHub Settings → Actions → Workflow permissions = "Read and write"

### Workflow failed

**Problem:** Workflow started but failed with error

**Solution:**
- Click on failed workflow run
- Scroll down to see error logs
- Common issues:
  - `checkout` failed → check branch name
  - `.NET` setup failed → check SDK version requirement
  - `publish` failed → check project path in workflow

### No Release created

**Problem:** Workflow passed but no Release on GitHub

**Solution:**
- Check: Repository settings → Permissions are set correctly
- Check: Workflow has "Create GitHub Release" step passing
- Try: Manual version input on workflow_dispatch

### Version not showing in app

**Problem:** App launches but title doesn't show version

**Solution:**
- Rebuild: `dotnet build src/ProxyForwarder.sln -c Release`
- Check: `src/ProxyForwarder.App/MainWindow.xaml.cs` has `DisplayVersion()` method
- Check: `Directory.Build.props` has version properties

## Next Release (After First)

### Quick way (auto-bump PATCH):

```bash
git commit -m "feat: your change [release]"
git push origin main
```

### Specific version (via GitHub UI):

1. GitHub repo → Actions → Release & Tag
2. Run workflow
3. Enter version: `0.2.0`
4. Run workflow

## Customization

### Change branch names:

Edit `.github/workflows/release-tag.yml`:
```yaml
branches: [ main, develop ]  # Add/remove branches
```

### Change version numbering:

Edit `.github/workflows/release-tag.yml`:
```yaml
PA=$((PA+1))  # Change to MI=$((MI+1)) for minor version
```

### Change app display:

Edit `src/ProxyForwarder.App/MainWindow.xaml.cs`:
```csharp
this.Title = $"{this.Title}  v{version}";  // Customize format
```

## Verification Checklist

Run through this before using in production:

- [ ] Repository settings checked (Actions → Permissions)
- [ ] All files committed and pushed
- [ ] First release triggered successfully
- [ ] GitHub Release created with tag
- [ ] Installer ZIP available in Release
- [ ] App title shows version number
- [ ] Local `git tag -l` shows new tag
- [ ] Installer runs on test system
- [ ] Documentation files accessible in repo

## Support

**Docs:**
- See `VERSIONING.md` for complete guide
- See `VERSIONING-QUICK-REF.txt` for commands
- See `IMPLEMENTATION_COMPLETE.txt` for overview

**Issues:**
- Check GitHub Actions logs (Actions tab → workflow run → logs)
- Check VERSIONING.md Troubleshooting section
- Review permission settings in repository

## Done!

✓ Versioning system is ready to use
✓ You can now create releases automatically
✓ Each commit with `[release]` triggers a new version
✓ Enjoy automated releases! 🚀
