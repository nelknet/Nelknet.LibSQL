#nullable disable warnings

using Nelknet.LibSQL.Data;
using System;
using System.Reflection;
using Xunit;

namespace Nelknet.LibSQL.Tests;

public class LibSQLConnectionStringTests
{
    [Fact]
    public void Parse_WithValidFileDataSource_ShouldReturnCorrectValues()
    {
        const string connectionString = "Data Source=test.db";
        
        var parsed = ParseConnectionString(connectionString);
        
        Assert.Equal("test.db", parsed.DataSource);
        Assert.True(parsed.IsFile);
        Assert.False(parsed.IsRemote);
        Assert.False(parsed.IsInMemory);
        Assert.Null(parsed.AuthToken);
        Assert.False(parsed.WithWebPKI);
    }

    [Fact]
    public void Parse_WithInMemoryDataSource_ShouldReturnCorrectValues()
    {
        const string connectionString = "Data Source=:memory:";
        
        var parsed = ParseConnectionString(connectionString);
        
        Assert.Equal(":memory:", parsed.DataSource);
        Assert.False(parsed.IsFile);
        Assert.False(parsed.IsRemote);
        Assert.True(parsed.IsInMemory);
    }

    [Fact]
    public void Parse_WithRemoteLibSQLUrl_ShouldReturnCorrectValues()
    {
        const string connectionString = "Data Source=libsql://example.com/mydb;Auth Token=secret123";
        
        var parsed = ParseConnectionString(connectionString);
        
        Assert.Equal("libsql://example.com/mydb", parsed.DataSource);
        Assert.False(parsed.IsFile);
        Assert.True(parsed.IsRemote);
        Assert.False(parsed.IsInMemory);
        Assert.Equal("secret123", parsed.AuthToken);
    }

    [Fact]
    public void Parse_WithRemoteHttpsUrl_ShouldReturnCorrectValues()
    {
        const string connectionString = "Data Source=https://example.com/mydb;AuthToken=secret123";
        
        var parsed = ParseConnectionString(connectionString);
        
        Assert.Equal("https://example.com/mydb", parsed.DataSource);
        Assert.False(parsed.IsFile);
        Assert.True(parsed.IsRemote);
        Assert.False(parsed.IsInMemory);
        Assert.Equal("secret123", parsed.AuthToken);
    }

    [Fact]
    public void Parse_WithWebPKI_ShouldReturnCorrectValues()
    {
        const string connectionString = "Data Source=libsql://example.com/mydb;Auth Token=secret123;With WebPKI=true";
        
        var parsed = ParseConnectionString(connectionString);
        
        Assert.True(parsed.WithWebPKI);
    }

    [Fact]
    public void Parse_WithWebPKIVariations_ShouldAcceptDifferentFormats()
    {
        var testCases = new[]
        {
            ("With WebPKI=1", true),
            ("With WebPKI=yes", true),
            ("With WebPKI=y", true),
            ("With WebPKI=true", true),
            ("With WebPKI=0", false),
            ("With WebPKI=no", false),
            ("With WebPKI=n", false),
            ("With WebPKI=false", false),
            ("WithWebPKI=true", true) // Test alias
        };

        foreach (var (webPkiValue, expected) in testCases)
        {
            var connectionString = $"Data Source=libsql://example.com/mydb;{webPkiValue}";
            var parsed = ParseConnectionString(connectionString);
            Assert.Equal(expected, parsed.WithWebPKI);
        }
    }

    [Fact]
    public void Parse_WithQuotedValues_ShouldHandleQuotes()
    {
        const string connectionString = "Data Source=\"my test.db\";Auth Token='secret with spaces'";
        
        var parsed = ParseConnectionString(connectionString);
        
        Assert.Equal("my test.db", parsed.DataSource);
        Assert.Equal("secret with spaces", parsed.AuthToken);
    }

    [Fact]
    public void Parse_WithEmptyConnectionString_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() => LibSQLConnectionString.Parse(""));
        Assert.Throws<ArgumentException>(() => LibSQLConnectionString.Parse("   "));
        Assert.Throws<ArgumentException>(() => LibSQLConnectionString.Parse(null));
    }

    [Fact]
    public void Parse_WithoutDataSource_ShouldThrowArgumentException()
    {
        const string connectionString = "Auth Token=secret123";
        
        Assert.Throws<ArgumentException>(() => LibSQLConnectionString.Parse(connectionString));
    }

    [Fact]
    public void Parse_WithEmptyDataSource_ShouldThrowArgumentException()
    {
        const string connectionString = "Data Source=;Auth Token=secret123";
        
        Assert.Throws<ArgumentException>(() => LibSQLConnectionString.Parse(connectionString));
    }

    [Fact]
    public void Parse_WithComplexConnectionString_ShouldParseAllParameters()
    {
        const string connectionString = "Data Source=libsql://example.com:8080/mydb;Auth Token=abc123xyz;With WebPKI=true";
        
        var parsed = ParseConnectionString(connectionString);
        
        Assert.Equal("libsql://example.com:8080/mydb", parsed.DataSource);
        Assert.Equal("abc123xyz", parsed.AuthToken);
        Assert.True(parsed.WithWebPKI);
        Assert.True(parsed.IsRemote);
    }

    /// <summary>
    /// Helper method to parse connection string
    /// </summary>
    private static LibSQLConnectionString ParseConnectionString(string connectionString)
    {
        return LibSQLConnectionString.Parse(connectionString);
    }
}