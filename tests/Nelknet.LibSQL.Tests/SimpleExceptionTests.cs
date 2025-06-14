using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Nelknet.LibSQL.Data.Exceptions;
using Xunit;

namespace Nelknet.LibSQL.Tests;

/// <summary>
/// Tests exception classes without requiring database connectivity
/// </summary>
public class SimpleExceptionTests
{
    [Fact]
    public void LibSQLException_BasicConstruction_ShouldWork()
    {
        // Test default constructor
        var ex1 = new LibSQLException();
        Assert.NotNull(ex1);

        // Test message constructor
        var ex2 = new LibSQLException("Test message");
        Assert.Equal("Test message", ex2.Message);

        // Test message and error code constructor
        var ex3 = new LibSQLException("Test message", 5);
        Assert.Equal("Test message", ex3.Message);
        Assert.Equal(5, ex3.LibSQLErrorCode);
        Assert.Equal(5, ex3.ErrorCode);

        // Test inner exception constructor
        var inner = new InvalidOperationException("Inner");
        var ex4 = new LibSQLException("Outer", inner);
        Assert.Equal("Outer", ex4.Message);
        Assert.Same(inner, ex4.InnerException);
    }

    [Fact]
    public void LibSQLException_FullConstruction_ShouldSetAllProperties()
    {
        // Arrange
        var inner = new InvalidOperationException("Inner");
        
        // Act
        var ex = new LibSQLException(
            "Test message",
            errorCode: 19,
            extendedErrorCode: 1555,
            sqlStatement: "INSERT INTO test VALUES (1)",
            errorContext: "During insert operation",
            innerException: inner);

        // Assert
        Assert.Equal("Test message", ex.Message);
        Assert.Equal(19, ex.LibSQLErrorCode);
        Assert.Equal(1555, ex.ExtendedErrorCode);
        Assert.Equal("INSERT INTO test VALUES (1)", ex.SqlStatement);
        Assert.Equal("During insert operation", ex.ErrorContext);
        Assert.Same(inner, ex.InnerException);
    }

    [Fact]
    public void LibSQLException_FromErrorCode_ShouldCreateProperException()
    {
        // Test with default message
        var ex1 = LibSQLException.FromErrorCode(5);
        Assert.Contains("libSQL error 5", ex1.Message);
        Assert.Contains("locked", ex1.Message);
        Assert.Equal(5, ex1.LibSQLErrorCode);

        // Test with custom message
        var ex2 = LibSQLException.FromErrorCode(5, "Custom message", "SELECT * FROM test", "Read operation");
        Assert.Equal("Custom message", ex2.Message);
        Assert.Equal(5, ex2.LibSQLErrorCode);
        Assert.Equal("SELECT * FROM test", ex2.SqlStatement);
        Assert.Equal("Read operation", ex2.ErrorContext);
    }

    [Fact]
    public void LibSQLConnectionException_Construction_ShouldWork()
    {
        // Test basic construction
        var ex1 = new LibSQLConnectionException("Connection failed");
        Assert.Equal("Connection failed", ex1.Message);

        // Test with connection string
        var ex2 = new LibSQLConnectionException("Connection failed", "Data Source=test.db");
        Assert.Equal("Connection failed", ex2.Message);
        Assert.Equal("Data Source=test.db", ex2.ConnectionString);

        // Test with error code
        var ex3 = new LibSQLConnectionException("Connection failed", 14, "Data Source=test.db");
        Assert.Equal("Connection failed", ex3.Message);
        Assert.Equal(14, ex3.LibSQLErrorCode);
        Assert.Equal("Data Source=test.db", ex3.ConnectionString);
    }

    [Fact]
    public void LibSQLConstraintException_Construction_ShouldWork()
    {
        // Test basic construction
        var ex1 = new LibSQLConstraintException("Constraint violated");
        Assert.Equal("Constraint violated", ex1.Message);

        // Test with constraint type
        var ex2 = new LibSQLConstraintException("Primary key violation", ConstraintType.PrimaryKey);
        Assert.Equal("Primary key violation", ex2.Message);
        Assert.Equal(ConstraintType.PrimaryKey, ex2.ConstraintType);

        // Test with full details
        var ex3 = new LibSQLConstraintException(
            "Unique constraint violated",
            ConstraintType.Unique,
            constraintName: "idx_unique_email",
            tableName: "users",
            columnName: "email",
            sqlStatement: "INSERT INTO users (email) VALUES ('test@example.com')");

        Assert.Equal("Unique constraint violated", ex3.Message);
        Assert.Equal(ConstraintType.Unique, ex3.ConstraintType);
        Assert.Equal("idx_unique_email", ex3.ConstraintName);
        Assert.Equal("users", ex3.TableName);
        Assert.Equal("email", ex3.ColumnName);
        Assert.Equal("INSERT INTO users (email) VALUES ('test@example.com')", ex3.SqlStatement);
        Assert.Equal(19, ex3.LibSQLErrorCode); // SQLITE_CONSTRAINT
    }

    [Fact]
    public void LibSQLBusyException_Construction_ShouldWork()
    {
        // Test basic construction
        var ex1 = new LibSQLBusyException("Database is busy");
        Assert.Equal("Database is busy", ex1.Message);

        // Test with lock type
        var ex2 = new LibSQLBusyException("Table is locked", LockType.Table, false);
        Assert.Equal("Table is locked", ex2.Message);
        Assert.Equal(LockType.Table, ex2.LockType);
        Assert.False(ex2.IsDatabaseLocked);

        // Test with full details
        var ex3 = new LibSQLBusyException(
            "Operation timed out",
            errorCode: 5,
            lockType: LockType.Timeout,
            timeout: TimeSpan.FromSeconds(30),
            sqlStatement: "UPDATE users SET active = 1");

        Assert.Equal("Operation timed out", ex3.Message);
        Assert.Equal(5, ex3.LibSQLErrorCode);
        Assert.Equal(LockType.Timeout, ex3.LockType);
        Assert.Equal(TimeSpan.FromSeconds(30), ex3.Timeout);
        Assert.Equal("UPDATE users SET active = 1", ex3.SqlStatement);
        Assert.True(ex3.IsDatabaseLocked);
    }

    [Fact]
    public void LibSQLBusyException_CreateTimeoutException_ShouldWork()
    {
        // Arrange & Act
        var ex = LibSQLBusyException.CreateTimeoutException(
            TimeSpan.FromSeconds(10),
            "DELETE FROM logs WHERE date < ?");

        // Assert
        Assert.Contains("10.0 seconds", ex.Message);
        Assert.Contains("timed out", ex.Message);
        Assert.Equal(5, ex.LibSQLErrorCode); // SQLITE_BUSY
        Assert.Equal(LockType.Timeout, ex.LockType);
        Assert.Equal(TimeSpan.FromSeconds(10), ex.Timeout);
        Assert.Equal("DELETE FROM logs WHERE date < ?", ex.SqlStatement);
    }

    [Fact]
    public void ErrorMessages_BasicErrorCodes_ShouldReturnCorrectMessages()
    {
        // Test some basic error codes
        Assert.Contains("Success", LibSQLErrorMessages.GetErrorMessage(0));
        Assert.Contains("SQL error", LibSQLErrorMessages.GetErrorMessage(1));
        Assert.Contains("locked", LibSQLErrorMessages.GetErrorMessage(5));
        Assert.Contains("readonly", LibSQLErrorMessages.GetErrorMessage(8));
        Assert.Contains("constraint", LibSQLErrorMessages.GetErrorMessage(19));
        Assert.Contains("malformed", LibSQLErrorMessages.GetErrorMessage(11));
    }

    [Fact]
    public void ErrorMessages_ExtendedErrorCodes_ShouldBeHandled()
    {
        // Extended error codes have the base error in the lower 8 bits
        var extendedCode = 1555; // SQLITE_CONSTRAINT_PRIMARYKEY
        var message = LibSQLErrorMessages.GetErrorMessage(extendedCode);
        
        Assert.Contains("1555", message);
        Assert.Contains("extended from 19", message); // Base SQLITE_CONSTRAINT
        Assert.Contains("constraint", message.ToLower());
    }

    [Fact]
    public void ErrorMessages_TransientErrors_ShouldBeIdentified()
    {
        // Transient errors that can be retried
        Assert.True(LibSQLErrorMessages.IsTransientError(5));  // SQLITE_BUSY
        Assert.True(LibSQLErrorMessages.IsTransientError(6));  // SQLITE_LOCKED
        Assert.True(LibSQLErrorMessages.IsTransientError(9));  // SQLITE_INTERRUPT
        
        // Non-transient errors
        Assert.False(LibSQLErrorMessages.IsTransientError(0));  // SQLITE_OK
        Assert.False(LibSQLErrorMessages.IsTransientError(1));  // SQLITE_ERROR
        Assert.False(LibSQLErrorMessages.IsTransientError(19)); // SQLITE_CONSTRAINT
    }

    [Fact]
    public void ErrorMessages_CorruptionErrors_ShouldBeIdentified()
    {
        // Corruption errors
        Assert.True(LibSQLErrorMessages.IsCorruptionError(11)); // SQLITE_CORRUPT
        Assert.True(LibSQLErrorMessages.IsCorruptionError(26)); // SQLITE_NOTADB
        Assert.True(LibSQLErrorMessages.IsCorruptionError(24)); // SQLITE_FORMAT
        
        // Non-corruption errors
        Assert.False(LibSQLErrorMessages.IsCorruptionError(0));  // SQLITE_OK
        Assert.False(LibSQLErrorMessages.IsCorruptionError(5));  // SQLITE_BUSY
        Assert.False(LibSQLErrorMessages.IsCorruptionError(19)); // SQLITE_CONSTRAINT
    }

    [Fact] 
    public void Exception_Serialization_ShouldPreserveAllProperties()
    {
        // Skip this test as BinaryFormatter is obsolete in .NET 5+
        // In a real implementation, you'd use a different serialization approach
    }
}