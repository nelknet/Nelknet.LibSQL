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

    // --- Regression tests for issue #65: BindParameters must resolve by name, not by collection order ---

    [Fact]
    public void ExecuteScalar_NamedParametersAddedOutOfOrder_BindsByName()
    {
        using var connection = new LibSQLConnection("Data Source=:memory:");

        try
        {
            connection.Open();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Failed to load libSQL native library"))
        {
            return;
        }

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT @a || '|' || @b";

        var pB = cmd.CreateParameter();
        pB.ParameterName = "@b";
        pB.Value = "BEE";
        cmd.Parameters.Add(pB);

        var pA = cmd.CreateParameter();
        pA.ParameterName = "@a";
        pA.Value = "AYE";
        cmd.Parameters.Add(pA);

        var result = cmd.ExecuteScalar();

        Assert.Equal("AYE|BEE", result);
    }

    [Fact]
    public void ExecuteScalar_RepeatedNamedParameter_BindsSingleValueToAllOccurrences()
    {
        using var connection = new LibSQLConnection("Data Source=:memory:");

        try
        {
            connection.Open();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Failed to load libSQL native library"))
        {
            return;
        }

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT @v || '-' || @v";
        cmd.Parameters.AddWithValue("@v", "X");

        var result = cmd.ExecuteScalar();

        Assert.Equal("X-X", result);
    }

    [Fact]
    public void ExecuteScalar_ParameterMarkerInStringLiteral_NotTreatedAsParameter()
    {
        using var connection = new LibSQLConnection("Data Source=:memory:");

        try
        {
            connection.Open();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Failed to load libSQL native library"))
        {
            return;
        }

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT '@a=' || @a";
        cmd.Parameters.AddWithValue("@a", "42");

        var result = cmd.ExecuteScalar();

        Assert.Equal("@a=42", result);
    }

    [Fact]
    public void ExecuteScalar_ParameterMarkerInBlockComment_NotTreatedAsParameter()
    {
        using var connection = new LibSQLConnection("Data Source=:memory:");

        try
        {
            connection.Open();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Failed to load libSQL native library"))
        {
            return;
        }

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT /* @ignored */ @a";
        cmd.Parameters.AddWithValue("@a", 7);

        var result = cmd.ExecuteScalar();

        Assert.Equal(7L, result);
    }

    [Fact]
    public void ExecuteNonQuery_UpdateWithParametersAddedOutOfOrder_AppliesCorrectValues()
    {
        using var connection = new LibSQLConnection("Data Source=:memory:");

        try
        {
            connection.Open();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Failed to load libSQL native library"))
        {
            return;
        }

        using (var create = connection.CreateCommand())
        {
            create.CommandText = "CREATE TABLE t (id INTEGER PRIMARY KEY, name TEXT)";
            create.ExecuteNonQuery();
        }
        using (var insert = connection.CreateCommand())
        {
            insert.CommandText = "INSERT INTO t (id, name) VALUES (1, 'old')";
            insert.ExecuteNonQuery();
        }

        using var update = connection.CreateCommand();
        update.CommandText = "UPDATE t SET name = @name WHERE id = @id";
        // Parameters intentionally added in reverse order relative to how they appear in the SQL.
        update.Parameters.AddWithValue("@id", 1);
        update.Parameters.AddWithValue("@name", "updated");

        var affected = update.ExecuteNonQuery();
        Assert.Equal(1, affected);

        using var verify = connection.CreateCommand();
        verify.CommandText = "SELECT name FROM t WHERE id = 1";
        var name = verify.ExecuteScalar() as string;

        Assert.Equal("updated", name);
    }

    [Fact]
    public void ExecuteScalar_NamedParameterInCollectionNotReferencedInSql_SilentlyIgnored()
    {
        using var connection = new LibSQLConnection("Data Source=:memory:");

        try
        {
            connection.Open();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Failed to load libSQL native library"))
        {
            return;
        }

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT @a";
        cmd.Parameters.AddWithValue("@a", 1);
        cmd.Parameters.AddWithValue("@unused", 999);

        var result = cmd.ExecuteScalar();

        Assert.Equal(1L, result);
    }

    [Fact]
    public void ExecuteScalar_NamedParameterCaseInsensitiveMatch_Binds()
    {
        using var connection = new LibSQLConnection("Data Source=:memory:");

        try
        {
            connection.Open();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Failed to load libSQL native library"))
        {
            return;
        }

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT @Value";
        cmd.Parameters.AddWithValue("@value", 123);

        var result = cmd.ExecuteScalar();

        Assert.Equal(123L, result);
    }

    [Fact]
    public void ExecuteScalar_MixedNumberedAndNamedParameters_BindsCorrectPositions()
    {
        using var connection = new LibSQLConnection("Data Source=:memory:");

        try
        {
            connection.Open();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Failed to load libSQL native library"))
        {
            return;
        }

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT ?1 || '|' || @b";
        cmd.Parameters.AddWithValue("?1", "first");
        cmd.Parameters.AddWithValue("@b", "second");

        var result = cmd.ExecuteScalar();

        Assert.Equal("first|second", result);
    }

    [Fact]
    public void ExecuteScalar_DollarNamedParameter_Binds()
    {
        using var connection = new LibSQLConnection("Data Source=:memory:");

        try
        {
            connection.Open();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Failed to load libSQL native library"))
        {
            return;
        }

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT $value";
        cmd.Parameters.AddWithValue("$value", "bound");

        var result = cmd.ExecuteScalar();

        Assert.Equal("bound", result);
    }

    [Fact]
    public void ExecuteScalar_MissingNamedParameter_Throws()
    {
        using var connection = new LibSQLConnection("Data Source=:memory:");

        try
        {
            connection.Open();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Failed to load libSQL native library"))
        {
            return;
        }

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT @a || @missing";
        cmd.Parameters.AddWithValue("@a", "AYE");

        var exception = Assert.Throws<InvalidOperationException>(() => cmd.ExecuteScalar());

        Assert.Contains("@missing", exception.Message, StringComparison.Ordinal);
    }
}
