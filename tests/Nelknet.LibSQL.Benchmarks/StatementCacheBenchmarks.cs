using BenchmarkDotNet.Attributes;
using Nelknet.LibSQL.Data;

namespace Nelknet.LibSQL.Benchmarks;

[BenchmarkCategory("StatementCache", "Native")]
public class StatementCacheBenchmarks : IDisposable
{
    private LibSQLConnection _connection = null!;
    private LibSQLCommand _command = null!;

    [GlobalSetup]
    public void Setup()
    {
        _connection = new LibSQLConnection("Data Source=:memory:")
        {
            EnableStatementCaching = true,
        };
        _connection.Open();

        _command = new LibSQLCommand("SELECT @value + 1", _connection);
        _command.Parameters.AddWithValue("@value", 41L);

        _command.ExecuteScalar();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        Dispose();
    }

    [Benchmark]
    public object? ExecuteScalar()
    {
        return _command.ExecuteScalar();
    }

    public void Dispose()
    {
        _command?.Dispose();
        _connection?.Dispose();
        GC.SuppressFinalize(this);
    }
}
