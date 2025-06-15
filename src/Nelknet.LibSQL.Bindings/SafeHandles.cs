using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

namespace Nelknet.LibSQL.Bindings;

/// <summary>
/// Base class for all libSQL safe handles
/// </summary>
internal abstract class LibSQLSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    protected LibSQLSafeHandle() : base(true)
    {
    }

    protected LibSQLSafeHandle(IntPtr handle) : base(true)
    {
        SetHandle(handle);
    }
}

/// <summary>
/// Safe handle for libsql_database_t
/// </summary>
internal sealed class LibSQLDatabaseHandle : LibSQLSafeHandle
{
    internal LibSQLDatabaseHandle() : base()
    {
    }

    internal LibSQLDatabaseHandle(IntPtr handle) : base(handle)
    {
    }

    protected override bool ReleaseHandle()
    {
        if (!IsInvalid && !IsClosed)
        {
            LibSQLNative.libsql_close(handle);
        }
        return true;
    }
}

/// <summary>
/// Safe handle for libsql_connection_t
/// </summary>
internal sealed class LibSQLConnectionHandle : LibSQLSafeHandle
{
    internal LibSQLConnectionHandle() : base()
    {
    }

    internal LibSQLConnectionHandle(IntPtr handle) : base(handle)
    {
    }

    protected override bool ReleaseHandle()
    {
        if (!IsInvalid && !IsClosed)
        {
            LibSQLNative.libsql_disconnect(handle);
        }
        return true;
    }
}

/// <summary>
/// Safe handle for libsql_stmt_t
/// </summary>
internal sealed class LibSQLStatementHandle : LibSQLSafeHandle
{
    internal LibSQLStatementHandle() : base()
    {
    }

    internal LibSQLStatementHandle(IntPtr handle) : base(handle)
    {
    }

    protected override bool ReleaseHandle()
    {
        if (!IsInvalid && !IsClosed)
        {
            LibSQLNative.libsql_free_stmt(handle);
        }
        return true;
    }
}

/// <summary>
/// Safe handle for libsql_rows_t
/// </summary>
internal sealed class LibSQLRowsHandle : LibSQLSafeHandle
{
    internal LibSQLRowsHandle() : base()
    {
    }

    internal LibSQLRowsHandle(IntPtr handle) : base(handle)
    {
    }

    protected override bool ReleaseHandle()
    {
        if (!IsInvalid && !IsClosed)
        {
            LibSQLNative.libsql_free_rows(handle);
        }
        return true;
    }
}

/// <summary>
/// Safe handle for libsql_row_t
/// </summary>
internal sealed class LibSQLRowHandle : LibSQLSafeHandle
{
    internal LibSQLRowHandle() : base()
    {
    }

    internal LibSQLRowHandle(IntPtr handle) : base(handle)
    {
    }

    protected override bool ReleaseHandle()
    {
        if (!IsInvalid && !IsClosed)
        {
            LibSQLNative.libsql_free_row(handle);
        }
        return true;
    }
}

/// <summary>
/// Safe handle for generic libSQL pointer that needs to be freed with free()
/// Used for error messages and other allocated strings
/// </summary>
internal sealed class LibSQLAllocatedPointerHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    internal LibSQLAllocatedPointerHandle() : base(true)
    {
    }

    internal LibSQLAllocatedPointerHandle(IntPtr handle) : base(true)
    {
        SetHandle(handle);
    }

    protected override bool ReleaseHandle()
    {
        if (!IsInvalid && !IsClosed)
        {
            // Use standard C library free() for generic allocated pointers
            // This handle is used for error messages and similar allocated strings
            Marshal.FreeHGlobal(handle);
        }
        return true;
    }
}

/// <summary>
/// Safe handle for managing libSQL string pointers that need to be freed
/// </summary>
internal sealed class LibSQLStringHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    internal LibSQLStringHandle() : base(true)
    {
    }

    internal LibSQLStringHandle(IntPtr handle) : base(true)
    {
        SetHandle(handle);
    }

    protected override bool ReleaseHandle()
    {
        if (!IsInvalid && !IsClosed)
        {
            LibSQLNative.libsql_free_string(handle);
        }
        return true;
    }
}

/// <summary>
/// Safe handle for sqlite3_backup operations
/// </summary>
internal sealed class LibSQLBackupHandle : LibSQLSafeHandle
{
    internal LibSQLBackupHandle() : base()
    {
    }

    internal LibSQLBackupHandle(IntPtr handle) : base(handle)
    {
    }

    protected override bool ReleaseHandle()
    {
        if (!IsInvalid && !IsClosed)
        {
            LibSQLNative.sqlite3_backup_finish(handle);
        }
        return true;
    }
}