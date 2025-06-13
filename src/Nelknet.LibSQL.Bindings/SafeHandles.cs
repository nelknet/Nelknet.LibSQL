using Microsoft.Win32.SafeHandles;
using System;

namespace Nelknet.LibSQL.Native;

/// <summary>
/// Safe handle for libsql_database_t
/// </summary>
internal sealed class LibSQLDatabaseHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    internal LibSQLDatabaseHandle() : base(true)
    {
    }

    internal LibSQLDatabaseHandle(IntPtr handle) : base(true)
    {
        SetHandle(handle);
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
internal sealed class LibSQLConnectionHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    internal LibSQLConnectionHandle() : base(true)
    {
    }

    internal LibSQLConnectionHandle(IntPtr handle) : base(true)
    {
        SetHandle(handle);
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
internal sealed class LibSQLStatementHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    internal LibSQLStatementHandle() : base(true)
    {
    }

    internal LibSQLStatementHandle(IntPtr handle) : base(true)
    {
        SetHandle(handle);
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
internal sealed class LibSQLRowsHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    internal LibSQLRowsHandle() : base(true)
    {
    }

    internal LibSQLRowsHandle(IntPtr handle) : base(true)
    {
        SetHandle(handle);
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
internal sealed class LibSQLRowHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    internal LibSQLRowHandle() : base(true)
    {
    }

    internal LibSQLRowHandle(IntPtr handle) : base(true)
    {
        SetHandle(handle);
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