# Platform-Specific Notes

This document covers platform-specific considerations when using Nelknet.LibSQL on different operating systems.

## Supported Platforms

Nelknet.LibSQL supports the following platforms:
- Windows (x64, x86, ARM64)
- Linux (x64, ARM64, ARM)
- macOS (x64, ARM64)

## Windows

### Native Library Loading

On Windows, the native libSQL library (`libsql.dll`) is loaded from:
1. The application directory
2. System PATH locations
3. NuGet package runtime folder

### File Paths

Windows supports both forward slashes and backslashes in file paths:

```csharp
// Both are valid
var conn1 = new LibSQLConnection(@"Data Source=C:\data\app.db");
var conn2 = new LibSQLConnection("Data Source=C:/data/app.db");
```

### Performance Considerations

1. **Antivirus Software**: Exclude database files from real-time scanning
2. **File System**: NTFS is recommended for production use
3. **Windows Defender**: May impact performance during heavy I/O

### Common Issues

**Issue**: "Unable to load DLL 'libsql'"
```csharp
// Solution: Install Visual C++ Redistributables
// Or explicitly set library path:
LibSQLNative.LibraryPath = @"C:\path\to\libsql.dll";
```

## Linux

### Native Library Loading

On Linux, the native library (`libsql.so`) is loaded from:
1. The application directory
2. LD_LIBRARY_PATH locations
3. Standard system locations (/usr/lib, /usr/local/lib)
4. NuGet package runtime folder

### Dependencies

Ensure required dependencies are installed:

```bash
# Debian/Ubuntu
sudo apt-get install libc6

# RHEL/CentOS/Fedora
sudo yum install glibc

# Alpine Linux
apk add libc6-compat
```

### File Permissions

Ensure proper permissions for database files:

```bash
# Set appropriate permissions
chmod 644 mydatabase.db

# For directories
chmod 755 /path/to/database/directory
```

### Performance Considerations

1. **File System**: ext4, XFS recommended; avoid network file systems
2. **I/O Scheduler**: Consider `deadline` or `noop` for SSDs
3. **File Descriptors**: Increase limits for many connections

```bash
# Increase file descriptor limit
ulimit -n 4096

# Make permanent in /etc/security/limits.conf
* soft nofile 4096
* hard nofile 8192
```

### Docker Considerations

When running in Docker containers:

```dockerfile
FROM mcr.microsoft.com/dotnet/runtime:8.0

# Install required dependencies
RUN apt-get update && apt-get install -y \
    libc6 \
    && rm -rf /var/lib/apt/lists/*

# Copy application
COPY app /app
WORKDIR /app

# Ensure library is accessible
ENV LD_LIBRARY_PATH=/app:$LD_LIBRARY_PATH

ENTRYPOINT ["dotnet", "MyApp.dll"]
```

### Common Issues

**Issue**: "Unable to load shared library 'libsql'"
```bash
# Check library dependencies
ldd libsql.so

# Set library path if needed
export LD_LIBRARY_PATH=/path/to/lib:$LD_LIBRARY_PATH
```

## macOS

### Native Library Loading

On macOS, the native library (`libsql.dylib`) is loaded from:
1. The application directory
2. DYLD_LIBRARY_PATH locations
3. Standard system locations
4. NuGet package runtime folder

### Apple Silicon (M1/M2) Support

Nelknet.LibSQL includes native ARM64 support for Apple Silicon:

```csharp
// Automatically uses correct architecture
var connection = new LibSQLConnection("Data Source=app.db");
```

### Security and Permissions

macOS may require additional permissions:

1. **Gatekeeper**: May block unsigned libraries
```bash
# If blocked, allow in System Preferences or:
xattr -d com.apple.quarantine libsql.dylib
```

2. **App Sandbox**: Ensure database access permissions in entitlements

### Performance Considerations

1. **File System**: APFS is optimized for SSDs
2. **Memory Pressure**: Monitor with Activity Monitor
3. **File Descriptors**: Default limits are usually sufficient

```bash
# Check current limit
ulimit -n

# Increase if needed
ulimit -n 2048
```

### Common Issues

**Issue**: "Library not loaded: @rpath/libsql.dylib"
```bash
# Check library dependencies
otool -L libsql.dylib

# Fix rpath if needed
install_name_tool -add_rpath @loader_path libsql.dylib
```

## Cross-Platform Development

### Conditional Compilation

Use platform detection when needed:

```csharp
if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    // Windows-specific code
}
else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
{
    // Linux-specific code
}
else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
{
    // macOS-specific code
}
```

### Path Handling

Use `Path.Combine` for cross-platform paths:

```csharp
// Good - works on all platforms
var dbPath = Path.Combine(dataDirectory, "app.db");

// Avoid - platform-specific
var dbPath = dataDirectory + "\\app.db"; // Windows only
```

### Environment Variables

Access cross-platform environment variables:

```csharp
// Works on all platforms
var home = Environment.GetEnvironmentVariable("HOME") ??
           Environment.GetEnvironmentVariable("USERPROFILE");

var tempPath = Path.GetTempPath(); // Platform-specific temp directory
```

## Architecture-Specific Notes

### x86 vs x64

- **Memory Limits**: x86 limited to 4GB address space
- **Performance**: x64 generally faster for large datasets
- **Compatibility**: Ensure matching architecture for all dependencies

### ARM Support

- **ARM64**: Full support on Windows, Linux, and macOS
- **ARM32**: Supported on Linux (Raspberry Pi, etc.)
- **Performance**: Native ARM builds recommended over emulation

## Deployment Considerations

### Self-Contained Deployments

Include platform-specific runtime:

```bash
# Windows x64
dotnet publish -c Release -r win-x64 --self-contained

# Linux x64
dotnet publish -c Release -r linux-x64 --self-contained

# macOS ARM64
dotnet publish -c Release -r osx-arm64 --self-contained
```

### Runtime Identifiers (RIDs)

Common RIDs for deployment:
- `win-x64`, `win-x86`, `win-arm64`
- `linux-x64`, `linux-arm64`, `linux-arm`
- `osx-x64`, `osx-arm64`

### NuGet Package Structure

The package includes native libraries for all platforms:

```
runtimes/
├── win-x64/native/libsql.dll
├── win-x86/native/libsql.dll
├── win-arm64/native/libsql.dll
├── linux-x64/native/libsql.so
├── linux-arm64/native/libsql.so
├── linux-arm/native/libsql.so
├── osx-x64/native/libsql.dylib
└── osx-arm64/native/libsql.dylib
```

## Troubleshooting

### Debug Native Loading

Enable detailed logging:

```csharp
// Enable native library loading diagnostics
Environment.SetEnvironmentVariable("COREHOST_TRACE", "1");
Environment.SetEnvironmentVariable("COREHOST_TRACE_VERBOSITY", "4");
```

### Platform Detection

Check runtime information:

```csharp
Console.WriteLine($"OS: {RuntimeInformation.OSDescription}");
Console.WriteLine($"Architecture: {RuntimeInformation.ProcessArchitecture}");
Console.WriteLine($"Framework: {RuntimeInformation.FrameworkDescription}");
```

### Library Resolution

Manually specify library location if needed:

```csharp
// Platform-specific library loading
if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    LibSQLNative.LibraryPath = @"C:\libs\libsql.dll";
}
else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
{
    LibSQLNative.LibraryPath = "/usr/local/lib/libsql.so";
}
else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
{
    LibSQLNative.LibraryPath = "/usr/local/lib/libsql.dylib";
}
```