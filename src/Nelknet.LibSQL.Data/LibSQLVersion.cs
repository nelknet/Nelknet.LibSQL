using System;
using System.Runtime.InteropServices;
using Nelknet.LibSQL.Bindings;

namespace Nelknet.LibSQL.Data;

/// <summary>
/// Provides version information about the libSQL library.
/// </summary>
public static class LibSQLVersion
{
    private static string? _libSQLVersion;
    private static string? _sqliteVersion;
    private static int? _sqliteVersionNumber;
    private static string? _sqliteSourceId;
    private static readonly object _lock = new();

    /// <summary>
    /// Gets the version of the libSQL library.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the native library is not loaded.</exception>
    public static string LibSQLVersionString
    {
        get
        {
            if (_libSQLVersion != null)
                return _libSQLVersion;

            lock (_lock)
            {
                if (_libSQLVersion != null)
                    return _libSQLVersion;

                EnsureNativeLibraryLoaded();
                
                try
                {
                    var ptr = LibSQLNative.libsql_libversion();
                    if (ptr == IntPtr.Zero)
                        throw new InvalidOperationException("Failed to get libSQL version");

                    _libSQLVersion = Marshal.PtrToStringAnsi(ptr) ?? "Unknown";
                }
                catch (EntryPointNotFoundException)
                {
                    // Fallback: If using SQLite3 instead of libSQL, return SQLite version
                    _libSQLVersion = $"SQLite {SQLiteVersionString} (libSQL-compatible)";
                }
                
                return _libSQLVersion;
            }
        }
    }

    /// <summary>
    /// Gets the version of the underlying SQLite library.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the native library is not loaded.</exception>
    public static string SQLiteVersionString
    {
        get
        {
            if (_sqliteVersion != null)
                return _sqliteVersion;

            lock (_lock)
            {
                if (_sqliteVersion != null)
                    return _sqliteVersion;

                EnsureNativeLibraryLoaded();
                
                var ptr = LibSQLNative.sqlite3_libversion();
                if (ptr == IntPtr.Zero)
                    throw new InvalidOperationException("Failed to get SQLite version");

                _sqliteVersion = Marshal.PtrToStringAnsi(ptr) ?? "Unknown";
                return _sqliteVersion;
            }
        }
    }

    /// <summary>
    /// Gets the version number of the underlying SQLite library.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the native library is not loaded.</exception>
    public static int SQLiteVersionNumber
    {
        get
        {
            if (_sqliteVersionNumber.HasValue)
                return _sqliteVersionNumber.Value;

            lock (_lock)
            {
                if (_sqliteVersionNumber.HasValue)
                    return _sqliteVersionNumber.Value;

                EnsureNativeLibraryLoaded();
                
                _sqliteVersionNumber = LibSQLNative.sqlite3_libversion_number();
                return _sqliteVersionNumber.Value;
            }
        }
    }

    /// <summary>
    /// Gets the source identifier of the underlying SQLite library.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the native library is not loaded.</exception>
    public static string SQLiteSourceId
    {
        get
        {
            if (_sqliteSourceId != null)
                return _sqliteSourceId;

            lock (_lock)
            {
                if (_sqliteSourceId != null)
                    return _sqliteSourceId;

                EnsureNativeLibraryLoaded();
                
                var ptr = LibSQLNative.sqlite3_sourceid();
                if (ptr == IntPtr.Zero)
                    throw new InvalidOperationException("Failed to get SQLite source ID");

                _sqliteSourceId = Marshal.PtrToStringAnsi(ptr) ?? "Unknown";
                return _sqliteSourceId;
            }
        }
    }

    /// <summary>
    /// Checks if the native library is loaded and compatible.
    /// </summary>
    /// <returns>True if the library is loaded and versions can be retrieved; otherwise, false.</returns>
    public static bool IsLibraryLoaded()
    {
        try
        {
            LibSQLNative.Initialize();
            
            // Try to get SQLite version to verify the library is working
            var ptr = LibSQLNative.sqlite3_libversion();
            return ptr != IntPtr.Zero;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets detailed version information as a formatted string.
    /// </summary>
    /// <returns>A string containing all version information.</returns>
    public static string GetVersionInfo()
    {
        try
        {
            return $"libSQL Version: {LibSQLVersionString}\n" +
                   $"SQLite Version: {SQLiteVersionString} ({SQLiteVersionNumber})\n" +
                   $"SQLite Source ID: {SQLiteSourceId}";
        }
        catch (Exception ex)
        {
            return $"Failed to retrieve version information: {ex.Message}";
        }
    }

    private static void EnsureNativeLibraryLoaded()
    {
        LibSQLNative.Initialize();
    }
}