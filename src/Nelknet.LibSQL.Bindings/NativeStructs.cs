using System;
using System.Runtime.InteropServices;

namespace Nelknet.LibSQL.Native;

/// <summary>
/// LibSQL data type constants
/// </summary>
internal static class LibSQLType
{
    internal const int Int = 1;
    internal const int Float = 2;
    internal const int Text = 3;
    internal const int Blob = 4;
    internal const int Null = 5;
}

/// <summary>
/// libsql_config structure for database configuration
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct LibSQLConfig
{
    /// <summary>
    /// Database file path
    /// </summary>
    public IntPtr DbPath;
    
    /// <summary>
    /// Primary URL for replication
    /// </summary>
    public IntPtr PrimaryUrl;
    
    /// <summary>
    /// Authentication token
    /// </summary>
    public IntPtr AuthToken;
    
    /// <summary>
    /// Read your writes consistency
    /// </summary>
    public byte ReadYourWrites;
    
    /// <summary>
    /// Encryption key
    /// </summary>
    public IntPtr EncryptionKey;
    
    /// <summary>
    /// Sync interval in milliseconds
    /// </summary>
    public int SyncInterval;
    
    /// <summary>
    /// Use WebPKI for TLS verification
    /// </summary>
    public byte WithWebpki;
    
    /// <summary>
    /// Offline mode
    /// </summary>
    public byte Offline;
}

/// <summary>
/// replicated structure for sync operations
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct LibSQLReplicated
{
    /// <summary>
    /// Frame number
    /// </summary>
    public int FrameNo;
    
    /// <summary>
    /// Number of frames synced
    /// </summary>
    public int FramesSynced;
}

/// <summary>
/// blob structure for binary data
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct LibSQLBlob
{
    /// <summary>
    /// Pointer to blob data
    /// </summary>
    public IntPtr Ptr;
    
    /// <summary>
    /// Length of blob data
    /// </summary>
    public int Len;
    
    /// <summary>
    /// Gets the blob data as a byte array
    /// </summary>
    /// <returns>Byte array containing the blob data</returns>
    public readonly byte[] ToByteArray()
    {
        if (Ptr == IntPtr.Zero || Len <= 0)
            return Array.Empty<byte>();
            
        var result = new byte[Len];
        Marshal.Copy(Ptr, result, 0, Len);
        return result;
    }
}