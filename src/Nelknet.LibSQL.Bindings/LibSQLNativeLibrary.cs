using System;
using System.Collections.Generic;
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
    private static bool _resolverRegistered;
    private static IntPtr _libraryHandle = IntPtr.Zero;
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
                EnsureResolverRegistered();

                var rid = GetRuntimeIdentifier();
                if (rid == null)
                    return false;

                foreach (var path in EnumerateSearchPaths(rid))
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
    /// Enumerates the directories searched for the native library, in order of preference.
    /// </summary>
    /// <param name="rid">The runtime identifier for the current platform (e.g. <c>win-x64</c>).</param>
    /// <remarks>
    /// <para>
    /// <see cref="AppContext.BaseDirectory"/> is checked first because it is the API that
    /// works reliably in single-file publishes. In those scenarios <see cref="Assembly.Location"/>
    /// returns an empty string for bundled assemblies, so paths derived from it collapse
    /// and must be treated as non-load-bearing. See issue #64.
    /// </para>
    /// <para>Null and empty entries are never yielded.</para>
    /// </remarks>
    internal static IEnumerable<string> EnumerateSearchPaths(string rid)
    {
        ArgumentNullException.ThrowIfNull(rid);

        var baseDirectory = AppContext.BaseDirectory;
        if (!string.IsNullOrEmpty(baseDirectory))
        {
            // NuGet runtimes convention, rooted at the app's base directory.
            yield return Path.Combine(baseDirectory, "runtimes", rid, "native");
            // Direct runtime-named subdirectory (common for local development layouts).
            yield return Path.Combine(baseDirectory, rid);
            // Base directory itself (where single-file publishes place native assets).
            yield return baseDirectory;
        }

        // The IsNullOrEmpty guard below is precisely the documented mitigation for
        // the IL3000 warning: Assembly.Location returns an empty string in single-file
        // bundles, which this code treats as "skip the assembly-directory fallbacks".
        // AppContext.BaseDirectory above already covers single-file publishes; this
        // block only contributes when the assembly lives on disk at a resolvable path.
#pragma warning disable IL3000
        var assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
#pragma warning restore IL3000
        if (!string.IsNullOrEmpty(assemblyDirectory))
        {
            yield return Path.Combine(assemblyDirectory, "runtimes", rid, "native");
            yield return Path.Combine(assemblyDirectory, rid);
            yield return assemblyDirectory;

            var parent = Path.GetDirectoryName(assemblyDirectory);
            if (!string.IsNullOrEmpty(parent))
                yield return parent;
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
        if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
            return false;

        var libraryNames = GetPlatformSpecificLibraryNames();

        foreach (var libraryName in libraryNames)
        {
            var libraryPath = Path.Combine(directory, libraryName);
            if (File.Exists(libraryPath))
            {
                try
                {
                    if (NativeLibrary.TryLoad(libraryPath, out var handle))
                    {
                        _libraryHandle = handle;
                        return true;
                    }
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
                    out var handle))
                {
                    _libraryHandle = handle;
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
                if (NativeLibrary.TryLoad(libraryName, out var handle))
                {
                    _libraryHandle = handle;
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
    /// Registers a <see cref="NativeLibrary.SetDllImportResolver"/> delegate so that
    /// P/Invoke lookups for <c>libsql</c> in this assembly resolve to the handle we
    /// loaded explicitly.
    /// </summary>
    /// <remarks>
    /// On single-file builds the default P/Invoke resolution does not
    /// always locate a module that has already been loaded via
    /// <see cref="NativeLibrary.TryLoad(string, out IntPtr)"/> with an absolute path.
    /// Routing through our own resolver closes that gap without changing call sites.
    /// </remarks>
    private static void EnsureResolverRegistered()
    {
        if (_resolverRegistered)
            return;

        NativeLibrary.SetDllImportResolver(typeof(LibSQLNativeLibrary).Assembly, ResolveLibrary);
        _resolverRegistered = true;
    }

    private static IntPtr ResolveLibrary(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (string.Equals(libraryName, LibraryName, StringComparison.Ordinal) && _libraryHandle != IntPtr.Zero)
            return _libraryHandle;

        // Fall through to the default P/Invoke resolver.
        return IntPtr.Zero;
    }
}
