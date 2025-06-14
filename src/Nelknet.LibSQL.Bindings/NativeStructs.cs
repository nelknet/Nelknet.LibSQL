using System;
using System.Runtime.InteropServices;

namespace Nelknet.LibSQL.Bindings;

/// <summary>
/// LibSQL result codes (based on SQLite result codes)
/// </summary>
internal enum LibSQLResultCode : int
{
    /// <summary>
    /// Successful result
    /// </summary>
    Ok = 0,
    
    /// <summary>
    /// Generic error
    /// </summary>
    Error = 1,
    
    /// <summary>
    /// Internal logic error in SQLite
    /// </summary>
    Internal = 2,
    
    /// <summary>
    /// Access permission denied
    /// </summary>
    Perm = 3,
    
    /// <summary>
    /// Callback routine requested an abort
    /// </summary>
    Abort = 4,
    
    /// <summary>
    /// The database file is locked
    /// </summary>
    Busy = 5,
    
    /// <summary>
    /// A table in the database is locked
    /// </summary>
    Locked = 6,
    
    /// <summary>
    /// A malloc() failed
    /// </summary>
    NoMem = 7,
    
    /// <summary>
    /// Attempt to write a readonly database
    /// </summary>
    ReadOnly = 8,
    
    /// <summary>
    /// Operation terminated by interrupt
    /// </summary>
    Interrupt = 9,
    
    /// <summary>
    /// Some kind of disk I/O error occurred
    /// </summary>
    IoErr = 10,
    
    /// <summary>
    /// The database disk image is malformed
    /// </summary>
    Corrupt = 11,
    
    /// <summary>
    /// Unknown opcode in file control
    /// </summary>
    NotFound = 12,
    
    /// <summary>
    /// Insertion failed because database is full
    /// </summary>
    Full = 13,
    
    /// <summary>
    /// Unable to open the database file
    /// </summary>
    CantOpen = 14,
    
    /// <summary>
    /// Database lock protocol error
    /// </summary>
    Protocol = 15,
    
    /// <summary>
    /// Internal use only
    /// </summary>
    Empty = 16,
    
    /// <summary>
    /// The database schema changed
    /// </summary>
    Schema = 17,
    
    /// <summary>
    /// String or BLOB exceeds size limit
    /// </summary>
    TooBig = 18,
    
    /// <summary>
    /// Abort due to constraint violation
    /// </summary>
    Constraint = 19,
    
    /// <summary>
    /// Data type mismatch
    /// </summary>
    Mismatch = 20,
    
    /// <summary>
    /// Library used incorrectly
    /// </summary>
    Misuse = 21,
    
    /// <summary>
    /// Uses OS features not supported on host
    /// </summary>
    NoLfs = 22,
    
    /// <summary>
    /// Authorization denied
    /// </summary>
    Auth = 23,
    
    /// <summary>
    /// Not used
    /// </summary>
    Format = 24,
    
    /// <summary>
    /// 2nd parameter to bind out of range
    /// </summary>
    Range = 25,
    
    /// <summary>
    /// File opened that is not a database file
    /// </summary>
    NotADb = 26,
    
    /// <summary>
    /// Notifications from log
    /// </summary>
    Notice = 27,
    
    /// <summary>
    /// Warnings from log
    /// </summary>
    Warning = 28,
    
    /// <summary>
    /// Step has another row ready
    /// </summary>
    Row = 100,
    
    /// <summary>
    /// Step has finished executing
    /// </summary>
    Done = 101
}

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

/// <summary>
/// Transaction behavior modes
/// </summary>
internal enum LibSQLTransactionBehavior
{
    /// <summary>
    /// Deferred transaction (default) - lock acquired when needed
    /// </summary>
    Deferred = 0,
    
    /// <summary>
    /// Immediate transaction - reserved lock acquired immediately
    /// </summary>
    Immediate = 1,
    
    /// <summary>
    /// Exclusive transaction - exclusive lock acquired immediately
    /// </summary>
    Exclusive = 2
}