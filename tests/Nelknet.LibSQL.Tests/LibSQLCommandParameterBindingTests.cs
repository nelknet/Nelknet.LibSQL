#nullable disable warnings

using Nelknet.LibSQL.Data;
using System;
using System.Data;
using Xunit;

namespace Nelknet.LibSQL.Tests;

public class LibSQLCommandParameterBindingTests
{
    [Fact]
    public void Parameters_AddParameter_ShouldIncreaseCount()
    {
        using var command = new LibSQLCommand();
        var parameter = new LibSQLParameter("@test", "value");
        parameter.DbType = DbType.String;
        
        command.Parameters.Add(parameter);
        
        Assert.Equal(1, command.Parameters.Count);
        Assert.Same(parameter, command.Parameters[0]);
    }

    [Fact]
    public void Parameters_AddMultipleParameters_ShouldMaintainOrder()
    {
        using var command = new LibSQLCommand();
        var param1 = new LibSQLParameter("@param1", 1) { DbType = DbType.Int32 };
        var param2 = new LibSQLParameter("@param2", "test") { DbType = DbType.String };
        var param3 = new LibSQLParameter("@param3", 3.14) { DbType = DbType.Double };
        
        command.Parameters.Add(param1);
        command.Parameters.Add(param2);
        command.Parameters.Add(param3);
        
        Assert.Equal(3, command.Parameters.Count);
        Assert.Same(param1, command.Parameters[0]);
        Assert.Same(param2, command.Parameters[1]);
        Assert.Same(param3, command.Parameters[2]);
    }

    [Fact]
    public void Parameters_AddParameterWithValue_ShouldSetCorrectProperties()
    {
        using var command = new LibSQLCommand();
        
        var parameter = command.Parameters.AddWithValue("@test", "hello world");
        
        Assert.Equal("@test", parameter.ParameterName);
        Assert.Equal("hello world", parameter.Value);
        Assert.Equal(DbType.String, parameter.DbType);
    }

    [Fact]
    public void Parameters_AddNullParameter_ShouldHandleNullValue()
    {
        using var command = new LibSQLCommand();
        
        var parameter = command.Parameters.AddWithValue("@null", null);
        
        Assert.Equal("@null", parameter.ParameterName);
        Assert.Null(parameter.Value);
    }

    [Fact]
    public void Parameters_AddDBNullParameter_ShouldHandleDBNull()
    {
        using var command = new LibSQLCommand();
        
        var parameter = command.Parameters.AddWithValue("@dbnull", DBNull.Value);
        
        Assert.Equal("@dbnull", parameter.ParameterName);
        Assert.Same(DBNull.Value, parameter.Value);
    }

    [Fact]
    public void Parameters_AddIntegerParameter_ShouldSetCorrectType()
    {
        using var command = new LibSQLCommand();
        
        var parameter = command.Parameters.AddWithValue("@int", 42);
        
        Assert.Equal("@int", parameter.ParameterName);
        Assert.Equal(42, parameter.Value);
        Assert.Equal(DbType.Int32, parameter.DbType);
    }

    [Fact]
    public void Parameters_AddLongParameter_ShouldSetCorrectType()
    {
        using var command = new LibSQLCommand();
        
        var parameter = command.Parameters.AddWithValue("@long", 9223372036854775807L);
        
        Assert.Equal("@long", parameter.ParameterName);
        Assert.Equal(9223372036854775807L, parameter.Value);
        Assert.Equal(DbType.Int64, parameter.DbType);
    }

    [Fact]
    public void Parameters_AddDoubleParameter_ShouldSetCorrectType()
    {
        using var command = new LibSQLCommand();
        
        var parameter = command.Parameters.AddWithValue("@double", 3.14159);
        
        Assert.Equal("@double", parameter.ParameterName);
        Assert.Equal(3.14159, parameter.Value);
        Assert.Equal(DbType.Double, parameter.DbType);
    }

    [Fact]
    public void Parameters_AddDecimalParameter_ShouldSetCorrectType()
    {
        using var command = new LibSQLCommand();
        
        var parameter = command.Parameters.AddWithValue("@decimal", 123.45m);
        
        Assert.Equal("@decimal", parameter.ParameterName);
        Assert.Equal(123.45m, parameter.Value);
        Assert.Equal(DbType.Decimal, parameter.DbType);
    }

    [Fact]
    public void Parameters_AddBooleanParameter_ShouldSetCorrectType()
    {
        using var command = new LibSQLCommand();
        
        var parameter = command.Parameters.AddWithValue("@bool", true);
        
        Assert.Equal("@bool", parameter.ParameterName);
        Assert.Equal(true, parameter.Value);
        Assert.Equal(DbType.Boolean, parameter.DbType);
    }

    [Fact]
    public void Parameters_AddDateTimeParameter_ShouldSetCorrectType()
    {
        using var command = new LibSQLCommand();
        var dateTime = new DateTime(2023, 12, 25, 10, 30, 0);
        
        var parameter = command.Parameters.AddWithValue("@datetime", dateTime);
        
        Assert.Equal("@datetime", parameter.ParameterName);
        Assert.Equal(dateTime, parameter.Value);
        Assert.Equal(DbType.DateTime, parameter.DbType);
    }

    [Fact]
    public void Parameters_AddByteArrayParameter_ShouldSetCorrectType()
    {
        using var command = new LibSQLCommand();
        var bytes = new byte[] { 1, 2, 3, 4, 5 };
        
        var parameter = command.Parameters.AddWithValue("@bytes", bytes);
        
        Assert.Equal("@bytes", parameter.ParameterName);
        Assert.Same(bytes, parameter.Value);
        Assert.Equal(DbType.Binary, parameter.DbType);
    }

    [Fact]
    public void Parameters_ClearParameters_ShouldRemoveAllParameters()
    {
        using var command = new LibSQLCommand();
        command.Parameters.AddWithValue("@param1", 1);
        command.Parameters.AddWithValue("@param2", "test");
        
        command.Parameters.Clear();
        
        Assert.Empty(command.Parameters);
    }

    [Fact]
    public void Parameters_RemoveParameter_ShouldDecreaseCount()
    {
        using var command = new LibSQLCommand();
        var param1 = command.Parameters.AddWithValue("@param1", 1);
        var param2 = command.Parameters.AddWithValue("@param2", "test");
        
        command.Parameters.Remove(param1);
        
        Assert.Equal(1, command.Parameters.Count);
        Assert.Same(param2, command.Parameters[0]);
    }

    [Fact]
    public void Parameters_RemoveParameterByName_ShouldRemoveCorrectParameter()
    {
        using var command = new LibSQLCommand();
        command.Parameters.AddWithValue("@param1", 1);
        var param2 = command.Parameters.AddWithValue("@param2", "test");
        
        command.Parameters.RemoveAt("@param1");
        
        Assert.Equal(1, command.Parameters.Count);
        Assert.Same(param2, command.Parameters[0]);
    }

    [Fact]
    public void Parameters_ContainsParameter_ShouldReturnTrue()
    {
        using var command = new LibSQLCommand();
        var parameter = command.Parameters.AddWithValue("@test", "value");
        
        Assert.True(command.Parameters.Contains(parameter));
        Assert.True(command.Parameters.Contains("@test"));
    }

    [Fact]
    public void Parameters_IndexOfParameter_ShouldReturnCorrectIndex()
    {
        using var command = new LibSQLCommand();
        var param1 = command.Parameters.AddWithValue("@param1", 1);
        var param2 = command.Parameters.AddWithValue("@param2", "test");
        
        Assert.Equal(0, command.Parameters.IndexOf(param1));
        Assert.Equal(1, command.Parameters.IndexOf(param2));
        Assert.Equal(0, command.Parameters.IndexOf("@param1"));
        Assert.Equal(1, command.Parameters.IndexOf("@param2"));
    }

    [Fact]
    public void Parameters_AccessByName_ShouldReturnCorrectParameter()
    {
        using var command = new LibSQLCommand();
        var parameter = command.Parameters.AddWithValue("@test", "value");
        
        Assert.Same(parameter, command.Parameters["@test"]);
    }

    [Fact]
    public void Parameters_AccessByIndex_ShouldReturnCorrectParameter()
    {
        using var command = new LibSQLCommand();
        var param1 = command.Parameters.AddWithValue("@param1", 1);
        var param2 = command.Parameters.AddWithValue("@param2", "test");
        
        Assert.Same(param1, command.Parameters[0]);
        Assert.Same(param2, command.Parameters[1]);
    }

    [Fact]
    public void CreateParameter_ShouldReturnNewLibSQLParameter()
    {
        using var command = new LibSQLCommand();
        
        var parameter = command.CreateParameter();
        
        Assert.IsType<LibSQLParameter>(parameter);
        Assert.Empty(parameter.ParameterName);
        Assert.Null(parameter.Value);
    }

    [Fact]
    public void Parameters_TypedAccess_ShouldWorkCorrectly()
    {
        using var command = new LibSQLCommand();
        var parameter = new LibSQLParameter();
        
        command.Parameters.Add(parameter);
        
        // Test typed access through the property
        Assert.Same(parameter, command.Parameters[0]);
        
        // Test access through the base collection
        var dbParameterCollection = command.Parameters as System.Data.Common.DbParameterCollection;
        Assert.Same(parameter, dbParameterCollection[0]);
    }

    [Fact]
    public void Command_WithParameterizedQuery_ShouldHandleParameterValidation()
    {
        using var connection = new LibSQLConnection("Data Source=:memory:");
        using var command = new LibSQLCommand("SELECT * FROM users WHERE id = @id AND name = @name", connection);
        
        command.Parameters.AddWithValue("@id", 1);
        command.Parameters.AddWithValue("@name", "John");
        
        Assert.Equal(2, command.Parameters.Count);
        Assert.Equal("@id", command.Parameters[0].ParameterName);
        Assert.Equal(1, command.Parameters[0].Value);
        Assert.Equal("@name", command.Parameters[1].ParameterName);
        Assert.Equal("John", command.Parameters[1].Value);
    }

    [Fact]
    public void Command_ExecuteWithParameters_ShouldValidateCommandText()
    {
        using var connection = new LibSQLConnection("Data Source=:memory:");
        using var command = new LibSQLCommand("SELECT @value", connection);
        
        command.Parameters.AddWithValue("@value", 42);
        
        // Command should validate that connection is open before attempting to bind parameters
        var exception = Assert.Throws<InvalidOperationException>(() => command.ExecuteNonQuery());
        Assert.Contains("Connection must be open", exception.Message);
    }
}