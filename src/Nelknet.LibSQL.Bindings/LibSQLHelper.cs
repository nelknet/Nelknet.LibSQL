using System;
using System.Runtime.InteropServices;

namespace Nelknet.LibSQL.Bindings;

/// <summary>
/// Helper class for working with libSQL native functions and creating SafeHandle instances
/// </summary>
internal static class LibSQLHelper
{
    /// <summary>
    /// Gets a string from a native pointer and handles freeing it if needed
    /// </summary>
    /// <param name="ptr">Pointer to the string</param>
    /// <param name="shouldFree">Whether the string should be freed after use</param>
    /// <returns>The managed string</returns>
    internal static string? GetStringFromPtr(IntPtr ptr, bool shouldFree = false)
    {
        if (ptr == IntPtr.Zero)
            return null;
            
        try
        {
            return Marshal.PtrToStringUTF8(ptr);
        }
        finally
        {
            if (shouldFree)
            {
                LibSQLNative.libsql_free_string(ptr);
            }
        }
    }
    
    /// <summary>
    /// Creates a LibSQLDatabaseHandle from an IntPtr
    /// </summary>
    /// <param name="handle">The native handle</param>
    /// <returns>A managed SafeHandle wrapper</returns>
    internal static LibSQLDatabaseHandle CreateDatabaseHandle(IntPtr handle)
    {
        return new LibSQLDatabaseHandle(handle);
    }
    
    /// <summary>
    /// Creates a LibSQLConnectionHandle from an IntPtr
    /// </summary>
    /// <param name="handle">The native handle</param>
    /// <returns>A managed SafeHandle wrapper</returns>
    internal static LibSQLConnectionHandle CreateConnectionHandle(IntPtr handle)
    {
        return new LibSQLConnectionHandle(handle);
    }
    
    /// <summary>
    /// Creates a LibSQLStatementHandle from an IntPtr
    /// </summary>
    /// <param name="handle">The native handle</param>
    /// <returns>A managed SafeHandle wrapper</returns>
    internal static LibSQLStatementHandle CreateStatementHandle(IntPtr handle)
    {
        return new LibSQLStatementHandle(handle);
    }
    
    /// <summary>
    /// Creates a LibSQLRowsHandle from an IntPtr
    /// </summary>
    /// <param name="handle">The native handle</param>
    /// <returns>A managed SafeHandle wrapper</returns>
    internal static LibSQLRowsHandle CreateRowsHandle(IntPtr handle)
    {
        return new LibSQLRowsHandle(handle);
    }
    
    /// <summary>
    /// Creates a LibSQLRowHandle from an IntPtr
    /// </summary>
    /// <param name="handle">The native handle</param>
    /// <returns>A managed SafeHandle wrapper</returns>
    internal static LibSQLRowHandle CreateRowHandle(IntPtr handle)
    {
        return new LibSQLRowHandle(handle);
    }
    
    /// <summary>
    /// Checks if a libSQL operation result indicates success
    /// </summary>
    /// <param name="result">The result code from a libSQL operation</param>
    /// <returns>True if the operation was successful</returns>
    internal static bool IsSuccess(int result)
    {
        return result == 0; // In libSQL, 0 typically indicates success
    }
    
    /// <summary>
    /// Gets an error message from a native error message pointer
    /// </summary>
    /// <param name="errorPtr">Pointer to the error message</param>
    /// <returns>The error message string</returns>
    internal static string GetErrorMessage(IntPtr errorPtr)
    {
        if (errorPtr == IntPtr.Zero)
            return "Unknown error";
            
        return Marshal.PtrToStringUTF8(errorPtr) ?? "Unknown error";
    }
    
    /// <summary>
    /// Throws an exception if the libSQL operation failed
    /// </summary>
    /// <param name="result">The result code from a libSQL operation</param>
    /// <param name="errorMessage">The error message if available</param>
    /// <exception cref="InvalidOperationException">Thrown if the operation failed</exception>
    internal static void ThrowIfError(int result, string? errorMessage = null)
    {
        if (!IsSuccess(result))
        {
            throw new InvalidOperationException(errorMessage ?? $"LibSQL operation failed with code: {result}");
        }
    }
}