using System.Text;
using BenchmarkDotNet.Attributes;
using Nelknet.LibSQL.Data;

namespace Nelknet.LibSQL.Benchmarks;

[BenchmarkCategory("Parameters", "Managed")]
public class SqlParameterLayoutBenchmarks
{
    private string _sql = null!;
    private LibSQLParameterCollection _parameters = null!;

    [Params(1, 4, 16)]
    public int ParameterCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _sql = CreateSql(ParameterCount);
        _parameters = new LibSQLParameterCollection();

        for (int i = 0; i < ParameterCount; i++)
        {
            _parameters.AddWithValue($"@p{i}", i + 1L);
        }
    }

    [Benchmark]
    public object Parse()
    {
        return SqlParameterLayout.Parse(_sql);
    }

    [Benchmark]
    public int ParseAndResolve()
    {
        var layout = SqlParameterLayout.Parse(_sql);
        return layout.ResolveBindings(_parameters).Count;
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

        sql.Append(" /* ignore @comment */ || ';' -- ignore @line\n");
        return sql.ToString();
    }
}
