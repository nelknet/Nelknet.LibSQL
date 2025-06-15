using System;
using System.Linq;
using Xunit;
using Nelknet.LibSQL.Data;
using Nelknet.LibSQL.Data.Exceptions;

namespace Nelknet.LibSQL.Tests;

public class LibSQLCustomFunctionTests : IDisposable
{
    private readonly LibSQLConnection _connection;
    
    public LibSQLCustomFunctionTests()
    {
        _connection = new LibSQLConnection("Data Source=:memory:");
        _connection.Open();
    }
    
    public void Dispose()
    {
        _connection?.Dispose();
    }
    
    [Fact]
    public void RegisterFunction_WithScalarFunction_ExecutesCorrectly()
    {
        // Arrange
        var upperFunction = new UpperCaseFunction();
        _connection.RegisterFunction(upperFunction);
        
        // Act
        using var command = _connection.CreateCommand();
        command.CommandText = "SELECT UPPERCASE('hello world')";
        var result = command.ExecuteScalar();
        
        // Assert
        Assert.Equal("HELLO WORLD", result);
    }
    
    [Fact]
    public void RegisterFunction_WithDeterministicFunction_ExecutesCorrectly()
    {
        // Arrange
        var addFunction = new AddFunction();
        _connection.RegisterFunction(addFunction);
        
        // Act
        using var command = _connection.CreateCommand();
        command.CommandText = "SELECT ADD(5, 3)";
        var result = command.ExecuteScalar();
        
        // Assert
        Assert.Equal(8L, result);
    }
    
    [Fact]
    public void RegisterAggregate_WithCustomSum_CalculatesCorrectly()
    {
        // Arrange
        _connection.RegisterAggregate<CustomSumAggregate>();
        
        using (var command = _connection.CreateCommand())
        {
            command.CommandText = @"
                CREATE TABLE numbers (value INTEGER);
                INSERT INTO numbers VALUES (1), (2), (3), (4), (5);
            ";
            command.ExecuteNonQuery();
        }
        
        // Act
        using var selectCommand = _connection.CreateCommand();
        selectCommand.CommandText = "SELECT CUSTOM_SUM(value) FROM numbers";
        var result = selectCommand.ExecuteScalar();
        
        // Assert
        Assert.Equal(15.0, result);
    }
    
    [Fact]
    public void UnregisterFunction_RemovesFunction()
    {
        // Arrange
        var function = new UpperCaseFunction();
        _connection.RegisterFunction(function);
        
        // Verify it works first
        using (var command = _connection.CreateCommand())
        {
            command.CommandText = "SELECT UPPERCASE('test')";
            var result = command.ExecuteScalar();
            Assert.Equal("TEST", result);
        }
        
        // Act
        _connection.UnregisterFunction("UPPERCASE");
        
        // Assert
        using (var command = _connection.CreateCommand())
        {
            command.CommandText = "SELECT UPPERCASE('test')";
            Assert.Throws<LibSQLException>(() => command.ExecuteScalar());
        }
    }
    
    [Fact]
    public void RegisterFunction_WithNullHandling_WorksCorrectly()
    {
        // Arrange
        var function = new NullAwareFunction();
        _connection.RegisterFunction(function);
        
        // Act & Assert - with null
        using (var command = _connection.CreateCommand())
        {
            command.CommandText = "SELECT NULL_AWARE(NULL)";
            var result = command.ExecuteScalar();
            Assert.Equal("NULL VALUE", result);
        }
        
        // Act & Assert - with value
        using (var command = _connection.CreateCommand())
        {
            command.CommandText = "SELECT NULL_AWARE('test')";
            var result = command.ExecuteScalar();
            Assert.Equal("VALUE: test", result);
        }
    }
    
    // Test function implementations
    private class UpperCaseFunction : LibSQLFunction
    {
        public override string Name => "UPPERCASE";
        public override int ArgumentCount => 1;
        public override bool IsDeterministic => true;
        
        public override object? Invoke(object?[] args)
        {
            if (args.Length != 1 || args[0] == null)
                return null;
            
            return args[0]?.ToString()?.ToUpperInvariant();
        }
    }
    
    private class AddFunction : LibSQLFunction
    {
        public override string Name => "ADD";
        public override int ArgumentCount => 2;
        public override bool IsDeterministic => true;
        
        public override object? Invoke(object?[] args)
        {
            if (args.Length != 2)
                return null;
            
            if (args[0] is long a && args[1] is long b)
                return a + b;
            
            return null;
        }
    }
    
    private class NullAwareFunction : LibSQLFunction
    {
        public override string Name => "NULL_AWARE";
        public override int ArgumentCount => 1;
        
        public override object? Invoke(object?[] args)
        {
            if (args.Length != 1)
                return null;
            
            return args[0] == null ? "NULL VALUE" : $"VALUE: {args[0]}";
        }
    }
    
    private class CustomSumAggregate : LibSQLAggregate
    {
        private double _sum;
        
        public override string Name => "CUSTOM_SUM";
        public override int ArgumentCount => 1;
        
        public override void Reset()
        {
            _sum = 0;
        }
        
        public override void Step(object?[] args)
        {
            if (args.Length != 1 || args[0] == null)
                return;
            
            if (args[0] is long l)
                _sum += l;
            else if (args[0] is double d)
                _sum += d;
        }
        
        public override object? Final()
        {
            return _sum;
        }
    }
}