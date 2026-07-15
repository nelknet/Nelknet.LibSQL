using Nelknet.LibSQL.Data.Internal;

namespace Nelknet.LibSQL.Tests;

public sealed class SqlStatementClassifierTests
{
    [Theory]
    [InlineData("INSERT INTO items DEFAULT VALUES RETURNING id")]
    [InlineData("UPDATE items SET name = 'new' RETURNING id")]
    [InlineData("DELETE FROM items RETURNING id")]
    [InlineData("WITH value(name) AS (VALUES ('ada')) INSERT INTO items SELECT * FROM value RETURNING id")]
    [InlineData("/* leading comment */ REPLACE INTO items VALUES (1, 'ada') RETURNING id")]
    public void RequiresDrainOnReaderClose_ReturningWrite_ReturnsTrue(string sql)
    {
        Assert.True(SqlStatementClassifier.RequiresDrainOnReaderClose(sql));
    }

    [Theory]
    [InlineData("SELECT 'INSERT RETURNING id'")]
    [InlineData("SELECT returning_value FROM items")]
    [InlineData("SELECT 1 /* INSERT RETURNING id */")]
    [InlineData("SELECT 1; INSERT INTO items DEFAULT VALUES RETURNING id")]
    [InlineData("INSERT INTO items DEFAULT VALUES")]
    [InlineData("EXPLAIN INSERT INTO items DEFAULT VALUES RETURNING id")]
    public void RequiresDrainOnReaderClose_NonReturningWriteOrRead_ReturnsFalse(string sql)
    {
        Assert.False(SqlStatementClassifier.RequiresDrainOnReaderClose(sql));
    }
}
