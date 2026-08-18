using Nelknet.LibSQL.Data;
using Xunit;

namespace Nelknet.LibSQL.Tests;

public sealed class StatementCachePolicyTests
{
    [Fact]
    public void ExecuteScalar_CommandCacheDisabled_DoesNotPopulateConnectionCache()
    {
        using var connection = OpenCachedConnection();
        using var command = new LibSQLCommand("SELECT @value + 1", connection)
        {
            EnableStatementCaching = false
        };
        command.Parameters.AddWithValue("@value", 41L);

        Assert.Equal(42L, command.ExecuteScalar());
        Assert.Equal(0, connection.StatementCache!.Count);
    }

    [Fact]
    public void ExecuteScalar_PositionalParameter_DoesNotPopulateConnectionCache()
    {
        using var connection = OpenCachedConnection();
        using var command = new LibSQLCommand("SELECT ? + 1", connection);
        command.Parameters.AddWithValue("?value", 41L);

        Assert.Equal(42L, command.ExecuteScalar());
        Assert.Equal(0, connection.StatementCache!.Count);
    }

    private static LibSQLConnection OpenCachedConnection()
    {
        var connection = new LibSQLConnection("Data Source=:memory:")
        {
            EnableStatementCaching = true,
            MaxCachedStatements = 4
        };
        connection.Open();
        return connection;
    }
}
