using System.Text;
using BenchmarkDotNet.Attributes;
using Nelknet.LibSQL.Data;

namespace Nelknet.LibSQL.Benchmarks;

[BenchmarkCategory("Parameters", "Native")]
public class ParameterExecutionBenchmarks : IDisposable
{
    private LibSQLConnection _connection = null!;
    private LibSQLCommand _command = null!;

    [Params(1, 4, 16)]
    public int ParameterCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _connection = new LibSQLConnection("Data Source=:memory:");
        _connection.Open();

        _command = new LibSQLCommand(CreateSql(ParameterCount), _connection);
        for (int i = 0; i < ParameterCount; i++)
        {
            _command.Parameters.AddWithValue($"@p{i}", i + 1L);
        }

        _command.Prepare();
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

    private static string CreateSql(int parameterCount)
    {
        var sql = new StringBuilder("SELECT ");
        for (int i = 0; i < parameterCount; i++)
        {
            if (i > 0)
            {
                sql.Append(" + ");
            }

            sql.Append("@p");
            sql.Append(i);
        }

        return sql.ToString();
    }

    public void Dispose()
    {
        _command?.Dispose();
        _connection?.Dispose();
        GC.SuppressFinalize(this);
    }
}
