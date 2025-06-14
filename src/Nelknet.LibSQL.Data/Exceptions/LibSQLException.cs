using System;
using System.Data.Common;
using System.Runtime.Serialization;

namespace Nelknet.LibSQL.Data.Exceptions;

/// <summary>
/// Represents errors that occur during libSQL operations.
/// </summary>
[Serializable]
public class LibSQLException : DbException
{
    /// <summary>
    /// Gets the libSQL error code.
    /// </summary>
    public int LibSQLErrorCode { get; }

    /// <summary>
    /// Gets the extended error code, if available.
    /// </summary>
    public int? ExtendedErrorCode { get; }

    /// <summary>
    /// Gets the SQL statement that caused the error, if available.
    /// </summary>
    public string? SqlStatement { get; }

    /// <summary>
    /// Gets additional context about the error.
    /// </summary>
    public string? ErrorContext { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="LibSQLException"/> class.
    /// </summary>
    public LibSQLException() : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LibSQLException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public LibSQLException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LibSQLException"/> class with a specified error message and error code.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="errorCode">The libSQL error code.</param>
    public LibSQLException(string message, int errorCode) : base(message)
    {
        LibSQLErrorCode = errorCode;
        HResult = errorCode;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LibSQLException"/> class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public LibSQLException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LibSQLException"/> class with full error details.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="errorCode">The libSQL error code.</param>
    /// <param name="extendedErrorCode">The extended error code, if available.</param>
    /// <param name="sqlStatement">The SQL statement that caused the error.</param>
    /// <param name="errorContext">Additional context about the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public LibSQLException(
        string message, 
        int errorCode, 
        int? extendedErrorCode = null,
        string? sqlStatement = null,
        string? errorContext = null,
        Exception? innerException = null) 
        : base(message, innerException)
    {
        LibSQLErrorCode = errorCode;
        HResult = errorCode;
        ExtendedErrorCode = extendedErrorCode;
        SqlStatement = sqlStatement;
        ErrorContext = errorContext;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LibSQLException"/> class with serialized data.
    /// </summary>
    /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
    /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
    [Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
    protected LibSQLException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
        LibSQLErrorCode = info.GetInt32(nameof(LibSQLErrorCode));
        ExtendedErrorCode = (int?)info.GetValue(nameof(ExtendedErrorCode), typeof(int?));
        SqlStatement = info.GetString(nameof(SqlStatement));
        ErrorContext = info.GetString(nameof(ErrorContext));
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
        info.AddValue(nameof(LibSQLErrorCode), LibSQLErrorCode);
        info.AddValue(nameof(ExtendedErrorCode), ExtendedErrorCode);
        info.AddValue(nameof(SqlStatement), SqlStatement);
        info.AddValue(nameof(ErrorContext), ErrorContext);
    }

    /// <summary>
    /// Creates a LibSQLException from a native error code.
    /// </summary>
    /// <param name="errorCode">The native error code.</param>
    /// <param name="customMessage">Optional custom message to override the default.</param>
    /// <param name="sqlStatement">The SQL statement that caused the error.</param>
    /// <param name="errorContext">Additional context about the error.</param>
    /// <returns>A new LibSQLException instance.</returns>
    public static LibSQLException FromErrorCode(int errorCode, string? customMessage = null, string? sqlStatement = null, string? errorContext = null)
    {
        var message = customMessage ?? LibSQLErrorMessages.GetErrorMessage(errorCode);
        return new LibSQLException(message, errorCode, sqlStatement: sqlStatement, errorContext: errorContext);
    }
}