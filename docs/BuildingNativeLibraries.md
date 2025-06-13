# Building Native Libraries

This document describes how to build the native libSQL libraries for Nelknet.LibSQL.

## Overview

Nelknet.LibSQL uses native libSQL libraries that need to be built from the libSQL C bindings. Unlike the Go driver which uses static libraries, we need dynamic libraries (.dll, .so, .dylib) for P/Invoke.

## Quick Start

To build everything (native libraries, managed assemblies, and NuGet packages):

```bash
./build-all.sh --all
```

Or on Windows:

```powershell
# First build native libraries
.\scripts\build-native-libs.ps1

# Then build and pack
dotnet build -c Release
dotnet pack -c Release -p:BuildType=Full
```

## Building Native Libraries

### Prerequisites

- Rust toolchain (install from https://rustup.rs)
- Git
- Platform-specific build tools:
  - **Linux**: gcc, make
  - **macOS**: Xcode Command Line Tools
  - **Windows**: Visual Studio 2022 or MinGW

### Manual Build

#### Linux/macOS

```bash
./scripts/build-native-libs.sh [libsql-tag]

# Example:
./scripts/build-native-libs.sh libsql-0.6.2
```

#### Windows

```powershell
.\scripts\build-native-libs.ps1 -LibSQLTag "libsql-0.6.2"
```

### What Gets Built

The build scripts:

1. Clone the libSQL repository at the specified tag
2. Build the C bindings using Cargo
3. Convert the static library to a dynamic library
4. Place the libraries in the correct NuGet structure:

```
src/Nelknet.LibSQL.Bindings/runtimes/
├── win-x64/native/libsql.dll
├── win-x86/native/libsql.dll
├── win-arm64/native/libsql.dll
├── linux-x64/native/libsql.so
├── linux-arm64/native/libsql.so
├── osx-x64/native/libsql.dylib
└── osx-arm64/native/libsql.dylib
```

## GitHub Actions Workflow

For automated builds, we provide a GitHub Actions workflow that builds native libraries for all platforms:

```yaml
# Manually trigger the workflow
gh workflow run build-native-libs.yml -f libsql_tag=libsql-0.6.2
```

This workflow:
- Builds for all supported platforms using matrix builds
- Creates dynamic libraries from the static libSQL binaries
- Uploads artifacts that can be downloaded
- Can create a PR with updated libraries

## Cross-Compilation

### Linux

For cross-compilation on Linux, install `cross`:

```bash
cargo install cross
```

The build script will automatically use `cross` when available.

### Windows

Cross-compilation on Windows requires the appropriate toolchains:
- For ARM64: Visual Studio with ARM64 tools
- For x86: Visual Studio with x86 tools

## Updating libSQL Version

To update to a new libSQL version:

1. **Check the libSQL releases**: https://github.com/tursodatabase/libsql/releases
2. **Run the build script with the new tag**:
   ```bash
   ./build-all.sh --native --libsql-version libsql-0.7.0
   ```
3. **Test the new libraries**:
   ```bash
   ./build-all.sh --managed --skip-tests
   dotnet test
   ```
4. **Create packages**:
   ```bash
   ./build-all.sh --pack
   ```

## Troubleshooting

### Missing Dependencies

If the build fails due to missing dependencies:

**Linux**:
```bash
sudo apt-get install build-essential pkg-config libssl-dev
```

**macOS**:
```bash
xcode-select --install
```

**Windows**:
- Install Visual Studio 2022 with C++ workload
- Or install MinGW-w64

### Static vs Dynamic Libraries

libSQL C bindings produce static libraries (`.a` files). We convert these to dynamic libraries because:
- .NET P/Invoke requires dynamic libraries
- NuGet has better support for dynamic library distribution
- Easier to update without recompiling the entire application

### Platform-Specific Issues

**macOS Security**:
The built libraries need to link against Security and CoreFoundation frameworks.

**Linux GLIBC**:
Libraries built on newer Linux distributions might not work on older ones due to GLIBC version requirements.

**Windows CRT**:
Ensure the target system has the Visual C++ Redistributables installed.

## Distribution

After building, the libraries can be distributed via:

1. **NuGet Package**: Include in the `Nelknet.LibSQL.Data.Full` package
2. **GitHub Releases**: Upload as release assets
3. **Direct Download**: Users can download and place in their application directory

## Version Compatibility

| Nelknet.LibSQL Version | libSQL Version | Notes |
|------------------------|----------------|-------|
| 0.1.0                  | libsql-0.6.2   | Initial release |

Always test with the specific libSQL version before releasing.