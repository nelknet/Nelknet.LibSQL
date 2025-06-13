using Nelknet.LibSQL.Data;
using Xunit;

namespace Nelknet.LibSQL.Tests;

public class LibSQLConnectionStringBuilderTests
{
    [Fact]
    public void Constructor_DefaultValues_ShouldBeSet()
    {
        // Arrange & Act
        var builder = new LibSQLConnectionStringBuilder();

        // Assert
        Assert.Null(builder.DataSource);
        Assert.Null(builder.AuthToken);
        Assert.Null(builder.EncryptionKey);
        Assert.Null(builder.SyncUrl);
        Assert.Null(builder.SyncAuthToken);
        Assert.True(builder.ReadYourWrites);
        Assert.Equal(LibSQLConnectionMode.Local, builder.Mode);
    }

    [Theory]
    [InlineData("test.db")]
    [InlineData(":memory:")]
    [InlineData("/path/to/database.db")]
    public void DataSource_LocalFile_ShouldSetModeToLocal(string dataSource)
    {
        // Arrange
        var builder = new LibSQLConnectionStringBuilder();

        // Act
        builder.DataSource = dataSource;

        // Assert
        Assert.Equal(dataSource, builder.DataSource);
        Assert.Equal(LibSQLConnectionMode.Local, builder.Mode);
    }

    [Theory]
    [InlineData("http://example.com/db")]
    [InlineData("https://example.com/db")]
    [InlineData("libsql://example.com/db")]
    public void DataSource_RemoteUrl_ShouldSetModeToRemote(string dataSource)
    {
        // Arrange
        var builder = new LibSQLConnectionStringBuilder();

        // Act
        builder.DataSource = dataSource;

        // Assert
        Assert.Equal(dataSource, builder.DataSource);
        Assert.Equal(LibSQLConnectionMode.Remote, builder.Mode);
    }

    [Fact]
    public void SyncUrl_WithLocalDataSource_ShouldSetModeToEmbeddedReplica()
    {
        // Arrange
        var builder = new LibSQLConnectionStringBuilder
        {
            DataSource = "local.db"
        };

        // Act
        builder.SyncUrl = "https://sync.example.com";

        // Assert
        Assert.Equal(LibSQLConnectionMode.EmbeddedReplica, builder.Mode);
    }

    [Fact]
    public void ConnectionString_Parse_ShouldSetProperties()
    {
        // Arrange
        var connectionString = "Data Source=test.db;Auth Token=mytoken;Encryption Key=mykey";

        // Act
        var builder = new LibSQLConnectionStringBuilder(connectionString);

        // Assert
        Assert.Equal("test.db", builder.DataSource);
        Assert.Equal("mytoken", builder.AuthToken);
        Assert.Equal("mykey", builder.EncryptionKey);
    }

    [Theory]
    [InlineData("DataSource", "test.db")]
    [InlineData("Database", "test.db")]
    [InlineData("DB", "test.db")]
    [InlineData("Uri", "test.db")]
    [InlineData("Url", "test.db")]
    public void DataSource_Aliases_ShouldWork(string keyword, string value)
    {
        // Arrange
        var builder = new LibSQLConnectionStringBuilder();

        // Act
        builder[keyword] = value;

        // Assert
        Assert.Equal(value, builder.DataSource);
    }

    [Theory]
    [InlineData("AuthToken", "token123")]
    [InlineData("Token", "token123")]
    public void AuthToken_Aliases_ShouldWork(string keyword, string value)
    {
        // Arrange
        var builder = new LibSQLConnectionStringBuilder();

        // Act
        builder[keyword] = value;

        // Assert
        Assert.Equal(value, builder.AuthToken);
    }

    [Fact]
    public void InvalidKeyword_ShouldThrowArgumentException()
    {
        // Arrange
        var builder = new LibSQLConnectionStringBuilder();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => builder["InvalidKeyword"] = "value");
    }

    [Fact]
    public void CreateInMemoryConnectionString_ShouldReturnCorrectString()
    {
        // Act
        var connectionString = LibSQLConnectionStringBuilder.CreateInMemoryConnectionString();

        // Assert
        Assert.Equal("Data Source=:memory:", connectionString);
    }

    [Fact]
    public void CreateSharedMemoryConnectionString_ShouldReturnCorrectString()
    {
        // Act
        var connectionString = LibSQLConnectionStringBuilder.CreateSharedMemoryConnectionString();

        // Assert
        Assert.Equal("Data Source=:memory:?cache=shared", connectionString);
    }

    [Fact]
    public void Clear_ShouldResetAllProperties()
    {
        // Arrange
        var builder = new LibSQLConnectionStringBuilder
        {
            DataSource = "test.db",
            AuthToken = "token",
            EncryptionKey = "key",
            SyncUrl = "https://sync.example.com",
            SyncAuthToken = "synctoken",
            ReadYourWrites = false
        };

        // Act
        builder.Clear();

        // Assert
        Assert.Null(builder.DataSource);
        Assert.Null(builder.AuthToken);
        Assert.Null(builder.EncryptionKey);
        Assert.Null(builder.SyncUrl);
        Assert.Null(builder.SyncAuthToken);
        Assert.True(builder.ReadYourWrites);
        Assert.Equal(LibSQLConnectionMode.Local, builder.Mode);
    }

    [Theory]
    [InlineData("Data Source")]
    [InlineData("DataSource")]
    [InlineData("Auth Token")]
    [InlineData("AuthToken")]
    public void ContainsKey_ValidKey_ShouldReturnTrue(string keyword)
    {
        // Arrange
        var builder = new LibSQLConnectionStringBuilder();

        // Act & Assert
        Assert.True(builder.ContainsKey(keyword));
    }

    [Fact]
    public void Remove_ValidKey_ShouldRemoveProperty()
    {
        // Arrange
        var builder = new LibSQLConnectionStringBuilder
        {
            DataSource = "test.db"
        };

        // Act
        var result = builder.Remove("Data Source");

        // Assert
        Assert.True(result);
        Assert.Null(builder.DataSource);
    }

    [Fact]
    public void TryGetValue_ValidKey_ShouldReturnValue()
    {
        // Arrange
        var builder = new LibSQLConnectionStringBuilder
        {
            DataSource = "test.db"
        };

        // Act
        var result = builder.TryGetValue("Data Source", out var value);

        // Assert
        Assert.True(result);
        Assert.Equal("test.db", value);
    }

    [Fact]
    public void TryGetValue_InvalidKey_ShouldReturnFalse()
    {
        // Arrange
        var builder = new LibSQLConnectionStringBuilder();

        // Act
        var result = builder.TryGetValue("InvalidKey", out var value);

        // Assert
        Assert.False(result);
        Assert.Null(value);
    }
}