using Nelknet.LibSQL.Data;

namespace Nelknet.LibSQL.Tests;

public sealed class HttpRemoteIntegrationRegressionTests
{
    [Fact]
    public async Task BeginInsertRollback_DoesNotPersistRow()
    {
        var testUrl = Environment.GetEnvironmentVariable("LIBSQL_TEST_URL");
        if (string.IsNullOrWhiteSpace(testUrl))
        {
            return;
        }

        var testToken = Environment.GetEnvironmentVariable("LIBSQL_TEST_TOKEN");
        var connectionString = string.IsNullOrEmpty(testToken)
            ? $"Data Source={testUrl}"
            : $"Data Source={testUrl};Auth Token={testToken}";

        var tableName = "http_rollback_" + Guid.NewGuid().ToString("N");
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

            await transaction.RollbackAsync();
        }

        long persistedRows;
        await using (var connection = new LibSQLConnection(connectionString))
        {
            await connection.OpenAsync();

            await using (var select = connection.CreateCommand())
            {
                select.CommandText = $"SELECT COUNT(*) FROM {tableName} WHERE name = @name";
                select.Parameters.AddWithValue("@name", marker);
                persistedRows = Assert.IsType<long>(await select.ExecuteScalarAsync());
            }

            await using var drop = connection.CreateCommand();
            drop.CommandText = $"DROP TABLE {tableName}";
            await drop.ExecuteNonQueryAsync();
        }

        Assert.Equal(0, persistedRows);
    }
}
