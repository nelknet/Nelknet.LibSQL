using System;
using System.Runtime.Serialization;

namespace Nelknet.LibSQL.Data.Exceptions;

/// <summary>
/// Represents errors that occur due to constraint violations in a libSQL database.
/// </summary>
[Serializable]
public class LibSQLConstraintException : LibSQLException
{
    /// <summary>
    /// Gets the type of constraint that was violated.
    /// </summary>
    public ConstraintType ConstraintType { get; }

    /// <summary>
    /// Gets the name of the constraint that was violated, if available.
    /// </summary>
    public string? ConstraintName { get; }

    /// <summary>
    /// Gets the table name where the constraint violation occurred, if available.
    /// </summary>
    public string? TableName { get; }

    /// <summary>
    /// Gets the column name where the constraint violation occurred, if available.
    /// </summary>
    public string? ColumnName { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="LibSQLConstraintException"/> class.
    /// </summary>
    public LibSQLConstraintException() : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LibSQLConstraintException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public LibSQLConstraintException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LibSQLConstraintException"/> class with a specified error message and constraint type.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="constraintType">The type of constraint that was violated.</param>
    public LibSQLConstraintException(string message, ConstraintType constraintType) : base(message)
    {
        ConstraintType = constraintType;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LibSQLConstraintException"/> class with full constraint details.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="constraintType">The type of constraint that was violated.</param>
    /// <param name="constraintName">The name of the constraint.</param>
    /// <param name="tableName">The table where the violation occurred.</param>
    /// <param name="columnName">The column where the violation occurred.</param>
    /// <param name="sqlStatement">The SQL statement that caused the violation.</param>
    public LibSQLConstraintException(
        string message, 
        ConstraintType constraintType,
        string? constraintName = null,
        string? tableName = null,
        string? columnName = null,
        string? sqlStatement = null) 
        : base(message, LibSQLErrorMessages.SQLITE_CONSTRAINT, sqlStatement: sqlStatement)
    {
        ConstraintType = constraintType;
        ConstraintName = constraintName;
        TableName = tableName;
        ColumnName = columnName;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LibSQLConstraintException"/> class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public LibSQLConstraintException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LibSQLConstraintException"/> class with serialized data.
    /// </summary>
    /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
    /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
    [Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
    protected LibSQLConstraintException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
        ConstraintType = (ConstraintType)info.GetValue(nameof(ConstraintType), typeof(ConstraintType))!;
        ConstraintName = info.GetString(nameof(ConstraintName));
        TableName = info.GetString(nameof(TableName));
        ColumnName = info.GetString(nameof(ColumnName));
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
        info.AddValue(nameof(ConstraintType), ConstraintType);
        info.AddValue(nameof(ConstraintName), ConstraintName);
        info.AddValue(nameof(TableName), TableName);
        info.AddValue(nameof(ColumnName), ColumnName);
    }
}

/// <summary>
/// Specifies the type of database constraint that was violated.
/// </summary>
public enum ConstraintType
{
    /// <summary>
    /// Unknown constraint type.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Primary key constraint violation.
    /// </summary>
    PrimaryKey,

    /// <summary>
    /// Unique constraint violation.
    /// </summary>
    Unique,

    /// <summary>
    /// Foreign key constraint violation.
    /// </summary>
    ForeignKey,

    /// <summary>
    /// Not null constraint violation.
    /// </summary>
    NotNull,

    /// <summary>
    /// Check constraint violation.
    /// </summary>
    Check,

    /// <summary>
    /// Row ID constraint violation.
    /// </summary>
    RowId
}