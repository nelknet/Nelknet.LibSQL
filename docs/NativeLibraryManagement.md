# Native Library Management

This document describes how Nelknet.LibSQL manages native libSQL libraries across different platforms.

## Package Types

Nelknet.LibSQL is available in two package variants:

### 1. Managed Only Package (`Nelknet.LibSQL.Data`)
- Contains only the managed .NET assemblies
- Smaller package size
- Requires you to provide the native libSQL library separately
- Ideal when you want to manage native dependencies yourself

### 2. Full Package (`Nelknet.LibSQL.Data.Full`)
- Includes native libSQL libraries for all supported platforms
- Larger package size but works out-of-the-box
- No additional setup required
- Recommended for most users

## Supported Platforms

| Platform | Architecture | Runtime ID | Library Name |
|----------|-------------|------------|--------------|
| Windows | x64 | win-x64 | libsql.dll |
| Windows | x86 | win-x86 | libsql.dll |
| Windows | ARM64 | win-arm64 | libsql.dll |
| Linux | x64 | linux-x64 | liblibsql.so |
| Linux | ARM64 | linux-arm64 | liblibsql.so |
| Linux | ARM | linux-arm | liblibsql.so |
| macOS | x64 | osx-x64 | liblibsql.dylib |
| macOS | ARM64 (Apple Silicon) | osx-arm64 | liblibsql.dylib |

## Library Loading Process

The library loader searches for native libraries in the following order:

1. **Runtime-specific NuGet path**: `runtimes/{rid}/native/` (standard NuGet convention)
2. **Direct runtime path**: `{rid}/` (for local development)
3. **Assembly directory**: The directory containing the Nelknet.LibSQL assemblies
4. **Parent directory**: Parent of the assembly directory (for bin/Debug scenarios)
5. **Application base directory**: `AppDomain.CurrentDomain.BaseDirectory`
6. **Current directory**: `Environment.CurrentDirectory`
7. **System-wide**: Standard system library paths (PATH on Windows, LD_LIBRARY_PATH on Linux, etc.)

## Building from Source

### Building Managed-Only Package
```bash
# Windows PowerShell
./build.ps1 -BuildType ManagedOnly

# Linux/macOS
./build.sh --build-type ManagedOnly
```

### Building Full Package with Native Libraries
```bash
# Windows PowerShell
./build.ps1 -BuildType Full

# Linux/macOS
./build.sh --build-type Full
```

### Building Without ARM Support
```bash
# Windows PowerShell
./build.ps1 -BuildType Full -SkipArm

# Linux/macOS
./build.sh --build-type Full --skip-arm
```

## Creating NuGet Packages

### Create Both Package Types
```bash
# Windows PowerShell
./pack.ps1 -PackageType Both

# Linux/macOS
./pack.sh --package-type Both
```

### Create Specific Package Type
```bash
# Windows PowerShell
./pack.ps1 -PackageType Full -OutputDirectory ./packages

# Linux/macOS
./pack.sh --package-type Full --output ./packages
```

## Version Information

You can check the loaded library versions at runtime:

```csharp
using Nelknet.LibSQL.Data;

// Check if library is loaded
if (LibSQLVersion.IsLibraryLoaded())
{
    Console.WriteLine($"libSQL Version: {LibSQLVersion.LibSQLVersionString}");
    Console.WriteLine($"SQLite Version: {LibSQLVersion.SQLiteVersionString}");
    Console.WriteLine($"SQLite Version Number: {LibSQLVersion.SQLiteVersionNumber}");
    Console.WriteLine($"SQLite Source ID: {LibSQLVersion.SQLiteSourceId}");
    
    // Or get all info at once
    Console.WriteLine(LibSQLVersion.GetVersionInfo());
}
```

## Building libSQL from Source

Currently, pre-built libSQL binaries are not available from the official releases. To use Nelknet.LibSQL, you'll need to:

1. **Build libSQL from source**:
   ```bash
   git clone https://github.com/tursodatabase/libsql
   cd libsql
   cargo xtask build
   ```
   The compiled libraries will be in `libsql-sqlite3/.libs/`

2. **Use SQLite3 for testing** (since libSQL is SQLite3-compatible):
   ```bash
   # A helper script is provided to download and build SQLite3
   ./scripts/download-sqlite3.sh
   ```
   This will build SQLite3 libraries that can be used for testing basic functionality

3. **Place the library in your application directory**:
   - Windows: `libsql.dll` or `sqlite3.dll`
   - Linux: `libsql.so` or `libsqlite3.so`
   - macOS: `libsql.dylib` or `libsqlite3.dylib`

## Troubleshooting

### Library Not Found

If you encounter "Failed to load libSQL native library" errors:

1. **Using Managed-Only Package**: Ensure you have the native library installed:
   - Place the appropriate library file in your application's output directory
   - Or install libSQL system-wide
   - Or switch to the Full package

2. **Using Full Package**: Verify the package was restored correctly:
   - Check that the `runtimes` folder exists in your output directory
   - Ensure the correct platform-specific library is present

3. **Custom Deployment**: You can place the native library in any of the search paths listed above

### Wrong Architecture

Ensure your application's target architecture matches the native library:
- Use `AnyCPU` with `Prefer32Bit=false` for maximum compatibility
- Or explicitly target the same architecture as your native library

### Version Mismatch

Use `LibSQLVersion.GetVersionInfo()` to verify the loaded library version matches your expectations.

## Custom Library Loading

If you need to load the library from a custom location, ensure it's in one of the search paths before the first libSQL call, or place it in your application's directory.

```csharp
// Example: Copy library to application directory at startup
var libraryName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "libsql.dll" :
                  RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "liblibsql.so" :
                  "liblibsql.dylib";

var sourcePath = Path.Combine("custom", "location", libraryName);
var destPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, libraryName);
File.Copy(sourcePath, destPath, overwrite: true);
```