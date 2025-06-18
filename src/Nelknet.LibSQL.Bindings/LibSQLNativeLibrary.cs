using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Nelknet.LibSQL.Bindings;

/// <summary>
/// Handles platform-specific native library loading for libSQL.
/// </summary>
internal static class LibSQLNativeLibrary
{
    /// <summary>
    /// The name of the libSQL native library.
    /// </summary>
    internal const string LibraryName = "libsql";

    private static bool _isInitialized;
    private static readonly object _lock = new();

    /// <summary>
    /// Ensures the native library is loaded and available.
    /// </summary>
    /// <returns>True if the library was successfully loaded or was already loaded.</returns>
    internal static bool TryInitialize()
    {
        if (_isInitialized)
            return true;

        lock (_lock)
        {
            if (_isInitialized)
                return true;

            try
            {
                var rid = GetRuntimeIdentifier();
                if (rid == null)
                    return false;

                var assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                if (assemblyDirectory == null)
                    return false;

                // Try multiple locations in order of preference
                var searchPaths = new[]
                {
                    // 1. Runtime-specific path (NuGet convention)
                    Path.Combine(assemblyDirectory, "runtimes", rid, "native"),
                    // 2. Direct runtime path (for local development)
                    Path.Combine(assemblyDirectory, rid),
                    // 3. Main assembly directory
                    assemblyDirectory,
                    // 4. Parent directory (for bin/Debug/net8.0 scenarios)
                    Path.GetDirectoryName(assemblyDirectory) ?? assemblyDirectory,
                    // 5. Application base directory
                    AppDomain.CurrentDomain.BaseDirectory,
                    // 6. Current directory
                    Environment.CurrentDirectory
                };

                foreach (var path in searchPaths)
                {
                    if (TryLoadFromDirectory(path))
                    {
                        _isInitialized = true;
                        return true;
                    }
                }

                // Last resort: try system-wide loading
                if (TryLoadSystemWide())
                {
                    _isInitialized = true;
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Gets the runtime identifier for the current platform.
    /// </summary>
    /// <returns>The runtime identifier string, or null if unsupported.</returns>
    private static string? GetRuntimeIdentifier()
    {
        if (OperatingSystem.IsWindows())
        {
            return RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X64 => "win-x64",
                Architecture.X86 => "win-x86",
                Architecture.Arm64 => "win-arm64",
                _ => null
            };
        }

        if (OperatingSystem.IsLinux())
        {
            return RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X64 => "linux-x64",
                Architecture.Arm64 => "linux-arm64",
                Architecture.Arm => "linux-arm",
                _ => null
            };
        }

        if (OperatingSystem.IsMacOS())
        {
            return RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X64 => "osx-x64",
                Architecture.Arm64 => "osx-arm64",
                _ => "osx" // Fallback for universal binaries
            };
        }

        return null;
    }

    /// <summary>
    /// Attempts to load the native library from the specified directory.
    /// </summary>
    /// <param name="directory">The directory to search for the library.</param>
    /// <returns>True if the library was successfully loaded.</returns>
    private static bool TryLoadFromDirectory(string directory)
    {
        if (!Directory.Exists(directory))
            return false;

        var libraryNames = GetPlatformSpecificLibraryNames();
        
        foreach (var libraryName in libraryNames)
        {
            var libraryPath = Path.Combine(directory, libraryName);
            if (File.Exists(libraryPath))
            {
                try
                {
                    if (NativeLibrary.TryLoad(libraryPath, out _))
                        return true;
                }
                catch
                {
                    // Continue trying other names
                }
            }
        }

        // Try loading by name without full path
        foreach (var libraryName in libraryNames)
        {
            try
            {
                if (NativeLibrary.TryLoad(
                    libraryName, 
                    Assembly.GetExecutingAssembly(), 
                    DllImportSearchPath.SafeDirectories, 
                    out _))
                {
                    return true;
                }
            }
            catch
            {
                // Continue trying other names
            }
        }

        return false;
    }

    /// <summary>
    /// Gets the platform-specific library names to try loading.
    /// </summary>
    /// <returns>An array of library names to attempt.</returns>
    private static string[] GetPlatformSpecificLibraryNames()
    {
        if (OperatingSystem.IsWindows())
        {
            return new[] { "libsql.dll", "sqlite3.dll" };
        }

        if (OperatingSystem.IsMacOS())
        {
            return new[] { "libsql.dylib", "libsqlite3.dylib", "sqlite3.dylib" };
        }

        // Linux and other Unix-like systems
        return new[] { "libsql.so", "libsqlite3.so", "sqlite3.so" };
    }

    /// <summary>
    /// Attempts to load the native library from system-wide locations.
    /// </summary>
    /// <returns>True if the library was successfully loaded.</returns>
    private static bool TryLoadSystemWide()
    {
        var libraryNames = GetPlatformSpecificLibraryNames();
        
        // Try loading each library name without a specific path
        // This allows the system loader to search standard paths
        foreach (var libraryName in libraryNames)
        {
            try
            {
                if (NativeLibrary.TryLoad(libraryName, out _))
                    return true;
            }
            catch
            {
                // Continue trying other names
            }
        }

        return false;
    }
}