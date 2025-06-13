#nullable disable warnings

using Nelknet.LibSQL.Data;
using System;
using System.Data;
using Xunit;

namespace Nelknet.LibSQL.Tests;

public class LibSQLCommandTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        var command = new LibSQLCommand();
        
        Assert.Equal(string.Empty, command.CommandText);
        Assert.Equal(30, command.CommandTimeout);
        Assert.Equal(CommandType.Text, command.CommandType);
        Assert.Null(command.Connection);
        Assert.NotNull(command.Parameters);
        Assert.Equal(0, command.Parameters.Count);
    }

    [Fact]
    public void Constructor_WithCommandText_ShouldSetCommandText()
    {
        const string sql = "SELECT * FROM users";
        var command = new LibSQLCommand(sql);
        
        Assert.Equal(sql, command.CommandText);
    }

    [Fact]
    public void Constructor_WithCommandTextAndConnection_ShouldSetBoth()
    {
        const string sql = "SELECT * FROM users";
        var connection = new LibSQLConnection();
        var command = new LibSQLCommand(sql, connection);
        
        Assert.Equal(sql, command.CommandText);
        Assert.Same(connection, command.Connection);
    }

    [Fact]
    public void CommandText_WhenSetToNull_ShouldBeEmptyString()
    {
        var command = new LibSQLCommand();
        
        command.CommandText = null;
        
        Assert.Equal(string.Empty, command.CommandText);
    }

    [Fact]
    public void CommandTimeout_WhenSetToNegative_ShouldThrowArgumentOutOfRangeException()
    {
        var command = new LibSQLCommand();
        
        Assert.Throws<ArgumentOutOfRangeException>(() => command.CommandTimeout = -1);
    }

    [Fact]
    public void CommandTimeout_WhenSetToZero_ShouldAcceptValue()
    {
        var command = new LibSQLCommand();
        
        command.CommandTimeout = 0;
        
        Assert.Equal(0, command.CommandTimeout);
    }

    [Fact]
    public void Connection_WhenSet_ShouldUpdateProperty()
    {
        var command = new LibSQLCommand();
        var connection = new LibSQLConnection();
        
        command.Connection = connection;
        
        Assert.Same(connection, command.Connection);
    }

    [Fact]
    public void CreateParameter_ShouldReturnLibSQLParameter()
    {
        var command = new LibSQLCommand();
        
        var parameter = command.CreateParameter();
        
        Assert.IsType<LibSQLParameter>(parameter);
    }

    [Fact]
    public void Cancel_ShouldNotThrow()
    {
        var command = new LibSQLCommand();
        
        // Cancel is a no-op for libSQL, should not throw
        command.Cancel();
    }

    [Fact]
    public void ExecuteNonQuery_WithoutConnection_ShouldThrowInvalidOperationException()
    {
        var command = new LibSQLCommand("INSERT INTO test VALUES (1)");
        
        Assert.Throws<InvalidOperationException>(() => command.ExecuteNonQuery());
    }

    [Fact]
    public void ExecuteNonQuery_WithClosedConnection_ShouldThrowInvalidOperationException()
    {
        var connection = new LibSQLConnection("Data Source=:memory:");
        var command = new LibSQLCommand("INSERT INTO test VALUES (1)", connection);
        
        Assert.Throws<InvalidOperationException>(() => command.ExecuteNonQuery());
    }

    [Fact]
    public void ExecuteScalar_WithoutConnection_ShouldThrowInvalidOperationException()
    {
        var command = new LibSQLCommand("SELECT COUNT(*) FROM test");
        
        Assert.Throws<InvalidOperationException>(() => command.ExecuteScalar());
    }

    [Fact]
    public void ExecuteReader_WithoutConnection_ShouldThrowInvalidOperationException()
    {
        var command = new LibSQLCommand("SELECT * FROM test");
        
        Assert.Throws<InvalidOperationException>(() => command.ExecuteReader());
    }

    [Fact]
    public void Prepare_WithoutConnection_ShouldThrowInvalidOperationException()
    {
        var command = new LibSQLCommand("SELECT * FROM test WHERE id = ?");
        
        Assert.Throws<InvalidOperationException>(() => command.Prepare());
    }

    [Fact]
    public void Parameters_ShouldAllowAddingParameters()
    {
        var command = new LibSQLCommand();
        var parameter = new LibSQLParameter("@id", 123);
        
        command.Parameters.Add(parameter);
        
        Assert.Equal(1, command.Parameters.Count);
        Assert.Same(parameter, command.Parameters[0]);
    }

    [Fact]
    public void Parameters_AddWithValue_ShouldCreateAndAddParameter()
    {
        var command = new LibSQLCommand();
        
        var parameter = command.Parameters.AddWithValue("@name", "John");
        
        Assert.Equal(1, command.Parameters.Count);
        Assert.Equal("@name", parameter.ParameterName);
        Assert.Equal("John", parameter.Value);
    }
}