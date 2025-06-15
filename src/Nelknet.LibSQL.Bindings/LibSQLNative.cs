using System;
using System.Runtime.InteropServices;

namespace Nelknet.LibSQL.Bindings;

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
    
    #region Error Handling
    
    [LibraryImport(LibraryName, EntryPoint = "libsql_free_string")]
    internal static partial void libsql_free_error_msg(IntPtr errMsg);
    
    /// <summary>
    /// Returns the last error message for the database connection in English.
    /// </summary>
    /// <param name="db">Database handle</param>
    /// <returns>Pointer to error message string</returns>
    [LibraryImport(LibraryName, EntryPoint = "sqlite3_errmsg")]
    internal static partial IntPtr sqlite3_errmsg(IntPtr db);
    
    /// <summary>
    /// Returns the extended error code for the most recent failed API call.
    /// </summary>
    /// <param name="db">Database handle</param>
    /// <returns>Extended error code</returns>
    [LibraryImport(LibraryName, EntryPoint = "sqlite3_extended_errcode")]
    internal static partial int sqlite3_extended_errcode(IntPtr db);
    
    /// <summary>
    /// Returns the error code for the most recent failed API call.
    /// </summary>
    /// <param name="db">Database handle</param>
    /// <returns>Error code</returns>
    [LibraryImport(LibraryName, EntryPoint = "sqlite3_errcode")]
    internal static partial int sqlite3_errcode(IntPtr db);
    
    #endregion
    
    #region Transaction Control
    
    // Transactions in libSQL are handled via SQL commands (BEGIN, COMMIT, ROLLBACK)
    // not via separate API functions
    
    #endregion
    
    #region Tracing
    
    [LibraryImport(LibraryName)]
    internal static partial int libsql_enable_internal_tracing();
    
    #endregion
    
    #region Version Information
    
    /// <summary>
    /// Returns the libSQL version string.
    /// </summary>
    /// <returns>The libSQL version as a string (e.g., "0.2.3").</returns>
    [LibraryImport(LibraryName)]
    internal static partial IntPtr libsql_libversion();
    
    /// <summary>
    /// Returns the SQLite version string.
    /// </summary>
    /// <returns>The SQLite version as a string (e.g., "3.45.1").</returns>
    [LibraryImport(LibraryName, EntryPoint = "sqlite3_libversion")]
    internal static partial IntPtr sqlite3_libversion();
    
    /// <summary>
    /// Returns the SQLite version number.
    /// </summary>
    /// <returns>The SQLite version as an integer (e.g., 3045001).</returns>
    [LibraryImport(LibraryName, EntryPoint = "sqlite3_libversion_number")]
    internal static partial int sqlite3_libversion_number();
    
    /// <summary>
    /// Returns the SQLite source identifier.
    /// </summary>
    /// <returns>The SQLite source identifier string.</returns>
    [LibraryImport(LibraryName, EntryPoint = "sqlite3_sourceid")]
    internal static partial IntPtr sqlite3_sourceid();
    
    #endregion
    
    #region Custom Functions and Aggregates
    
    /// <summary>
    /// Creates or redefines a SQL function.
    /// </summary>
    /// <param name="db">Database connection</param>
    /// <param name="zFunctionName">Name of the SQL function</param>
    /// <param name="nArg">Number of arguments (-1 for any number)</param>
    /// <param name="eTextRep">Text encoding (e.g., SQLITE_UTF8)</param>
    /// <param name="pApp">Application data pointer</param>
    /// <param name="xFunc">Function callback for scalar functions</param>
    /// <param name="xStep">Step callback for aggregate functions</param>
    /// <param name="xFinal">Final callback for aggregate functions</param>
    /// <returns>Result code</returns>
    [LibraryImport(LibraryName, EntryPoint = "sqlite3_create_function", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial int sqlite3_create_function(
        IntPtr db,
        string zFunctionName,
        int nArg,
        int eTextRep,
        IntPtr pApp,
        IntPtr xFunc,  // void (*xFunc)(sqlite3_context*,int,sqlite3_value**)
        IntPtr xStep,  // void (*xStep)(sqlite3_context*,int,sqlite3_value**)
        IntPtr xFinal  // void (*xFinal)(sqlite3_context*)
    );
    
    /// <summary>
    /// Creates or redefines a SQL function with destructor callback.
    /// </summary>
    [LibraryImport(LibraryName, EntryPoint = "sqlite3_create_function_v2", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial int sqlite3_create_function_v2(
        IntPtr db,
        string zFunctionName,
        int nArg,
        int eTextRep,
        IntPtr pApp,
        IntPtr xFunc,
        IntPtr xStep,
        IntPtr xFinal,
        IntPtr xDestroy  // void(*xDestroy)(void*)
    );
    
    // Function context methods
    [LibraryImport(LibraryName, EntryPoint = "sqlite3_result_null")]
    internal static partial void sqlite3_result_null(IntPtr context);
    
    [LibraryImport(LibraryName, EntryPoint = "sqlite3_result_int64")]
    internal static partial void sqlite3_result_int64(IntPtr context, long value);
    
    [LibraryImport(LibraryName, EntryPoint = "sqlite3_result_double")]
    internal static partial void sqlite3_result_double(IntPtr context, double value);
    
    [LibraryImport(LibraryName, EntryPoint = "sqlite3_result_text", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial void sqlite3_result_text(IntPtr context, string value, int nBytes, IntPtr destructor);
    
    [LibraryImport(LibraryName, EntryPoint = "sqlite3_result_blob")]
    internal static partial void sqlite3_result_blob(IntPtr context, IntPtr value, int nBytes, IntPtr destructor);
    
    [LibraryImport(LibraryName, EntryPoint = "sqlite3_result_error", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial void sqlite3_result_error(IntPtr context, string errMsg, int nBytes);
    
    // Value access methods
    [LibraryImport(LibraryName, EntryPoint = "sqlite3_value_type")]
    internal static partial int sqlite3_value_type(IntPtr value);
    
    [LibraryImport(LibraryName, EntryPoint = "sqlite3_value_int64")]
    internal static partial long sqlite3_value_int64(IntPtr value);
    
    [LibraryImport(LibraryName, EntryPoint = "sqlite3_value_double")]
    internal static partial double sqlite3_value_double(IntPtr value);
    
    [LibraryImport(LibraryName, EntryPoint = "sqlite3_value_text")]
    internal static partial IntPtr sqlite3_value_text(IntPtr value);
    
    [LibraryImport(LibraryName, EntryPoint = "sqlite3_value_blob")]
    internal static partial IntPtr sqlite3_value_blob(IntPtr value);
    
    [LibraryImport(LibraryName, EntryPoint = "sqlite3_value_bytes")]
    internal static partial int sqlite3_value_bytes(IntPtr value);
    
    // Aggregate context
    [LibraryImport(LibraryName, EntryPoint = "sqlite3_aggregate_context")]
    internal static partial IntPtr sqlite3_aggregate_context(IntPtr context, int nBytes);
    
    // User data
    [LibraryImport(LibraryName, EntryPoint = "sqlite3_user_data")]
    internal static partial IntPtr sqlite3_user_data(IntPtr context);
    
    // Text encoding constants
    internal const int SQLITE_UTF8 = 1;
    internal const int SQLITE_UTF16LE = 2;
    internal const int SQLITE_UTF16BE = 3;
    internal const int SQLITE_UTF16 = 4;
    internal const int SQLITE_ANY = 5;
    
    // Function flags
    internal const int SQLITE_DETERMINISTIC = 0x000000800;
    internal const int SQLITE_DIRECTONLY = 0x000080000;
    internal const int SQLITE_SUBTYPE = 0x000100000;
    internal const int SQLITE_INNOCUOUS = 0x000200000;
    internal const int SQLITE_RESULT_SUBTYPE = 0x001000000;
    
    // Special destructor values
    internal static readonly IntPtr SQLITE_STATIC = IntPtr.Zero;
    internal static readonly IntPtr SQLITE_TRANSIENT = new IntPtr(-1);
    
    #endregion
    
    #region Backup/Restore
    
    /// <summary>
    /// Initialize a backup operation.
    /// </summary>
    [LibraryImport(LibraryName, EntryPoint = "sqlite3_backup_init", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial IntPtr sqlite3_backup_init(
        IntPtr pDest,
        string zDestName,
        IntPtr pSource,
        string zSourceName
    );
    
    /// <summary>
    /// Perform a backup step.
    /// </summary>
    [LibraryImport(LibraryName, EntryPoint = "sqlite3_backup_step")]
    internal static partial int sqlite3_backup_step(IntPtr backup, int nPage);
    
    /// <summary>
    /// Finish a backup operation.
    /// </summary>
    [LibraryImport(LibraryName, EntryPoint = "sqlite3_backup_finish")]
    internal static partial int sqlite3_backup_finish(IntPtr backup);
    
    /// <summary>
    /// Get the number of pages remaining to be backed up.
    /// </summary>
    [LibraryImport(LibraryName, EntryPoint = "sqlite3_backup_remaining")]
    internal static partial int sqlite3_backup_remaining(IntPtr backup);
    
    /// <summary>
    /// Get the total number of pages in the source database.
    /// </summary>
    [LibraryImport(LibraryName, EntryPoint = "sqlite3_backup_pagecount")]
    internal static partial int sqlite3_backup_pagecount(IntPtr backup);
    
    #endregion
    
    #region Extended Result Codes
    
    /// <summary>
    /// Enable or disable extended result codes.
    /// </summary>
    [LibraryImport(LibraryName, EntryPoint = "sqlite3_extended_result_codes")]
    internal static partial int sqlite3_extended_result_codes(IntPtr db, int onoff);
    
    #endregion
    
    #region Statement Status
    
    /// <summary>
    /// Check if a prepared statement is an EXPLAIN statement.
    /// </summary>
    [LibraryImport(LibraryName, EntryPoint = "sqlite3_stmt_isexplain")]
    internal static partial int sqlite3_stmt_isexplain(IntPtr stmt);
    
    /// <summary>
    /// Change the EXPLAIN setting for a prepared statement.
    /// </summary>
    [LibraryImport(LibraryName, EntryPoint = "sqlite3_stmt_explain")]
    internal static partial int sqlite3_stmt_explain(IntPtr stmt, int eMode);
    
    #endregion
}