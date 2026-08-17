using System.Text.Json;

namespace Nelknet.LibSQL.Benchmarks;

internal sealed record BenchmarkEnvironment(
    string Processor,
    string Architecture,
    string Runtime,
    string OperatingSystem);

internal sealed record BenchmarkMeasurement(
    string Name,
    double MeanNanoseconds,
    double AllocatedBytes);

internal sealed record BenchmarkReport(
    BenchmarkEnvironment Environment,
    IReadOnlyDictionary<string, BenchmarkMeasurement> Measurements)
{
    internal static BenchmarkReport Read(string path)
    {
        var reportFiles = GetReportFiles(path);
        if (reportFiles.Length == 0)
        {
            throw new InvalidOperationException($"No full JSON benchmark reports exist at '{path}'.");
        }

        BenchmarkEnvironment? environment = null;
        var measurements = new Dictionary<string, BenchmarkMeasurement>(StringComparer.Ordinal);

        foreach (var reportFile in reportFiles)
        {
            using var document = JsonDocument.Parse(File.ReadAllText(reportFile));
            var root = document.RootElement;
            var currentEnvironment = ReadEnvironment(root.GetProperty("HostEnvironmentInfo"));
            environment ??= currentEnvironment;

            if (environment != currentEnvironment)
            {
                throw new InvalidOperationException($"Benchmark reports at '{path}' contain different environments.");
            }

            foreach (var benchmark in root.GetProperty("Benchmarks").EnumerateArray())
            {
                var name = benchmark.GetProperty("FullName").GetString()
                    ?? throw new InvalidOperationException($"A benchmark in '{reportFile}' has no full name.");
                var mean = benchmark.GetProperty("Statistics").GetProperty("Mean").GetDouble();
                var allocatedBytes = ReadAllocatedBytes(benchmark);
                var measurement = new BenchmarkMeasurement(name, mean, allocatedBytes);

                if (!measurements.TryAdd(name, measurement))
                {
                    throw new InvalidOperationException($"Benchmark '{name}' occurs more than once at '{path}'.");
                }
            }
        }

        return new BenchmarkReport(environment!, measurements);
    }

    private static string[] GetReportFiles(string path)
    {
        if (File.Exists(path))
        {
            return path.EndsWith("-report-full.json", StringComparison.OrdinalIgnoreCase)
                ? new[] { Path.GetFullPath(path) }
                : Array.Empty<string>();
        }

        if (!Directory.Exists(path))
        {
            return Array.Empty<string>();
        }

        return Directory
            .EnumerateFiles(path, "*-report-full.json", SearchOption.AllDirectories)
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    private static BenchmarkEnvironment ReadEnvironment(JsonElement environment)
    {
        return new BenchmarkEnvironment(
            environment.GetProperty("ProcessorName").GetString() ?? string.Empty,
            environment.GetProperty("Architecture").GetString() ?? string.Empty,
            environment.GetProperty("RuntimeVersion").GetString() ?? string.Empty,
            environment.GetProperty("OsVersion").GetString() ?? string.Empty);
    }

    private static double ReadAllocatedBytes(JsonElement benchmark)
    {
        if (!benchmark.TryGetProperty("Memory", out var memory)
            || !memory.TryGetProperty("BytesAllocatedPerOperation", out var allocatedBytes))
        {
            return 0;
        }

        return allocatedBytes.GetDouble();
    }
}
