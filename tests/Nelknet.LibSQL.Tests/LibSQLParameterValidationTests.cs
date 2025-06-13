#nullable disable warnings

using Nelknet.LibSQL.Data;
using System;
using System.Data;
using Xunit;

namespace Nelknet.LibSQL.Tests;

public class LibSQLParameterValidationTests
{
    [Fact]
    public void Parameter_WithEmptyName_ShouldFailValidation()
    {
        var parameter = new LibSQLParameter(string.Empty, "value");
        
        var exception = Assert.Throws<InvalidOperationException>(() => parameter.Validate());
        Assert.Contains("Parameter name cannot be null or empty", exception.Message);
    }

    [Fact]
    public void Parameter_WithNullName_ShouldFailValidation()
    {
        var parameter = new LibSQLParameter();
        parameter.ParameterName = null;
        
        var exception = Assert.Throws<InvalidOperationException>(() => parameter.Validate());
        Assert.Contains("Parameter name cannot be null or empty", exception.Message);
    }

    [Fact]
    public void Parameter_WithInvalidNamePrefix_ShouldFailValidation()
    {
        var parameter = new LibSQLParameter("invalidname", "value");
        
        var exception = Assert.Throws<ArgumentException>(() => parameter.Validate());
        Assert.Contains("Parameter name must start with", exception.Message);
    }

    [Fact]
    public void Parameter_WithValidAtPrefix_ShouldPassValidation()
    {
        var parameter = new LibSQLParameter("@validname", "value");
        
        // Should not throw
        parameter.Validate();
    }

    [Fact]
    public void Parameter_WithValidColonPrefix_ShouldPassValidation()
    {
        var parameter = new LibSQLParameter(":validname", "value");
        
        // Should not throw
        parameter.Validate();
    }

    [Fact]
    public void Parameter_WithValidQuestionPrefix_ShouldPassValidation()
    {
        var parameter = new LibSQLParameter("?", "value");
        
        // Should not throw
        parameter.Validate();
    }

    [Fact]
    public void Parameter_PrecisionAndScale_CanBeSet()
    {
        var parameter = new LibSQLParameter("@decimal", 123.45m);
        
        parameter.Precision = 5;
        parameter.Scale = 2;
        
        Assert.Equal(5, parameter.Precision);
        Assert.Equal(2, parameter.Scale);
    }

    [Fact]
    public void ParameterCollection_AddWithDbType_ShouldCreateCorrectParameter()
    {
        var collection = new LibSQLParameterCollection();
        
        var parameter = collection.AddWithValue("@test", DbType.Int64, 123L);
        
        Assert.Equal("@test", parameter.ParameterName);
        Assert.Equal(DbType.Int64, parameter.DbType);
        Assert.Equal(123L, parameter.Value);
    }

    [Fact]
    public void ParameterCollection_AddRange_ShouldAddMultipleParameters()
    {
        var collection = new LibSQLParameterCollection();
        var parameters = new[]
        {
            new LibSQLParameter("@param1", 1),
            new LibSQLParameter("@param2", "test"),
            new LibSQLParameter("@param3", true)
        };
        
        collection.AddRange(parameters);
        
        Assert.Equal(3, collection.Count);
        Assert.Equal("@param1", collection[0].ParameterName);
        Assert.Equal("@param2", collection[1].ParameterName);
        Assert.Equal("@param3", collection[2].ParameterName);
    }

    [Fact]
    public void ParameterCollection_AddRangeWithNull_ShouldThrowArgumentNullException()
    {
        var collection = new LibSQLParameterCollection();
        
        Assert.Throws<ArgumentNullException>(() => collection.AddRange((LibSQLParameter[])null));
    }

    [Fact]
    public void ParameterCollection_ValidateParameters_WithDuplicateNames_ShouldThrow()
    {
        var collection = new LibSQLParameterCollection();
        collection.Add(new LibSQLParameter("@test", 1));
        collection.Add(new LibSQLParameter("@TEST", 2)); // Same name, different case
        
        var exception = Assert.Throws<InvalidOperationException>(() => collection.ValidateParameters());
        Assert.Contains("Duplicate parameter name", exception.Message);
    }

    [Fact]
    public void ParameterCollection_ValidateParameters_WithInvalidParameterNames_ShouldThrow()
    {
        var collection = new LibSQLParameterCollection();
        collection.Add(new LibSQLParameter("invalidname", 1));
        
        var exception = Assert.Throws<ArgumentException>(() => collection.ValidateParameters());
        Assert.Contains("Parameter name must start with", exception.Message);
    }

    [Fact]
    public void ParameterCollection_ValidateParameters_WithValidParameters_ShouldPass()
    {
        var collection = new LibSQLParameterCollection();
        collection.Add(new LibSQLParameter("@param1", 1));
        collection.Add(new LibSQLParameter(":param2", "test"));
        collection.Add(new LibSQLParameter("?", true));
        
        // Should not throw
        collection.ValidateParameters();
    }

    [Fact]
    public void ParameterCollection_IndexOfWithNullOrEmpty_ShouldReturnMinusOne()
    {
        var collection = new LibSQLParameterCollection();
        collection.Add(new LibSQLParameter("@test", 1));
        
        Assert.Equal(-1, collection.IndexOf((string)null));
        Assert.Equal(-1, collection.IndexOf(string.Empty));
    }

#if NET6_0_OR_GREATER
    [Fact]
    public void Parameter_WithDateOnlyValue_ShouldInferDateType()
    {
        var dateOnly = new DateOnly(2023, 12, 25);
        var parameter = new LibSQLParameter("@date", dateOnly);
        
        Assert.Equal(DbType.Date, parameter.DbType);
    }

    [Fact]
    public void Parameter_WithTimeOnlyValue_ShouldInferTimeType()
    {
        var timeOnly = new TimeOnly(14, 30, 0);
        var parameter = new LibSQLParameter("@time", timeOnly);
        
        Assert.Equal(DbType.Time, parameter.DbType);
    }
#endif

    [Fact]
    public void Parameter_WithObjectValue_ShouldInferObjectType()
    {
        var customObject = new { Name = "Test", Value = 123 };
        var parameter = new LibSQLParameter("@object", customObject);
        
        Assert.Equal(DbType.Object, parameter.DbType);
    }

    [Fact]
    public void Command_ParameterValidation_ShouldValidateBeforeExecution()
    {
        using var command = new LibSQLCommand("SELECT @value");
        
        // Add a parameter with invalid name
        command.Parameters.Add(new LibSQLParameter("invalidname", 42));
        
        // Test that validation happens when we call ValidateParameters directly
        var exception = Assert.Throws<ArgumentException>(() => command.Parameters.ValidateParameters());
        Assert.Contains("Parameter name must start with", exception.Message);
    }
}