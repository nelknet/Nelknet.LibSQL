using System.Globalization;
using System.Text.Json;

namespace Nelknet.LibSQL.Benchmarks;

internal static class BenchmarkComparison
{
    private const string NamespacePrefix = "Nelknet.LibSQL.Benchmarks.";

    internal static int Run(string[] args)
    {
        try
        {
            var options = ComparisonOptions.Parse(args);
            var baseline = BenchmarkReport.Read(options.BaselinePath);
            var candidate = BenchmarkReport.Read(options.CandidatePath);

            VerifyEnvironment(baseline.Environment, candidate.Environment);
            return WriteComparison(baseline, candidate, options);
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or InvalidOperationException or JsonException)
        {
            Console.Error.WriteLine(exception.Message);
            return 2;
        }
    }

    private static int WriteComparison(
        BenchmarkReport baseline,
        BenchmarkReport candidate,
        ComparisonOptions options)
    {
        var sharedNames = baseline.Measurements.Keys
            .Intersect(candidate.Measurements.Keys, StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

        if (sharedNames.Length == 0)
        {
            throw new InvalidOperationException("The reports contain no common benchmarks.");
        }

        Console.WriteLine($"Environment: {candidate.Environment.Processor}, {candidate.Environment.Architecture}, {candidate.Environment.Runtime}");
        Console.WriteLine();
        Console.WriteLine("| Benchmark | Baseline | Candidate | Time delta | Baseline allocation | Candidate allocation | Allocation delta |");
        Console.WriteLine("|---|---:|---:|---:|---:|---:|---:|");

        bool timeRegression = false;
        bool allocationRegression = false;

        foreach (var name in sharedNames)
        {
            var baselineMeasurement = baseline.Measurements[name];
            var candidateMeasurement = candidate.Measurements[name];
            var timeDelta = CalculateDelta(baselineMeasurement.MeanNanoseconds, candidateMeasurement.MeanNanoseconds);
            var allocationDelta = CalculateDelta(baselineMeasurement.AllocatedBytes, candidateMeasurement.AllocatedBytes);

            timeRegression |= timeDelta > options.MaximumRegressionPercent;
            allocationRegression |= candidateMeasurement.AllocatedBytes > baselineMeasurement.AllocatedBytes;

            Console.WriteLine(
                $"| {ShortenName(name)} | {FormatDuration(baselineMeasurement.MeanNanoseconds)} | "
                + $"{FormatDuration(candidateMeasurement.MeanNanoseconds)} | {FormatDelta(timeDelta)} | "
                + $"{FormatBytes(baselineMeasurement.AllocatedBytes)} | {FormatBytes(candidateMeasurement.AllocatedBytes)} | "
                + $"{FormatDelta(allocationDelta)} |");
        }

        WriteMissingBenchmarks("Candidate report lacks", baseline.Measurements.Keys.Except(candidate.Measurements.Keys, StringComparer.Ordinal));
        WriteMissingBenchmarks("Candidate report adds", candidate.Measurements.Keys.Except(baseline.Measurements.Keys, StringComparer.Ordinal));

        if (timeRegression)
        {
            Console.Error.WriteLine($"A time regression exceeds {options.MaximumRegressionPercent:0.##}%.");
        }

        if (options.FailOnAllocationIncrease && allocationRegression)
        {
            Console.Error.WriteLine("A candidate benchmark allocates more memory than its baseline.");
        }

        return timeRegression || (options.FailOnAllocationIncrease && allocationRegression) ? 1 : 0;
    }

    private static void VerifyEnvironment(BenchmarkEnvironment baseline, BenchmarkEnvironment candidate)
    {
        if (baseline != candidate)
        {
            throw new InvalidOperationException(
                "The benchmark environments differ. Run both reports on the same machine, operating system, architecture, and runtime.");
        }
    }

    private static void WriteMissingBenchmarks(string label, IEnumerable<string> names)
    {
        var orderedNames = names.Order(StringComparer.Ordinal).Select(ShortenName).ToArray();
        if (orderedNames.Length == 0)
        {
            return;
        }

        Console.WriteLine();
        Console.WriteLine($"{label}: {string.Join(", ", orderedNames)}");
    }

    private static double CalculateDelta(double baseline, double candidate)
    {
        if (baseline == 0)
        {
            return candidate == 0 ? 0 : double.PositiveInfinity;
        }

        return ((candidate / baseline) - 1) * 100;
    }

    private static string FormatDuration(double nanoseconds)
    {
        return nanoseconds switch
        {
            >= 1_000_000 => $"{nanoseconds / 1_000_000:0.###} ms",
            >= 1_000 => $"{nanoseconds / 1_000:0.###} us",
            _ => $"{nanoseconds:0.###} ns",
        };
    }

    private static string FormatBytes(double bytes)
    {
        return bytes switch
        {
            >= 1024 * 1024 => $"{bytes / (1024 * 1024):0.###} MiB",
            >= 1024 => $"{bytes / 1024:0.###} KiB",
            _ => $"{bytes:0} B",
        };
    }

    private static string FormatDelta(double percent)
    {
        return double.IsPositiveInfinity(percent)
            ? "new"
            : percent.ToString("+0.00;-0.00;0.00", CultureInfo.InvariantCulture) + "%";
    }

    private static string ShortenName(string name)
    {
        return name.StartsWith(NamespacePrefix, StringComparison.Ordinal)
            ? name[NamespacePrefix.Length..]
            : name;
    }

    private sealed record ComparisonOptions(
        string BaselinePath,
        string CandidatePath,
        double MaximumRegressionPercent,
        bool FailOnAllocationIncrease)
    {
        internal static ComparisonOptions Parse(string[] args)
        {
            if (args.Length < 2)
            {
                throw new ArgumentException(
                    "Usage: compare <baseline-report-path> <candidate-report-path> [--max-regression <percent>] [--fail-on-allocation-increase]");
            }

            double maximumRegressionPercent = double.PositiveInfinity;
            bool failOnAllocationIncrease = false;

            for (int i = 2; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--max-regression" when i + 1 < args.Length:
                        if (!double.TryParse(args[++i], NumberStyles.Float, CultureInfo.InvariantCulture, out maximumRegressionPercent)
                            || maximumRegressionPercent < 0)
                        {
                            throw new ArgumentException("The maximum regression must be a nonnegative percentage.");
                        }
                        break;
                    case "--fail-on-allocation-increase":
                        failOnAllocationIncrease = true;
                        break;
                    default:
                        throw new ArgumentException($"Unknown comparison option: {args[i]}");
                }
            }

            return new ComparisonOptions(
                args[0],
                args[1],
                maximumRegressionPercent,
                failOnAllocationIncrease);
        }
    }
}
