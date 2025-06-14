using System;
using System.Runtime.Serialization;

namespace Nelknet.LibSQL.Data.Exceptions;

/// <summary>
/// Represents errors that occur when the database is busy or locked.
/// </summary>
[Serializable]
public class LibSQLBusyException : LibSQLException
{
    /// <summary>
    /// Gets the type of lock that caused the busy condition.
    /// </summary>
    public LockType LockType { get; }

    /// <summary>
    /// Gets the timeout value that was exceeded, if applicable.
    /// </summary>
    public TimeSpan? Timeout { get; }

    /// <summary>
    /// Gets whether this is a database-level lock.
    /// </summary>
    public bool IsDatabaseLocked { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="LibSQLBusyException"/> class.
    /// </summary>
    public LibSQLBusyException() : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LibSQLBusyException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public LibSQLBusyException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LibSQLBusyException"/> class with a specified error message and lock type.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="lockType">The type of lock that caused the busy condition.</param>
    /// <param name="isDatabaseLocked">Whether this is a database-level lock.</param>
    public LibSQLBusyException(string message, LockType lockType, bool isDatabaseLocked = false) : base(message)
    {
        LockType = lockType;
        IsDatabaseLocked = isDatabaseLocked;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LibSQLBusyException"/> class with full details.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="errorCode">The specific error code (SQLITE_BUSY or SQLITE_LOCKED).</param>
    /// <param name="lockType">The type of lock that caused the busy condition.</param>
    /// <param name="timeout">The timeout value that was exceeded.</param>
    /// <param name="sqlStatement">The SQL statement that encountered the lock.</param>
    public LibSQLBusyException(
        string message, 
        int errorCode,
        LockType lockType = LockType.Unknown,
        TimeSpan? timeout = null,
        string? sqlStatement = null) 
        : base(message, errorCode, sqlStatement: sqlStatement)
    {
        LockType = lockType;
        Timeout = timeout;
        IsDatabaseLocked = errorCode == LibSQLErrorMessages.SQLITE_BUSY;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LibSQLBusyException"/> class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public LibSQLBusyException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LibSQLBusyException"/> class with serialized data.
    /// </summary>
    /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
    /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
    [Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
    protected LibSQLBusyException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
        LockType = (LockType)info.GetValue(nameof(LockType), typeof(LockType))!;
        Timeout = (TimeSpan?)info.GetValue(nameof(Timeout), typeof(TimeSpan?));
        IsDatabaseLocked = info.GetBoolean(nameof(IsDatabaseLocked));
    }

    /// <summary>
    /// Sets the SerializationInfo with information about the exception.
    /// </summary>
    /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
    /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
    [Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        base.GetObjectData(info, context);
        info.AddValue(nameof(LockType), LockType);
        info.AddValue(nameof(Timeout), Timeout);
        info.AddValue(nameof(IsDatabaseLocked), IsDatabaseLocked);
    }

    /// <summary>
    /// Creates a busy exception with a timeout message.
    /// </summary>
    /// <param name="timeout">The timeout that was exceeded.</param>
    /// <param name="sqlStatement">The SQL statement that timed out.</param>
    /// <returns>A new LibSQLBusyException instance.</returns>
    public static LibSQLBusyException CreateTimeoutException(TimeSpan timeout, string? sqlStatement = null)
    {
        var message = $"Database operation timed out after {timeout.TotalSeconds:F1} seconds waiting for lock to be released.";
        return new LibSQLBusyException(message, LibSQLErrorMessages.SQLITE_BUSY, LockType.Timeout, timeout, sqlStatement);
    }
}

/// <summary>
/// Specifies the type of lock that caused a busy condition.
/// </summary>
public enum LockType
{
    /// <summary>
    /// Unknown lock type.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Database file is locked by another process.
    /// </summary>
    Database,

    /// <summary>
    /// A table is locked within the database.
    /// </summary>
    Table,

    /// <summary>
    /// Lock acquisition timed out.
    /// </summary>
    Timeout,

    /// <summary>
    /// Shared lock conflict.
    /// </summary>
    Shared,

    /// <summary>
    /// Reserved lock conflict.
    /// </summary>
    Reserved,

    /// <summary>
    /// Pending lock conflict.
    /// </summary>
    Pending,

    /// <summary>
    /// Exclusive lock conflict.
    /// </summary>
    Exclusive
}