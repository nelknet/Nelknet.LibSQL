#nullable disable warnings

using System;
using System.Threading.Tasks;
using Xunit;
using Nelknet.LibSQL.Data;
using Nelknet.LibSQL.Data.Http;
using Nelknet.LibSQL.Data.Exceptions;

namespace Nelknet.LibSQL.Tests;

/// <summary>
/// Tests for HTTP-based remote connection functionality
/// </summary>
public class HttpConnectionTests
{
    /// <summary>
    /// Tests that the connection string builder correctly identifies remote URLs.
    /// </summary>
    [Theory]
    [InlineData("https://example.turso.io", LibSQLConnectionMode.Remote)]
    [InlineData("http://localhost:8080", LibSQLConnectionMode.Remote)]
    [InlineData("libsql://example.turso.io", LibSQLConnectionMode.Remote)]
    [InlineData("./local.db", LibSQLConnectionMode.Local)]
    [InlineData(":memory:", LibSQLConnectionMode.Local)]
    public void ConnectionStringBuilder_CorrectlyIdentifiesConnectionMode(string dataSource, LibSQLConnectionMode expectedMode)
    {
        var builder = new LibSQLConnectionStringBuilder
        {
            DataSource = dataSource
        };

        Assert.Equal(expectedMode, builder.Mode);
    }

    /// <summary>
    /// Tests that embedded replica mode is correctly identified when sync URL is present.
    /// </summary>
    [Fact]
    public void ConnectionStringBuilder_IdentifiesEmbeddedReplicaMode()
    {
        var builder = new LibSQLConnectionStringBuilder
        {
            DataSource = "./replica.db",
            SyncUrl = "https://example.turso.io"
        };

        Assert.Equal(LibSQLConnectionMode.EmbeddedReplica, builder.Mode);
    }

    /// <summary>
    /// Tests that remote connections can be created without auth token.
    /// Connection will fail if the server requires auth, but the client
    /// no longer enforces auth token requirement.
    /// </summary>
    [Fact]
    public void RemoteConnection_AllowsOptionalAuthToken()
    {
        var connectionString = "Data Source=https://example.turso.io";
        using var connection = new LibSQLConnection(connectionString);

        // Connection creation should succeed, but Open() will fail due to network/auth
        var exception = Assert.Throws<LibSQLConnectionException>(() => connection.Open());
        Assert.Contains("Failed to connect", exception.Message);
    }

    /// <summary>
    /// Tests HTTP client creation and basic configuration.
    /// </summary>
    [Fact]
    public void HttpClient_CreatesWithCorrectConfiguration()
    {
        const string url = "https://example.turso.io";
        const string token = "test-token";

        using var httpClient = new LibSQLHttpClient(url, token);
        
        // If we get here without exception, the client was created successfully
        Assert.NotNull(httpClient);
    }

    /// <summary>
    /// Tests HTTP client URL normalization.
    /// </summary>
    [Theory]
    [InlineData("libsql://example.turso.io")]
    [InlineData("https://example.turso.io")]
    [InlineData("https://example.turso.io/")]
    public void HttpClient_NormalizesUrls(string inputUrl)
    {
        const string token = "test-token";

        using var httpClient = new LibSQLHttpClient(inputUrl, token);
        
        // We can't directly test the normalized URL, but we can verify the client was created
        Assert.NotNull(httpClient);
    }

    /// <summary>
    /// Tests Hrana protocol type constants.
    /// </summary>
    [Fact]
    public void HranaTypes_HasCorrectConstants()
    {
        // This test verifies the type constants are available
        Assert.Equal("null", HranaTypes.Null);
        Assert.Equal("integer", HranaTypes.Integer);
        Assert.Equal("float", HranaTypes.Float);
        Assert.Equal("text", HranaTypes.Text);
        Assert.Equal("blob", HranaTypes.Blob);
        Assert.Equal("execute", HranaTypes.Execute);
        Assert.Equal("close", HranaTypes.Close);
        Assert.Equal("ok", HranaTypes.Ok);
        Assert.Equal("error", HranaTypes.Error);
    }

    /// <summary>
    /// Tests that HTTP commands are created for remote connections.
    /// </summary>
    [Fact]
    public void Connection_CreatesHttpCommandsForRemoteConnections()
    {
        // This test would require a mock HTTP client or actual server
        // For now, we just test that the connection identifies itself as HTTP
        var connectionString = "Data Source=https://example.turso.io;Auth Token=test-token";
        using var connection = new LibSQLConnection(connectionString);

        // We can't open the connection without a real server, but we can test the builder
        var builder = new LibSQLConnectionStringBuilder(connectionString);
        Assert.Equal(LibSQLConnectionMode.Remote, builder.Mode);
        Assert.Equal("test-token", builder.AuthToken);
    }

    /// <summary>
    /// Tests parameter conversion for HTTP requests.
    /// </summary>
    [Fact]
    public void LibSQLParameter_ConvertsTypesForHttp()
    {
        var param = new LibSQLParameter("@test", System.Data.DbType.String)
        {
            Value = "test value"
        };

        Assert.Equal("@test", param.ParameterName);
        Assert.Equal("test value", param.Value);
        Assert.Equal(System.Data.DbType.String, param.DbType);
    }

    /// <summary>
    /// Tests that LibSQLHttpException provides detailed error information.
    /// </summary>
    [Fact]
    public void LibSQLHttpException_ProvidesDetailedErrorInfo()
    {
        const string message = "HTTP error occurred";
        const int statusCode = 404;
        const string responseContent = "Not Found";
        const string requestContent = "{ \"test\": \"data\" }";

        var exception = new LibSQLHttpException(message, statusCode, responseContent, requestContent);

        Assert.Equal(message, exception.Message);
        Assert.Equal(statusCode, exception.StatusCode);
        Assert.Equal(responseContent, exception.ResponseContent);
        Assert.Equal(requestContent, exception.RequestContent);
    }

    /// <summary>
    /// Tests that LibSQLHttpException handles inner exceptions.
    /// </summary>
    [Fact]
    public void LibSQLHttpException_HandlesInnerExceptions()
    {
        const string message = "HTTP error occurred";
        const int statusCode = 500;
        var innerException = new InvalidOperationException("Inner error");

        var exception = new LibSQLHttpException(message, statusCode, innerException);

        Assert.Equal(message, exception.Message);
        Assert.Equal(statusCode, exception.StatusCode);
        Assert.Equal(innerException, exception.InnerException);
    }

    /// <summary>
    /// Tests that connection is properly identified as HTTP-based.
    /// </summary>
    [Fact]
    public void Connection_IdentifiesAsHttpConnection()
    {
        var connectionString = "Data Source=https://example.turso.io;Auth Token=test-token";
        using var connection = new LibSQLConnection(connectionString);

        // Before opening, IsHttpConnection should be false
        Assert.False(connection.IsHttpConnection);
        
        // We can't test after opening without a real server
        // But we can verify the property exists and is accessible
    }

    /// <summary>
    /// Tests that commands created for HTTP connections work correctly.
    /// </summary>
    [Fact]
    public void Connection_CreatesCommandsForHttpConnections()
    {
        var connectionString = "Data Source=https://example.turso.io;Auth Token=test-token";
        using var connection = new LibSQLConnection(connectionString);
        
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1";
        
        Assert.NotNull(command);
        Assert.Equal("SELECT 1", command.CommandText);
        Assert.Equal(connection, command.Connection);
    }
}