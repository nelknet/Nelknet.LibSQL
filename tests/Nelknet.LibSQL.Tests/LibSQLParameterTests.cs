#nullable disable warnings

using Nelknet.LibSQL.Data;
using System;
using System.Data;
using Xunit;

namespace Nelknet.LibSQL.Tests;

public class LibSQLParameterTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        var parameter = new LibSQLParameter();
        
        Assert.Equal(string.Empty, parameter.ParameterName);
        Assert.Null(parameter.Value);
        Assert.Equal(DbType.String, parameter.DbType);
        Assert.Equal(ParameterDirection.Input, parameter.Direction);
        Assert.True(parameter.IsNullable);
        Assert.Equal(0, parameter.Size);
        Assert.Equal(string.Empty, parameter.SourceColumn);
        Assert.False(parameter.SourceColumnNullMapping);
        Assert.Equal(DataRowVersion.Current, parameter.SourceVersion);
    }

    [Fact]
    public void Constructor_WithParameterName_ShouldSetName()
    {
        var parameter = new LibSQLParameter("@id");
        
        Assert.Equal("@id", parameter.ParameterName);
    }

    [Fact]
    public void Constructor_WithParameterNameAndValue_ShouldSetBoth()
    {
        var parameter = new LibSQLParameter("@id", 123);
        
        Assert.Equal("@id", parameter.ParameterName);
        Assert.Equal(123, parameter.Value);
    }

    [Fact]
    public void Constructor_WithParameterNameAndDbType_ShouldSetBoth()
    {
        var parameter = new LibSQLParameter("@id", DbType.Int32);
        
        Assert.Equal("@id", parameter.ParameterName);
        Assert.Equal(DbType.Int32, parameter.DbType);
    }

    [Fact]
    public void ParameterName_WhenSetToNull_ShouldBeEmptyString()
    {
        var parameter = new LibSQLParameter();
        
        parameter.ParameterName = null;
        
        Assert.Equal(string.Empty, parameter.ParameterName);
    }

    [Fact]
    public void Direction_WhenSetToOutput_ShouldThrowNotSupportedException()
    {
        var parameter = new LibSQLParameter();
        
        Assert.Throws<NotSupportedException>(() => parameter.Direction = ParameterDirection.Output);
    }

    [Fact]
    public void Direction_WhenSetToInputOutput_ShouldThrowNotSupportedException()
    {
        var parameter = new LibSQLParameter();
        
        Assert.Throws<NotSupportedException>(() => parameter.Direction = ParameterDirection.InputOutput);
    }

    [Fact]
    public void Direction_WhenSetToReturnValue_ShouldThrowNotSupportedException()
    {
        var parameter = new LibSQLParameter();
        
        Assert.Throws<NotSupportedException>(() => parameter.Direction = ParameterDirection.ReturnValue);
    }

    [Fact]
    public void Direction_WhenSetToInput_ShouldAcceptValue()
    {
        var parameter = new LibSQLParameter();
        
        parameter.Direction = ParameterDirection.Input;
        
        Assert.Equal(ParameterDirection.Input, parameter.Direction);
    }

    [Fact]
    public void Value_CanBeSetToNull()
    {
        var parameter = new LibSQLParameter("@test", 123);
        
        parameter.Value = null;
        
        Assert.Null(parameter.Value);
    }

    [Fact]
    public void Value_CanBeSetToDBNull()
    {
        var parameter = new LibSQLParameter("@test", 123);
        
        parameter.Value = DBNull.Value;
        
        Assert.Equal(DBNull.Value, parameter.Value);
    }

    [Fact]
    public void DbType_CanBeChanged()
    {
        var parameter = new LibSQLParameter();
        
        parameter.DbType = DbType.Int64;
        
        Assert.Equal(DbType.Int64, parameter.DbType);
    }

    [Fact]
    public void ResetDbType_ShouldResetToString()
    {
        var parameter = new LibSQLParameter();
        parameter.DbType = DbType.Int32;
        
        parameter.ResetDbType();
        
        Assert.Equal(DbType.String, parameter.DbType);
    }

    [Fact]
    public void IsNullable_CanBeChanged()
    {
        var parameter = new LibSQLParameter();
        
        parameter.IsNullable = false;
        
        Assert.False(parameter.IsNullable);
    }

    [Fact]
    public void Size_CanBeSet()
    {
        var parameter = new LibSQLParameter();
        
        parameter.Size = 255;
        
        Assert.Equal(255, parameter.Size);
    }

    [Fact]
    public void SourceColumn_CanBeSet()
    {
        var parameter = new LibSQLParameter();
        
        parameter.SourceColumn = "user_id";
        
        Assert.Equal("user_id", parameter.SourceColumn);
    }

    [Fact]
    public void SourceColumnNullMapping_CanBeChanged()
    {
        var parameter = new LibSQLParameter();
        
        parameter.SourceColumnNullMapping = true;
        
        Assert.True(parameter.SourceColumnNullMapping);
    }

    [Fact]
    public void SourceVersion_CanBeChanged()
    {
        var parameter = new LibSQLParameter();
        
        parameter.SourceVersion = DataRowVersion.Original;
        
        Assert.Equal(DataRowVersion.Original, parameter.SourceVersion);
    }
}