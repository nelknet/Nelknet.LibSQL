using System;
using System.Runtime.Serialization;

namespace Nelknet.LibSQL.Data.Exceptions;

/// <summary>
/// Represents errors that occur when connecting to a libSQL database.
/// </summary>
[Serializable]
public class LibSQLConnectionException : LibSQLException
{
    /// <summary>
    /// Gets the connection string that was used when the error occurred.
    /// </summary>
    public string? ConnectionString { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="LibSQLConnectionException"/> class.
    /// </summary>
    public LibSQLConnectionException() : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LibSQLConnectionException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public LibSQLConnectionException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LibSQLConnectionException"/> class with a specified error message and connection string.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="connectionString">The connection string that was used.</param>
    public LibSQLConnectionException(string message, string connectionString) : base(message)
    {
        ConnectionString = connectionString;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LibSQLConnectionException"/> class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public LibSQLConnectionException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LibSQLConnectionException"/> class with full error details.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="errorCode">The libSQL error code.</param>
    /// <param name="connectionString">The connection string that was used.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public LibSQLConnectionException(string message, int errorCode, string? connectionString = null, Exception? innerException = null) 
        : base(message, errorCode, innerException: innerException)
    {
        ConnectionString = connectionString;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LibSQLConnectionException"/> class with serialized data.
    /// </summary>
    /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
    /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
    [Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
    protected LibSQLConnectionException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
        ConnectionString = info.GetString(nameof(ConnectionString));
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
        info.AddValue(nameof(ConnectionString), ConnectionString);
    }
}