#nullable disable warnings

using System;
using System.Threading.Tasks;
using Xunit;
using Nelknet.LibSQL.Data;
using Nelknet.LibSQL.Data.Http;
using Nelknet.LibSQL.Data.Exceptions;

namespace Nelknet.LibSQL.Tests;

/// <summary>
/// Integration tests for remote HTTP connections.
/// These tests require a running sqld server or valid Turso credentials.
/// Set environment variables to enable these tests:
/// - LIBSQL_TEST_URL: The URL of the libSQL server (e.g., "http://localhost:8080" or "https://your-db.turso.io")
/// - LIBSQL_TEST_TOKEN: The authentication token
/// </summary>
public class RemoteIntegrationTests
{
    private readonly string? _testUrl;
    private readonly string? _testToken;
    private readonly bool _testsEnabled;

    public RemoteIntegrationTests()
    {
        _testUrl = Environment.GetEnvironmentVariable("LIBSQL_TEST_URL");
        _testToken = Environment.GetEnvironmentVariable("LIBSQL_TEST_TOKEN");
        // Enable tests if URL is provided (token is optional for servers without auth)
        _testsEnabled = !string.IsNullOrEmpty(_testUrl);
    }

    [Fact]
    public async Task RemoteConnection_CanConnect()
    {
        if (!_testsEnabled)
        {
            // Skip test if environment variables not set
            return;
        }

        var connectionString = $"Data Source={_testUrl};Auth Token={_testToken}";
        using var connection = new LibSQLConnection(connectionString);

        await connection.OpenAsync();
        Assert.Equal(System.Data.ConnectionState.Open, connection.State);
        Assert.True(connection.IsHttpConnection);
    }

    [Fact]
    public async Task RemoteConnection_CanExecuteSimpleQuery()
    {
        if (!_testsEnabled)
        {
            return;
        }

        var connectionString = $"Data Source={_testUrl};Auth Token={_testToken}";
        using var connection = new LibSQLConnection(connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1 as test_value";

        var result = await command.ExecuteScalarAsync();
        Assert.Equal(1L, result);
    }

    [Fact]
    public async Task RemoteConnection_CanExecuteParameterizedQuery()
    {
        if (!_testsEnabled)
        {
            return;
        }

        var connectionString = $"Data Source={_testUrl};Auth Token={_testToken}";
        using var connection = new LibSQLConnection(connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT @value as param_test";
        command.Parameters.Add(new LibSQLParameter("@value", "test_string"));

        var result = await command.ExecuteScalarAsync();
        Assert.Equal("test_string", result);
    }

    [Fact]
    public async Task RemoteConnection_CanCreateTableAndInsertData()
    {
        if (!_testsEnabled)
        {
            return;
        }

        var connectionString = $"Data Source={_testUrl};Auth Token={_testToken}";
        using var connection = new LibSQLConnection(connectionString);
        await connection.OpenAsync();

        var tableName = $"test_table_{Guid.NewGuid():N}";

        try
        {
            // Create table
            using var createCmd = connection.CreateCommand();
            createCmd.CommandText = $@"
                CREATE TABLE {tableName} (
                    id INTEGER PRIMARY KEY,
                    name TEXT NOT NULL,
                    value REAL,
                    created_at TEXT DEFAULT CURRENT_TIMESTAMP
                )";
            await createCmd.ExecuteNonQueryAsync();

            // Insert data
            using var insertCmd = connection.CreateCommand();
            insertCmd.CommandText = $"INSERT INTO {tableName} (name, value) VALUES (@name, @value)";
            insertCmd.Parameters.Add(new LibSQLParameter("@name", "test_record"));
            insertCmd.Parameters.Add(new LibSQLParameter("@value", 123.45));

            var rowsAffected = await insertCmd.ExecuteNonQueryAsync();
            Assert.Equal(1, rowsAffected);

            // Query data back
            using var selectCmd = connection.CreateCommand();
            selectCmd.CommandText = $"SELECT name, value FROM {tableName} WHERE name = @name";
            selectCmd.Parameters.Add(new LibSQLParameter("@name", "test_record"));

            using var reader = await selectCmd.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync());
            Assert.Equal("test_record", reader.GetString(0));
            Assert.Equal(123.45, reader.GetDouble(1));
        }
        finally
        {
            // Clean up
            try
            {
                using var dropCmd = connection.CreateCommand();
                dropCmd.CommandText = $"DROP TABLE IF EXISTS {tableName}";
                await dropCmd.ExecuteNonQueryAsync();
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    // Note: Transaction tests are not included for HTTP/remote connections because:
    // 1. HTTP connections don't support stateful transactions across multiple requests
    // 2. Each HTTP request is independent and atomic
    // 3. While libSQL's Rust client supports transactional batches (multiple statements
    //    in one atomic request), our current implementation only sends one statement
    //    per request. This could be enhanced in the future.
    //
    // The ADO.NET transaction API (BeginTransaction, Commit, Rollback) is implemented
    // for compatibility but won't provide true transaction isolation over HTTP.

    [Fact]
    public async Task RemoteConnection_HandlesDataTypes()
    {
        if (!_testsEnabled)
        {
            return;
        }

        var connectionString = $"Data Source={_testUrl};Auth Token={_testToken}";
        using var connection = new LibSQLConnection(connectionString);
        await connection.OpenAsync();

        var tableName = $"test_types_{Guid.NewGuid():N}";

        try
        {
            // Create table with various data types
            using var createCmd = connection.CreateCommand();
            createCmd.CommandText = $@"
                CREATE TABLE {tableName} (
                    int_col INTEGER,
                    real_col REAL,
                    text_col TEXT,
                    blob_col BLOB,
                    null_col TEXT
                )";
            await createCmd.ExecuteNonQueryAsync();

            // Insert test data
            using var insertCmd = connection.CreateCommand();
            insertCmd.CommandText = $@"
                INSERT INTO {tableName} (int_col, real_col, text_col, blob_col, null_col) 
                VALUES (@int_val, @real_val, @text_val, @blob_val, @null_val)";
            
            insertCmd.Parameters.Add(new LibSQLParameter("@int_val", 42));
            insertCmd.Parameters.Add(new LibSQLParameter("@real_val", 3.14159));
            insertCmd.Parameters.Add(new LibSQLParameter("@text_val", "Hello, libSQL!"));
            insertCmd.Parameters.Add(new LibSQLParameter("@blob_val", new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F }));
            insertCmd.Parameters.Add(new LibSQLParameter("@null_val", DBNull.Value));

            await insertCmd.ExecuteNonQueryAsync();

            // Query and verify data types
            using var selectCmd = connection.CreateCommand();
            selectCmd.CommandText = $"SELECT int_col, real_col, text_col, blob_col, null_col FROM {tableName}";

            using var reader = await selectCmd.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync());

            Assert.Equal(42L, reader.GetInt64(0));
            Assert.Equal(3.14159, reader.GetDouble(1), 5); // 5 decimal places precision
            Assert.Equal("Hello, libSQL!", reader.GetString(2));
            
            var blobData = (byte[])reader.GetValue(3);
            Assert.Equal(new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F }, blobData);
            
            Assert.True(reader.IsDBNull(4));
        }
        finally
        {
            // Clean up
            try
            {
                using var dropCmd = connection.CreateCommand();
                dropCmd.CommandText = $"DROP TABLE IF EXISTS {tableName}";
                await dropCmd.ExecuteNonQueryAsync();
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Fact]
    public async Task RemoteConnection_HandlesConnectionErrors()
    {
        // Test invalid URL
        var invalidUrl = "https://invalid-nonexistent-server.example.com";
        var connectionString = $"Data Source={invalidUrl};Auth Token=invalid-token";
        using var connection = new LibSQLConnection(connectionString);

        await Assert.ThrowsAsync<LibSQLConnectionException>(() => connection.OpenAsync());
    }

    [Fact]
    public async Task RemoteConnection_HandlesAuthenticationErrors()
    {
        if (!_testsEnabled)
        {
            return;
        }

        // Skip this test if we're running against a server without auth
        // (e.g., local sqld without JWT key configured)
        if (string.IsNullOrEmpty(_testToken) || _testUrl.Contains("localhost:8080"))
        {
            // Server doesn't require auth, skip test
            return;
        }

        // Test with invalid token
        var connectionString = $"Data Source={_testUrl};Auth Token=invalid-token-12345";
        using var connection = new LibSQLConnection(connectionString);

        var exception = await Assert.ThrowsAnyAsync<Exception>(() => connection.OpenAsync());
        // The exact exception type depends on the server response
        // Could be LibSQLConnectionException or LibSQLHttpException
    }
}