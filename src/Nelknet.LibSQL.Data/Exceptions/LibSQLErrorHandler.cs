using System;
using System.Runtime.InteropServices;
using Nelknet.LibSQL.Bindings;

namespace Nelknet.LibSQL.Data.Exceptions;

/// <summary>
/// Provides centralized error handling and exception mapping for libSQL operations.
/// </summary>
internal static class LibSQLErrorHandler
{
    // Logger removed for now, can be added back with proper dependency

    // Logger functionality removed for now

    /// <summary>
    /// Checks the result code and throws an appropriate exception if it indicates an error.
    /// </summary>
    /// <param name="result">The result code from a libSQL operation.</param>
    /// <param name="db">The database handle for getting error message.</param>
    /// <param name="sqlStatement">The SQL statement that was executed.</param>
    /// <param name="errorContext">Additional context about the operation.</param>
    public static void CheckResult(int result, LibSQLSafeHandle? db = null, string? sqlStatement = null, string? errorContext = null)
    {
        if (result == LibSQLErrorMessages.SQLITE_OK || result == LibSQLErrorMessages.SQLITE_ROW || result == LibSQLErrorMessages.SQLITE_DONE)
        {
            return;
        }

        var exception = CreateException(result, db, sqlStatement, errorContext);
        LogError(exception);
        throw exception;
    }

    /// <summary>
    /// Creates an appropriate exception based on the error code.
    /// </summary>
    /// <param name="errorCode">The error code.</param>
    /// <param name="db">The database handle for getting error message.</param>
    /// <param name="sqlStatement">The SQL statement that was executed.</param>
    /// <param name="errorContext">Additional context about the operation.</param>
    /// <returns>The created exception.</returns>
    public static LibSQLException CreateException(int errorCode, LibSQLSafeHandle? db = null, string? sqlStatement = null, string? errorContext = null)
    {
        // Get the error message from the database if available
        string? dbErrorMessage = null;
        int extendedErrorCode = errorCode;

        if (db != null && !db.IsInvalid && !db.IsClosed)
        {
            try
            {
                dbErrorMessage = GetLastErrorMessage(db);
                extendedErrorCode = GetExtendedErrorCode(db);
            }
            catch
            {
                // Ignore errors getting error details
            }
        }

        var message = dbErrorMessage ?? LibSQLErrorMessages.GetErrorMessage(errorCode);
        
        // Determine the specific exception type based on error code
        var baseErrorCode = errorCode & 0xFF;
        
        switch (baseErrorCode)
        {
            case LibSQLErrorMessages.SQLITE_BUSY:
            case LibSQLErrorMessages.SQLITE_LOCKED:
                var lockType = baseErrorCode == LibSQLErrorMessages.SQLITE_BUSY ? LockType.Database : LockType.Table;
                return new LibSQLBusyException(message, errorCode, lockType, sqlStatement: sqlStatement);

            case LibSQLErrorMessages.SQLITE_CONSTRAINT:
                var constraintType = DetermineConstraintType(extendedErrorCode);
                return new LibSQLConstraintException(message, constraintType, sqlStatement: sqlStatement);

            case LibSQLErrorMessages.SQLITE_CANTOPEN:
            case LibSQLErrorMessages.SQLITE_NOTADB:
            case LibSQLErrorMessages.SQLITE_AUTH:
            case LibSQLErrorMessages.SQLITE_PERM:
                return new LibSQLConnectionException(message, errorCode);

            case LibSQLErrorMessages.SQLITE_CORRUPT:
            case LibSQLErrorMessages.SQLITE_FORMAT:
                // These are serious errors that might require database recovery
                return new LibSQLException(
                    $"Database corruption detected: {message}", 
                    errorCode, 
                    extendedErrorCode, 
                    sqlStatement, 
                    errorContext);

            default:
                return new LibSQLException(message, errorCode, extendedErrorCode, sqlStatement, errorContext);
        }
    }

    /// <summary>
    /// Gets the last error message from the database.
    /// </summary>
    private static string? GetLastErrorMessage(LibSQLSafeHandle db)
    {
        try
        {
            var ptr = LibSQLNative.sqlite3_errmsg(db.DangerousGetHandle());
            return Marshal.PtrToStringUTF8(ptr);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the extended error code from the database.
    /// </summary>
    private static int GetExtendedErrorCode(LibSQLSafeHandle db)
    {
        try
        {
            return LibSQLNative.sqlite3_extended_errcode(db.DangerousGetHandle());
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Determines the constraint type from an extended error code.
    /// </summary>
    private static ConstraintType DetermineConstraintType(int extendedErrorCode)
    {
        // SQLite extended result codes for SQLITE_CONSTRAINT
        const int SQLITE_CONSTRAINT_CHECK = 275;
        const int SQLITE_CONSTRAINT_FOREIGNKEY = 787;
        const int SQLITE_CONSTRAINT_NOTNULL = 1299;
        const int SQLITE_CONSTRAINT_PRIMARYKEY = 1555;
        const int SQLITE_CONSTRAINT_UNIQUE = 2067;
        const int SQLITE_CONSTRAINT_ROWID = 2579;

        return extendedErrorCode switch
        {
            SQLITE_CONSTRAINT_CHECK => ConstraintType.Check,
            SQLITE_CONSTRAINT_FOREIGNKEY => ConstraintType.ForeignKey,
            SQLITE_CONSTRAINT_NOTNULL => ConstraintType.NotNull,
            SQLITE_CONSTRAINT_PRIMARYKEY => ConstraintType.PrimaryKey,
            SQLITE_CONSTRAINT_UNIQUE => ConstraintType.Unique,
            SQLITE_CONSTRAINT_ROWID => ConstraintType.RowId,
            _ => ConstraintType.Unknown
        };
    }

    /// <summary>
    /// Logs an error if a logger is configured.
    /// </summary>
    private static void LogError(LibSQLException exception)
    {
        // Logger functionality removed for now
        // Can be re-added with proper dependency injection
    }

    // Log level determination removed for now

    /// <summary>
    /// Wraps an action with error handling.
    /// </summary>
    public static void Execute(Action action, LibSQLSafeHandle? db = null, string? sqlStatement = null, string? errorContext = null)
    {
        try
        {
            action();
        }
        catch (LibSQLException)
        {
            throw;
        }
        catch (Exception ex)
        {
            var wrappedException = new LibSQLException(
                $"Unexpected error during libSQL operation: {ex.Message}",
                LibSQLErrorMessages.SQLITE_ERROR,
                sqlStatement: sqlStatement,
                errorContext: errorContext,
                innerException: ex);
            
            LogError(wrappedException);
            throw wrappedException;
        }
    }

    /// <summary>
    /// Wraps a function with error handling.
    /// </summary>
    public static T Execute<T>(Func<T> func, LibSQLSafeHandle? db = null, string? sqlStatement = null, string? errorContext = null)
    {
        try
        {
            return func();
        }
        catch (LibSQLException)
        {
            throw;
        }
        catch (Exception ex)
        {
            var wrappedException = new LibSQLException(
                $"Unexpected error during libSQL operation: {ex.Message}",
                LibSQLErrorMessages.SQLITE_ERROR,
                sqlStatement: sqlStatement,
                errorContext: errorContext,
                innerException: ex);
            
            LogError(wrappedException);
            throw wrappedException;
        }
    }
}