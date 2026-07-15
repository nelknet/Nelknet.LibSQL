using System;
using System.Threading.Tasks;
using Nelknet.LibSQL.Data;
using Xunit;

namespace Nelknet.LibSQL.Tests;

/// <summary>
/// HTTP connections must round-trip the Hrana baton so BEGIN/statements/COMMIT
/// share one stream across pipeline requests.
/// </summary>
public sealed class HttpTransactionTests
{
    private readonly string? _testUrl = Environment.GetEnvironmentVariable("LIBSQL_TEST_URL");
    private readonly string? _testToken = Environment.GetEnvironmentVariable("LIBSQL_TEST_TOKEN");
    private readonly bool _enabled;

    public HttpTransactionTests()
    {
        _enabled = !string.IsNullOrEmpty(_testUrl);
    }

    [Fact]
    public async Task BeginInsertCommit_PersistsRowAcrossReconnect()
    {
        if (!_enabled)
        {
            return;
        }

        var connectionString = string.IsNullOrEmpty(_testToken)
            ? $"Data Source={_testUrl}"
            : $"Data Source={_testUrl};Auth Token={_testToken}";

        var tableName = "http_txn_" + Guid.NewGuid().ToString("N");
        var marker = Guid.NewGuid().ToString("N");

        await using (var connection = new LibSQLConnection(connectionString))
        {
            await connection.OpenAsync();

            await using (var setup = connection.CreateCommand())
            {
                setup.CommandText = $"CREATE TABLE {tableName} (id INTEGER PRIMARY KEY, name TEXT NOT NULL)";
                await setup.ExecuteNonQueryAsync();
            }

            await using var transaction = await connection.BeginTransactionAsync();
            await using (var insert = connection.CreateCommand())
            {
                insert.Transaction = transaction;
                insert.CommandText = $"INSERT INTO {tableName} (name) VALUES (@name)";
                insert.Parameters.AddWithValue("@name", marker);
                await insert.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
        }

        await using (var connection = new LibSQLConnection(connectionString))
        {
            await connection.OpenAsync();
            await using var select = connection.CreateCommand();
            select.CommandText = $"SELECT name FROM {tableName} WHERE name = @name";
            select.Parameters.AddWithValue("@name", marker);
            var value = await select.ExecuteScalarAsync();
            Assert.Equal(marker, value);

            await using var drop = connection.CreateCommand();
            drop.CommandText = $"DROP TABLE {tableName}";
            await drop.ExecuteNonQueryAsync();
        }
    }

    [Fact]
    public async Task Commit_AfterDdl_DoesNotThrow()
    {
        if (!_enabled)
        {
            return;
        }

        var connectionString = string.IsNullOrEmpty(_testToken)
            ? $"Data Source={_testUrl}"
            : $"Data Source={_testUrl};Auth Token={_testToken}";

        var tableName = "http_ddl_" + Guid.NewGuid().ToString("N");

        await using var connection = new LibSQLConnection(connectionString);
        await connection.OpenAsync();

        await using var transaction = await connection.BeginTransactionAsync();
        await using (var create = connection.CreateCommand())
        {
            create.Transaction = transaction;
            create.CommandText = $"CREATE TABLE {tableName} (id INTEGER PRIMARY KEY)";
            await create.ExecuteNonQueryAsync();
        }

        // sqld auto-commits DDL; Commit must still complete cleanly for EF EnsureCreated.
        await transaction.CommitAsync();

        await using var drop = connection.CreateCommand();
        drop.CommandText = $"DROP TABLE IF EXISTS {tableName}";
        await drop.ExecuteNonQueryAsync();
    }
}
