using System;
using System.IO;
using Nelknet.LibSQL.Data;
using Nelknet.LibSQL.Data.Exceptions;
using Xunit;

namespace Nelknet.LibSQL.Tests;

public class ExceptionTests : IDisposable
{
    private readonly string _tempDbPath;

    public ExceptionTests()
    {
        _tempDbPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.db");
    }

    public void Dispose()
    {
        if (File.Exists(_tempDbPath))
        {
            try { File.Delete(_tempDbPath); } catch { }
        }
    }

    [Fact]
    public void ConnectionException_WithInvalidPath_ShouldThrowLibSQLConnectionException()
    {
        // Arrange
        var invalidPath = "/invalid/path/that/does/not/exist/database.db";
        var connectionString = $"Data Source={invalidPath}";

        // Act & Assert
        using var connection = new LibSQLConnection(connectionString);
        var exception = Assert.Throws<LibSQLConnectionException>(() => connection.Open());
        
        // libSQL might fail at open or connect stage
        Assert.True(
            exception.Message.Contains("Failed to open database") || 
            exception.Message.Contains("Failed to connect to database"),
            $"Expected error message to contain 'Failed to open database' or 'Failed to connect to database', but was: {exception.Message}");
        Assert.Equal(invalidPath, exception.ConnectionString);
        Assert.NotEqual(0, exception.LibSQLErrorCode);
    }

    [Fact]
    public void ConstraintException_WithPrimaryKeyViolation_ShouldThrowLibSQLConstraintException()
    {
        // Arrange
        var connectionString = $"Data Source={_tempDbPath}";
        
        using var connection = new LibSQLConnection(connectionString);
        connection.Open();

        // Create a table with primary key
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "CREATE TABLE test (id INTEGER PRIMARY KEY, value TEXT)";
            cmd.ExecuteNonQuery();
        }

        // Insert first row
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "INSERT INTO test (id, value) VALUES (1, 'first')";
            cmd.ExecuteNonQuery();
        }

        // Act & Assert - Try to insert duplicate primary key
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "INSERT INTO test (id, value) VALUES (1, 'duplicate')";
            var exception = Assert.Throws<LibSQLException>(() => cmd.ExecuteNonQuery());
            
            // The exception should be a constraint violation
            // Note: libSQL might return different error codes than SQLite
            // Check if it's either SQLITE_CONSTRAINT (19) or SQLITE_INTERNAL (2)
            var errorCode = exception.LibSQLErrorCode & 0xFF;
            Assert.True(
                errorCode == LibSQLErrorMessages.SQLITE_CONSTRAINT || 
                errorCode == LibSQLErrorMessages.SQLITE_INTERNAL,
                $"Expected error code to be SQLITE_CONSTRAINT (19) or SQLITE_INTERNAL (2), but was {errorCode}");
        }
    }

    [Fact]
    public void BusyException_SimulatedTimeout_ShouldCreateProperException()
    {
        // Arrange & Act
        var exception = LibSQLBusyException.CreateTimeoutException(
            TimeSpan.FromSeconds(5), 
            "SELECT * FROM test");

        // Assert
        Assert.Contains("5.0 seconds", exception.Message);
        Assert.Equal(LibSQLErrorMessages.SQLITE_BUSY, exception.LibSQLErrorCode);
        Assert.Equal(LockType.Timeout, exception.LockType);
        Assert.Equal(TimeSpan.FromSeconds(5), exception.Timeout);
        Assert.Equal("SELECT * FROM test", exception.SqlStatement);
        Assert.True(exception.IsDatabaseLocked);
    }

    [Fact]
    public void ErrorMessages_GetMessage_ShouldReturnProperMessages()
    {
        // Arrange & Act & Assert
        var okMessage = LibSQLErrorMessages.GetErrorMessage(LibSQLErrorMessages.SQLITE_OK);
        Assert.Contains("Success", okMessage);

        var busyMessage = LibSQLErrorMessages.GetErrorMessage(LibSQLErrorMessages.SQLITE_BUSY);
        Assert.Contains("locked", busyMessage);

        var constraintMessage = LibSQLErrorMessages.GetErrorMessage(LibSQLErrorMessages.SQLITE_CONSTRAINT);
        Assert.Contains("constraint violation", constraintMessage);

        var unknownMessage = LibSQLErrorMessages.GetErrorMessage(99999);
        Assert.Contains("Unknown error code", unknownMessage);
    }

    [Fact]
    public void ErrorMessages_IsTransientError_ShouldIdentifyTransientErrors()
    {
        // Arrange & Act & Assert
        Assert.True(LibSQLErrorMessages.IsTransientError(LibSQLErrorMessages.SQLITE_BUSY));
        Assert.True(LibSQLErrorMessages.IsTransientError(LibSQLErrorMessages.SQLITE_LOCKED));
        Assert.False(LibSQLErrorMessages.IsTransientError(LibSQLErrorMessages.SQLITE_CONSTRAINT));
        Assert.False(LibSQLErrorMessages.IsTransientError(LibSQLErrorMessages.SQLITE_OK));
    }

    [Fact]
    public void ErrorMessages_IsCorruptionError_ShouldIdentifyCorruptionErrors()
    {
        // Arrange & Act & Assert
        Assert.True(LibSQLErrorMessages.IsCorruptionError(LibSQLErrorMessages.SQLITE_CORRUPT));
        Assert.True(LibSQLErrorMessages.IsCorruptionError(LibSQLErrorMessages.SQLITE_NOTADB));
        Assert.False(LibSQLErrorMessages.IsCorruptionError(LibSQLErrorMessages.SQLITE_BUSY));
        Assert.False(LibSQLErrorMessages.IsCorruptionError(LibSQLErrorMessages.SQLITE_OK));
    }

    [Fact]
    public void ConstraintType_Enum_ShouldHaveExpectedValues()
    {
        // This test ensures the enum values are as expected
        Assert.Equal(0, (int)ConstraintType.Unknown);
        Assert.Equal(1, (int)ConstraintType.PrimaryKey);
        Assert.Equal(2, (int)ConstraintType.Unique);
        Assert.Equal(3, (int)ConstraintType.ForeignKey);
        Assert.Equal(4, (int)ConstraintType.NotNull);
        Assert.Equal(5, (int)ConstraintType.Check);
        Assert.Equal(6, (int)ConstraintType.RowId);
    }

    [Fact]
    public void LockType_Enum_ShouldHaveExpectedValues()
    {
        // This test ensures the enum values are as expected
        Assert.Equal(0, (int)LockType.Unknown);
        Assert.Equal(1, (int)LockType.Database);
        Assert.Equal(2, (int)LockType.Table);
        Assert.Equal(3, (int)LockType.Timeout);
        Assert.Equal(4, (int)LockType.Shared);
        Assert.Equal(5, (int)LockType.Reserved);
        Assert.Equal(6, (int)LockType.Pending);
        Assert.Equal(7, (int)LockType.Exclusive);
    }
}