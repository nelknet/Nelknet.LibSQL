using System;
using System.Runtime.InteropServices;
using Nelknet.LibSQL.Bindings;

namespace Nelknet.LibSQL.Native;

/// <summary>
/// Native P/Invoke methods for libSQL library
/// </summary>
internal static partial class LibSQLNative
{
    private const string LibraryName = LibSQLNativeLibrary.LibraryName;
    
    /// <summary>
    /// Initializes the native library
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the native library cannot be loaded</exception>
    internal static void Initialize()
    {
        if (!LibSQLNativeLibrary.TryInitialize())
        {
            throw new InvalidOperationException("Failed to load libSQL native library. " +
                "Please ensure the appropriate native library is available for your platform.");
        }
    }
    
    #region Database Management
    
    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial int libsql_open_ext(string url, out IntPtr outDb, out IntPtr outErrMsg);
    
    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial int libsql_open_file(string url, out IntPtr outDb, out IntPtr outErrMsg);
    
    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial int libsql_open_remote(string url, string authToken, out IntPtr outDb, out IntPtr outErrMsg);
    
    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial int libsql_open_remote_with_webpki(string url, string authToken, out IntPtr outDb, out IntPtr outErrMsg);
    
    [LibraryImport(LibraryName)]
    internal static partial int libsql_open_sync_with_config(in LibSQLConfig config, out IntPtr outDb, out IntPtr outErrMsg);
    
    [LibraryImport(LibraryName)]
    internal static partial void libsql_close(IntPtr db);
    
    #endregion
    
    #region Connection Management
    
    [LibraryImport(LibraryName)]
    internal static partial int libsql_connect(LibSQLDatabaseHandle db, out IntPtr outConn, out IntPtr outErrMsg);
    
    [LibraryImport(LibraryName)]
    internal static partial void libsql_disconnect(IntPtr conn);
    
    [LibraryImport(LibraryName)]
    internal static partial int libsql_reset(LibSQLConnectionHandle conn, out IntPtr outErrMsg);
    
    #endregion
    
    #region Statement Execution
    
    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial int libsql_prepare(LibSQLConnectionHandle conn, string sql, out IntPtr outStmt, out IntPtr outErrMsg);
    
    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial int libsql_execute(LibSQLConnectionHandle conn, string sql, out IntPtr outErrMsg);
    
    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial int libsql_query(LibSQLConnectionHandle conn, string sql, out IntPtr outRows, out IntPtr outErrMsg);
    
    [LibraryImport(LibraryName)]
    internal static partial int libsql_execute_stmt(LibSQLStatementHandle stmt, out IntPtr outErrMsg);
    
    [LibraryImport(LibraryName)]
    internal static partial int libsql_query_stmt(LibSQLStatementHandle stmt, out IntPtr outRows, out IntPtr outErrMsg);
    
    [LibraryImport(LibraryName)]
    internal static partial int libsql_reset_stmt(LibSQLStatementHandle stmt, out IntPtr outErrMsg);
    
    [LibraryImport(LibraryName)]
    internal static partial void libsql_free_stmt(IntPtr stmt);
    
    #endregion
    
    #region Parameter Binding
    
    [LibraryImport(LibraryName)]
    internal static partial int libsql_bind_int(LibSQLStatementHandle stmt, int idx, long value, out IntPtr outErrMsg);
    
    [LibraryImport(LibraryName)]
    internal static partial int libsql_bind_float(LibSQLStatementHandle stmt, int idx, double value, out IntPtr outErrMsg);
    
    [LibraryImport(LibraryName)]
    internal static partial int libsql_bind_null(LibSQLStatementHandle stmt, int idx, out IntPtr outErrMsg);
    
    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial int libsql_bind_string(LibSQLStatementHandle stmt, int idx, string value, out IntPtr outErrMsg);
    
    [LibraryImport(LibraryName)]
    internal static partial int libsql_bind_blob(LibSQLStatementHandle stmt, int idx, IntPtr value, int valueLen, out IntPtr outErrMsg);
    
    #endregion
    
    #region Result Processing
    
    [LibraryImport(LibraryName)]
    internal static partial void libsql_free_rows(IntPtr rows);
    
    [LibraryImport(LibraryName)]
    internal static partial int libsql_column_count(LibSQLRowsHandle rows);
    
    [LibraryImport(LibraryName)]
    internal static partial int libsql_column_name(LibSQLRowsHandle rows, int col, out IntPtr outName, out IntPtr outErrMsg);
    
    [LibraryImport(LibraryName)]
    internal static partial int libsql_column_type(LibSQLRowsHandle rows, LibSQLRowHandle row, int col, out int outType, out IntPtr outErrMsg);
    
    [LibraryImport(LibraryName)]
    internal static partial int libsql_next_row(LibSQLRowsHandle rows, out IntPtr outRow, out IntPtr outErrMsg);
    
    [LibraryImport(LibraryName)]
    internal static partial void libsql_free_row(IntPtr row);
    
    [LibraryImport(LibraryName)]
    internal static partial int libsql_get_string(LibSQLRowHandle row, int col, out IntPtr outValue, out IntPtr outErrMsg);
    
    [LibraryImport(LibraryName)]
    internal static partial void libsql_free_string(IntPtr ptr);
    
    [LibraryImport(LibraryName)]
    internal static partial int libsql_get_int(LibSQLRowHandle row, int col, out long outValue, out IntPtr outErrMsg);
    
    [LibraryImport(LibraryName)]
    internal static partial int libsql_get_float(LibSQLRowHandle row, int col, out double outValue, out IntPtr outErrMsg);
    
    [LibraryImport(LibraryName)]
    internal static partial int libsql_get_blob(LibSQLRowHandle row, int col, out LibSQLBlob outBlob, out IntPtr outErrMsg);
    
    [LibraryImport(LibraryName)]
    internal static partial void libsql_free_blob(LibSQLBlob blob);
    
    #endregion
    
    #region Utility Functions
    
    [LibraryImport(LibraryName)]
    internal static partial ulong libsql_changes(LibSQLConnectionHandle conn);
    
    [LibraryImport(LibraryName)]
    internal static partial long libsql_last_insert_rowid(LibSQLConnectionHandle conn);
    
    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial int libsql_load_extension(LibSQLConnectionHandle conn, string path, string? entryPoint, out IntPtr outErrMsg);
    
    #endregion
    
    #region Sync/Replication
    
    [LibraryImport(LibraryName)]
    internal static partial int libsql_sync(LibSQLDatabaseHandle db, out IntPtr outErrMsg);
    
    [LibraryImport(LibraryName)]
    internal static partial int libsql_sync2(LibSQLDatabaseHandle db, out LibSQLReplicated outReplicated, out IntPtr outErrMsg);
    
    #endregion
    
    #region Tracing
    
    [LibraryImport(LibraryName)]
    internal static partial int libsql_enable_internal_tracing();
    
    #endregion
}