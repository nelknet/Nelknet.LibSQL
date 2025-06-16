# Release Checklist

Use this checklist before publishing a new version of Nelknet.LibSQL to NuGet.org.

## Pre-Release Checklist

### Code Quality
- [ ] All tests pass locally on Windows, Linux, and macOS
- [ ] No compiler warnings in Release build
- [ ] XML documentation is complete (no missing `<param>` or `<returns>` tags)
- [ ] Code follows project style guidelines
- [ ] No hardcoded paths or environment-specific code

### Version Management
- [ ] Version number updated in `Directory.Build.props`
- [ ] Version follows Semantic Versioning guidelines
- [ ] Pre-release suffix removed for stable releases

### Documentation
- [ ] README.md is up to date with latest features
- [ ] CHANGELOG.md updated with all changes for this version
- [ ] Migration guide added for breaking changes (if any)
- [ ] API documentation generated and reviewed
- [ ] Example code tested and working

### Package Configuration
- [ ] Package metadata in Directory.Build.props is accurate
- [ ] Package icon (icon.png) is included
- [ ] License file is included
- [ ] Package tags are relevant and complete
- [ ] Dependencies are minimal and necessary

### Testing
- [ ] All 328+ tests pass on all platforms
- [ ] Integration tests pass on all platforms
- [ ] Embedded replica functionality tested
- [ ] Package installs correctly from local feed
- [ ] Sample application works with new package
- [ ] No regression in existing functionality
- [ ] Performance benchmarks show no degradation

### Security
- [ ] No security vulnerabilities in dependencies (`dotnet list package --vulnerable`)
- [ ] No hardcoded credentials or secrets
- [ ] No paths exposing system information
- [ ] SBOM (Software Bill of Materials) generated

### Native Libraries
- [ ] libSQL native libraries included for all platforms
- [ ] Native library loading tested on each platform
- [ ] Library versions documented

## Release Process

### 1. Final Build
```bash
# Clean everything
git clean -xfd
dotnet clean

# Full rebuild
dotnet build --configuration Release

# Run all tests
dotnet test --configuration Release --logger "console;verbosity=detailed"
```

### 2. Create Packages
```bash
# Create NuGet packages
dotnet pack --configuration Release --output ./artifacts

# List created packages
ls -la ./artifacts/*.nupkg
ls -la ./artifacts/*.snupkg
```

### 3. Validate Packages
```bash
# Validate package structure
meziantou.validate-nuget-package ./artifacts/Nelknet.LibSQL.Data.*.nupkg
meziantou.validate-nuget-package ./artifacts/Nelknet.LibSQL.Bindings.*.nupkg

# Check package contents
dotnet nuget locals all --clear
mkdir package-test
cd package-test
dotnet new console
dotnet add package Nelknet.LibSQL.Data --version <VERSION> --source ../artifacts
```

### 4. Git Operations
```bash
# Ensure working directory is clean
git status

# Create release commit
git add -A
git commit -m "Release v<VERSION>"

# Create tag
git tag -a v<VERSION> -m "Release v<VERSION>"

# Push to repository
git push origin main
git push origin v<VERSION>
```

### 5. Publish to NuGet
```bash
# Publish to NuGet.org
dotnet nuget push ./artifacts/Nelknet.LibSQL.Bindings.<VERSION>.nupkg \
  --api-key <API_KEY> \
  --source https://api.nuget.org/v3/index.json

dotnet nuget push ./artifacts/Nelknet.LibSQL.Data.<VERSION>.nupkg \
  --api-key <API_KEY> \
  --source https://api.nuget.org/v3/index.json
```

## Post-Release Checklist

### Verification
- [ ] Packages visible on NuGet.org
- [ ] Package installation works: `dotnet add package Nelknet.LibSQL.Data --version <VERSION>`
- [ ] README displays correctly on NuGet.org
- [ ] Package icon shows correctly
- [ ] Symbol packages available on symbols.nuget.org

### Communication
- [ ] GitHub release created with release notes
- [ ] Tweet/blog about release (if significant)
- [ ] Update project website/documentation (if applicable)
- [ ] Notify major users of breaking changes (if any)

### Maintenance
- [ ] Create milestone for next version
- [ ] Move incomplete issues to next milestone
- [ ] Update project boards
- [ ] Plan next release features

### Monitoring
- [ ] Monitor NuGet download statistics
- [ ] Watch for issues reported by users
- [ ] Check for security advisories
- [ ] Review package ratings and feedback

## Rollback Procedure

If critical issues are found post-release:

1. **Deprecate Package on NuGet.org**
   - Mark the problematic version as deprecated
   - Point users to previous stable version

2. **Create Hotfix**
   ```bash
   # Create hotfix branch
   git checkout -b hotfix/<VERSION> v<VERSION>
   
   # Fix issues
   # Update version to <VERSION>.1
   # Test thoroughly
   
   # Release hotfix
   git tag v<VERSION>.1
   git push origin v<VERSION>.1
   ```

3. **Communicate**
   - Update GitHub release notes with known issues
   - Create GitHub issue for tracking
   - Notify users through available channels

## Version History

Track all releases here:

| Version | Date | Type | Notes |
|---------|------|------|-------|
| 0.1.0-alpha | TBD | Pre-release | Initial alpha release with embedded replica support |
| | | | |