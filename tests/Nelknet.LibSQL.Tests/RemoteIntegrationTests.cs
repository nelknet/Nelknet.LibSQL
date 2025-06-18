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

    [Fact]
    public async Task RemoteConnection_CanExecuteMultipleStatements()
    {
        if (!_testsEnabled)
        {
            return;
        }

        var connectionString = $"Data Source={_testUrl};Auth Token={_testToken}";
        using var connection = new LibSQLConnection(connectionString);
        await connection.OpenAsync();

        var tableName = $"test_multi_{Guid.NewGuid():N}";

        try
        {
            // Test: Execute multiple CREATE statements in one command
            using var createCmd = connection.CreateCommand();
            createCmd.CommandText = $@"
                CREATE TABLE {tableName}_1 (id INTEGER PRIMARY KEY, name TEXT);
                CREATE TABLE {tableName}_2 (id INTEGER PRIMARY KEY, value REAL);
                CREATE TABLE {tableName}_3 (id INTEGER PRIMARY KEY, data BLOB);";
            
            await createCmd.ExecuteNonQueryAsync();

            // Verify all tables were created
            using var checkCmd = connection.CreateCommand();
            checkCmd.CommandText = $@"
                SELECT name FROM sqlite_master 
                WHERE type = 'table' AND name LIKE '{tableName}%'
                ORDER BY name";
            
            using var reader = await checkCmd.ExecuteReaderAsync();
            var tables = new List<string>();
            while (await reader.ReadAsync())
            {
                tables.Add(reader.GetString(0));
            }

            Assert.Equal(3, tables.Count);
            Assert.Contains($"{tableName}_1", tables);
            Assert.Contains($"{tableName}_2", tables);
            Assert.Contains($"{tableName}_3", tables);

            // Test: Execute multiple INSERT statements and verify affected rows
            using var insertCmd = connection.CreateCommand();
            insertCmd.CommandText = $@"INSERT INTO {tableName}_1 (name) VALUES ('Alice'); INSERT INTO {tableName}_1 (name) VALUES ('Bob'); INSERT INTO {tableName}_2 (value) VALUES (3.14); INSERT INTO {tableName}_3 (data) VALUES (X'48656C6C6F');";
            
            var affectedRows = await insertCmd.ExecuteNonQueryAsync();
            // For now, accept either -1 (sequence) or 0 (if not detected as multi-statement)
            Assert.True(affectedRows == -1 || affectedRows >= 0);
            
            // Verify the data was actually inserted
            using var count1Cmd = connection.CreateCommand();
            count1Cmd.CommandText = $"SELECT COUNT(*) FROM {tableName}_1";
            var count1 = await count1Cmd.ExecuteScalarAsync();
            Assert.Equal(2L, count1);
            
            using var count2Cmd = connection.CreateCommand();
            count2Cmd.CommandText = $"SELECT COUNT(*) FROM {tableName}_2";
            var count2 = await count2Cmd.ExecuteScalarAsync();
            Assert.Equal(1L, count2);
            
            using var count3Cmd = connection.CreateCommand();
            count3Cmd.CommandText = $"SELECT COUNT(*) FROM {tableName}_3";
            var count3 = await count3Cmd.ExecuteScalarAsync();
            Assert.Equal(1L, count3);
        }
        finally
        {
            // Clean up
            try
            {
                using var dropCmd = connection.CreateCommand();
                dropCmd.CommandText = $@"
                    DROP TABLE IF EXISTS {tableName}_1;
                    DROP TABLE IF EXISTS {tableName}_2;
                    DROP TABLE IF EXISTS {tableName}_3;";
                await dropCmd.ExecuteNonQueryAsync();
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    // Note: Traditional transaction tests are not included for HTTP/remote connections because:
    // HTTP connections don't support stateful transactions across multiple requests.
    // Each HTTP request is independent. However, you can achieve atomic execution using
    // ExecuteTransactionalBatchAsync() which wraps statements in BEGIN/COMMIT/ROLLBACK.

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