using System.Collections.Generic;

namespace Nelknet.LibSQL.Data.Exceptions;

/// <summary>
/// Provides error messages for libSQL error codes.
/// </summary>
internal static class LibSQLErrorMessages
{
    // SQLite result codes (compatible with libSQL)
    public const int SQLITE_OK = 0;
    public const int SQLITE_ERROR = 1;
    public const int SQLITE_INTERNAL = 2;
    public const int SQLITE_PERM = 3;
    public const int SQLITE_ABORT = 4;
    public const int SQLITE_BUSY = 5;
    public const int SQLITE_LOCKED = 6;
    public const int SQLITE_NOMEM = 7;
    public const int SQLITE_READONLY = 8;
    public const int SQLITE_INTERRUPT = 9;
    public const int SQLITE_IOERR = 10;
    public const int SQLITE_CORRUPT = 11;
    public const int SQLITE_NOTFOUND = 12;
    public const int SQLITE_FULL = 13;
    public const int SQLITE_CANTOPEN = 14;
    public const int SQLITE_PROTOCOL = 15;
    public const int SQLITE_EMPTY = 16;
    public const int SQLITE_SCHEMA = 17;
    public const int SQLITE_TOOBIG = 18;
    public const int SQLITE_CONSTRAINT = 19;
    public const int SQLITE_MISMATCH = 20;
    public const int SQLITE_MISUSE = 21;
    public const int SQLITE_NOLFS = 22;
    public const int SQLITE_AUTH = 23;
    public const int SQLITE_FORMAT = 24;
    public const int SQLITE_RANGE = 25;
    public const int SQLITE_NOTADB = 26;
    public const int SQLITE_NOTICE = 27;
    public const int SQLITE_WARNING = 28;
    public const int SQLITE_ROW = 100;
    public const int SQLITE_DONE = 101;

    private static readonly Dictionary<int, string> ErrorMessages = new()
    {
        [SQLITE_OK] = "Success",
        [SQLITE_ERROR] = "SQL error or missing database",
        [SQLITE_INTERNAL] = "Internal logic error in SQLite",
        [SQLITE_PERM] = "Access permission denied",
        [SQLITE_ABORT] = "Callback routine requested an abort",
        [SQLITE_BUSY] = "The database file is locked",
        [SQLITE_LOCKED] = "A table in the database is locked",
        [SQLITE_NOMEM] = "A memory allocation failed",
        [SQLITE_READONLY] = "Attempt to write a readonly database",
        [SQLITE_INTERRUPT] = "Operation terminated by sqlite3_interrupt()",
        [SQLITE_IOERR] = "Some kind of disk I/O error occurred",
        [SQLITE_CORRUPT] = "The database disk image is malformed",
        [SQLITE_NOTFOUND] = "Unknown opcode in sqlite3_file_control()",
        [SQLITE_FULL] = "Insertion failed because database is full",
        [SQLITE_CANTOPEN] = "Unable to open the database file",
        [SQLITE_PROTOCOL] = "Database lock protocol error",
        [SQLITE_EMPTY] = "Database is empty",
        [SQLITE_SCHEMA] = "The database schema changed",
        [SQLITE_TOOBIG] = "String or BLOB exceeds size limit",
        [SQLITE_CONSTRAINT] = "Abort due to constraint violation",
        [SQLITE_MISMATCH] = "Data type mismatch",
        [SQLITE_MISUSE] = "Library used incorrectly",
        [SQLITE_NOLFS] = "Uses OS features not supported on host",
        [SQLITE_AUTH] = "Authorization denied",
        [SQLITE_FORMAT] = "Auxiliary database format error",
        [SQLITE_RANGE] = "2nd parameter to sqlite3_bind out of range",
        [SQLITE_NOTADB] = "File opened that is not a database file",
        [SQLITE_NOTICE] = "Notifications from sqlite3_log()",
        [SQLITE_WARNING] = "Warnings from sqlite3_log()",
        [SQLITE_ROW] = "sqlite3_step() has another row ready",
        [SQLITE_DONE] = "sqlite3_step() has finished executing",
    };

    /// <summary>
    /// Gets the error message for the specified error code.
    /// </summary>
    /// <param name="errorCode">The error code.</param>
    /// <returns>The error message.</returns>
    public static string GetErrorMessage(int errorCode)
    {
        if (ErrorMessages.TryGetValue(errorCode, out var message))
        {
            return $"libSQL error {errorCode}: {message}";
        }

        // Check for extended error codes (error code & 0xFF gives the base error)
        var baseErrorCode = errorCode & 0xFF;
        if (baseErrorCode != errorCode && ErrorMessages.TryGetValue(baseErrorCode, out message))
        {
            return $"libSQL error {errorCode} (extended from {baseErrorCode}): {message}";
        }

        return $"libSQL error {errorCode}: Unknown error code";
    }

    /// <summary>
    /// Checks if an error code represents a transient error that might succeed on retry.
    /// </summary>
    /// <param name="errorCode">The error code to check.</param>
    /// <returns>True if the error is transient; otherwise, false.</returns>
    public static bool IsTransientError(int errorCode)
    {
        return errorCode == SQLITE_BUSY || 
               errorCode == SQLITE_LOCKED || 
               errorCode == SQLITE_INTERRUPT ||
               (errorCode & 0xFF) == SQLITE_BUSY ||
               (errorCode & 0xFF) == SQLITE_LOCKED;
    }

    /// <summary>
    /// Checks if an error code represents a serious database corruption issue.
    /// </summary>
    /// <param name="errorCode">The error code to check.</param>
    /// <returns>True if the error indicates corruption; otherwise, false.</returns>
    public static bool IsCorruptionError(int errorCode)
    {
        var baseCode = errorCode & 0xFF;
        return baseCode == SQLITE_CORRUPT || 
               baseCode == SQLITE_NOTADB || 
               baseCode == SQLITE_FORMAT;
    }
}