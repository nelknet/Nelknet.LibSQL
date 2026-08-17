using Nelknet.LibSQL.Data.Http;
using Xunit;

namespace Nelknet.LibSQL.Tests;

public sealed class SqlStatementScannerTests
{
    [Theory]
    [InlineData("", false)]
    [InlineData("  ; ;  ", false)]
    [InlineData("-- comment only;", false)]
    [InlineData("/* comment only; */", false)]
    [InlineData("SELECT 1", false)]
    [InlineData("SELECT 1;", false)]
    [InlineData("; SELECT 1; ;", false)]
    [InlineData("-- ignored;\nSELECT 1;", false)]
    [InlineData("SELECT 1; -- ignored;", false)]
    [InlineData("SELECT 1; /* ignored; */", false)]
    [InlineData("SELECT 'a'';b';", false)]
    [InlineData("SELECT \"a\"\";b\";", false)]
    [InlineData("SELECT `a``;b`;", false)]
    [InlineData("SELECT 'unterminated;", false)]
    [InlineData("SELECT 1; SELECT 2;", true)]
    [InlineData("SELECT ';'; SELECT 2;", true)]
    [InlineData("SELECT 'it''s;fine'; SELECT 2;", true)]
    [InlineData("SELECT \"a;b\"; SELECT 2;", true)]
    [InlineData("SELECT `a;b`; SELECT 2;", true)]
    [InlineData("SELECT [a;b]; SELECT 2;", true)]
    [InlineData("SELECT 1; -- ignored;\nSELECT 2;", true)]
    [InlineData("SELECT 1 /* ignored; */; SELECT 2;", true)]
    [InlineData("SELECT 1; /* ignored; */ SELECT 2;", true)]
    public void ContainsMultipleStatements_SqlText_ReturnsExpectedResult(string sql, bool expected)
    {
        Assert.Equal(expected, SqlStatementScanner.ContainsMultipleStatements(sql));
    }
}
