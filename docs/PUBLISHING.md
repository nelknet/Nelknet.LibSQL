# Publishing to NuGet.org

This document describes the process for publishing Nelknet.LibSQL packages to NuGet.org.

## Prerequisites

1. **NuGet.org Account**
   - Create an account at https://www.nuget.org/
   - Verify your email address

2. **API Key**
   - Go to https://www.nuget.org/account/apikeys
   - Click "Create" to generate a new API key
   - Set the following properties:
     - Key Name: `Nelknet.LibSQL Publishing`
     - Expiration: 365 days (or as needed)
     - Packages: Select "Push new packages and package versions"
     - Glob Pattern: `Nelknet.LibSQL*`
   - Copy the generated API key (you won't be able to see it again)

3. **Repository Secrets**
   - Go to your GitHub repository settings
   - Navigate to Settings > Secrets and variables > Actions
   - Add a new repository secret:
     - Name: `NUGET_API_KEY`
     - Value: Your NuGet.org API key

## Manual Publishing (Local Development)

### 1. Build and Pack

```bash
# Clean previous builds
dotnet clean

# Build in Release mode
dotnet build --configuration Release

# Run tests
dotnet test --configuration Release

# Pack NuGet packages
dotnet pack src/Nelknet.LibSQL.Bindings --configuration Release --output ./artifacts
dotnet pack src/Nelknet.LibSQL.Data --configuration Release --output ./artifacts
```

### 2. Validate Packages

```bash
# Install validation tool
dotnet tool install --global Meziantou.Framework.NuGetPackageValidation.Tool

# Validate packages
meziantou.validate-nuget-package ./artifacts/Nelknet.LibSQL.Bindings.*.nupkg
meziantou.validate-nuget-package ./artifacts/Nelknet.LibSQL.Data.*.nupkg
```

### 3. Test Package Installation

```bash
# Create test project
mkdir test-install && cd test-install
dotnet new console

# Add local package source
dotnet nuget add source ../artifacts --name local-test

# Install package
dotnet add package Nelknet.LibSQL.Data --version <VERSION> --source local-test

# Test basic functionality
# Add test code to Program.cs and run
dotnet run
```

### 4. Push to NuGet.org

```bash
# Push packages (will also push .snupkg symbol packages)
dotnet nuget push ./artifacts/Nelknet.LibSQL.Bindings.<VERSION>.nupkg \
  --api-key <YOUR_API_KEY> \
  --source https://api.nuget.org/v3/index.json

dotnet nuget push ./artifacts/Nelknet.LibSQL.Data.<VERSION>.nupkg \
  --api-key <YOUR_API_KEY> \
  --source https://api.nuget.org/v3/index.json
```

## Automated Publishing (GitHub Actions)

The repository includes automated workflows for publishing:

### Development Releases

Pushes to the `develop` branch automatically publish pre-release packages to GitHub Packages.

### Production Releases

1. **Update Version**
   - Update version in `Directory.Build.props`
   - Update `CHANGELOG.md` with release notes
   - Commit changes

2. **Create Release**
   - Push a tag: `git tag v1.0.0 && git push origin v1.0.0`
   - Or manually trigger the Release workflow with version input

3. **Automated Process**
   - GitHub Actions will:
     - Build and test on multiple platforms
     - Create NuGet packages with symbols
     - Run security and license checks
     - Create a draft GitHub release
     - Publish to NuGet.org (if API key is configured)
     - Publish to GitHub Packages as backup

## Package Verification

After publishing, verify your packages:

1. **NuGet.org**
   - Visit https://www.nuget.org/packages/Nelknet.LibSQL.Data
   - Check package appears correctly
   - Verify README rendering
   - Check package icon
   - Ensure all metadata is correct

2. **Symbol Server**
   - Symbols are automatically published to https://symbols.nuget.org/
   - Enable Source Link in Visual Studio to debug into package code

3. **Installation Test**
   ```bash
   # Create new test project
   dotnet new console -n test-nuget
   cd test-nuget
   
   # Install from NuGet.org
   dotnet add package Nelknet.LibSQL.Data
   
   # Verify installation
   dotnet list package
   ```

## Troubleshooting

### Package Push Failures

- **409 Conflict**: Package version already exists
  - Solution: Increment version number
  
- **401 Unauthorized**: Invalid API key
  - Solution: Regenerate API key and update secret

- **403 Forbidden**: API key lacks permissions
  - Solution: Check API key scope includes your package names

### Symbol Package Issues

- Ensure `<IncludeSymbols>true</IncludeSymbols>` in project file
- Verify .snupkg files are generated alongside .nupkg files
- Symbol packages are pushed automatically with main package

### Package Validation Errors

Run the validation tool locally to identify issues:
```bash
meziantou.validate-nuget-package ./path/to/package.nupkg --verbose
```

Common issues:
- Missing package icon
- Missing README
- Invalid metadata
- Breaking changes without major version bump

## Best Practices

1. **Versioning**
   - Follow Semantic Versioning (MAJOR.MINOR.PATCH)
   - Use pre-release suffixes for non-stable releases (-alpha, -beta, -rc)

2. **Release Notes**
   - Always update CHANGELOG.md before release
   - Include breaking changes prominently
   - List new features and bug fixes

3. **Testing**
   - Test packages locally before publishing
   - Verify on Windows, Linux, and macOS
   - Test both .NET 8 and .NET 9 compatibility

4. **Security**
   - Never commit API keys to source control
   - Rotate API keys periodically
   - Use repository secrets for automation

## Package Deprecation

If you need to deprecate a package version:

1. Log in to NuGet.org
2. Go to package management page
3. Select the version to deprecate
4. Click "Deprecate" and provide:
   - Reason for deprecation
   - Alternate package recommendation
   - Custom message

## Support

For issues with:
- Package publishing: Check GitHub Actions logs
- NuGet.org: Contact https://www.nuget.org/policies/Contact
- Package usage: File issues at https://github.com/[your-org]/Nelknet.LibSQL/issues