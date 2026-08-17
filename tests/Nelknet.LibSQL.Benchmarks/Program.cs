using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Running;

namespace Nelknet.LibSQL.Benchmarks;

internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length > 0 && string.Equals(args[0], "compare", StringComparison.OrdinalIgnoreCase))
        {
            return BenchmarkComparison.Run(args[1..]);
        }

        var summaries = BenchmarkSwitcher
            .FromAssembly(typeof(Program).Assembly)
            .Run(args, CreateConfig());

        return summaries.Any(summary => summary.HasCriticalValidationErrors) ? 1 : 0;
    }

    private static ManualConfig CreateConfig()
    {
        return DefaultConfig.Instance
            .AddDiagnoser(MemoryDiagnoser.Default)
            .AddExporter(JsonExporter.Full)
            .AddColumn(StatisticColumn.P95)
            .AddColumn(CategoriesColumn.Default)
            .WithOption(ConfigOptions.JoinSummary, true);
    }
}
