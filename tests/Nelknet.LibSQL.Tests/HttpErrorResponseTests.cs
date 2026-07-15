using System;
using System.Threading.Tasks;
using Nelknet.LibSQL.Data;
using Nelknet.LibSQL.Data.Exceptions;
using Xunit;

namespace Nelknet.LibSQL.Tests;

/// <summary>
/// Hrana pipeline-level errors are shaped as
/// <c>{"type":"error","error":{"message":"…"}}</c> (not nested under <c>response</c>).
/// </summary>
public sealed class HttpErrorResponseTests
{
    private readonly string? _testUrl = Environment.GetEnvironmentVariable("LIBSQL_TEST_URL");
    private readonly string? _testToken = Environment.GetEnvironmentVariable("LIBSQL_TEST_TOKEN");
    private readonly bool _enabled;

    public HttpErrorResponseTests()
    {
        _enabled = !string.IsNullOrEmpty(_testUrl);
    }

    [Fact]
    public async Task ExecuteReader_MissingTable_ThrowsSqlErrorMessage()
    {
        if (!_enabled)
        {
            return; // Soft-skip when remote env is not configured (matches RemoteIntegrationTests).
        }

        var connectionString = string.IsNullOrEmpty(_testToken)
            ? $"Data Source={_testUrl}"
            : $"Data Source={_testUrl};Auth Token={_testToken}";

        await using var connection = new LibSQLConnection(connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = $"INSERT INTO \"missing_{Guid.NewGuid():N}\" (Id) VALUES (1)";

        var ex = await Assert.ThrowsAsync<LibSQLException>(async () =>
        {
            await using var reader = await command.ExecuteReaderAsync();
        });

        Assert.Contains("no such table", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Invalid response from server", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteNonQuery_MissingTable_ThrowsSqlErrorMessage()
    {
        if (!_enabled)
        {
            return;
        }

        var connectionString = string.IsNullOrEmpty(_testToken)
            ? $"Data Source={_testUrl}"
            : $"Data Source={_testUrl};Auth Token={_testToken}";

        await using var connection = new LibSQLConnection(connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = $"UPDATE \"missing_{Guid.NewGuid():N}\" SET Id = 1";

        var ex = await Assert.ThrowsAsync<LibSQLException>(() => command.ExecuteNonQueryAsync());

        Assert.Contains("no such table", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
