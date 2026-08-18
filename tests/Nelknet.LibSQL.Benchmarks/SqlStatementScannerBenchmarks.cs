using BenchmarkDotNet.Attributes;
using Nelknet.LibSQL.Data.Http;

namespace Nelknet.LibSQL.Benchmarks;

[BenchmarkCategory("Http", "Managed")]
public class SqlStatementScannerBenchmarks
{
    private string _sql = null!;

    [Params(
        SqlStatementShape.Single,
        SqlStatementShape.Multiple,
        SqlStatementShape.QuotedAndCommented)]
    public SqlStatementShape Shape { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _sql = Shape switch
        {
            SqlStatementShape.Single => "SELECT value FROM items WHERE name = 'alpha'",
            SqlStatementShape.Multiple => "SELECT 1; INSERT INTO items VALUES (2); SELECT 3;",
            SqlStatementShape.QuotedAndCommented =>
                "SELECT 'semi;colon', \"quoted;name\" FROM items /* ignored; */; -- ignored;\nSELECT 2;",
            _ => throw new InvalidOperationException($"Unknown SQL statement shape: {Shape}.")
        };
    }

    [Benchmark(Baseline = true)]
    public bool SplitAndCount()
    {
        if (string.IsNullOrWhiteSpace(_sql))
        {
            return false;
        }

        return _sql
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Count(statement => !string.IsNullOrWhiteSpace(statement)) > 1;
    }

    [Benchmark]
    public bool Scan()
    {
        return SqlStatementScanner.ContainsMultipleStatements(_sql);
    }
}

public enum SqlStatementShape
{
    Single,
    Multiple,
    QuotedAndCommented
}
