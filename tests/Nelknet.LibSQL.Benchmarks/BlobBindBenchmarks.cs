using BenchmarkDotNet.Attributes;
using Nelknet.LibSQL.Data;

namespace Nelknet.LibSQL.Benchmarks;

[BenchmarkCategory("Blob", "Native")]
public class BlobBindBenchmarks : IDisposable
{
    private LibSQLConnection _connection = null!;
    private LibSQLCommand _command = null!;

    [Params(0, 16, 4 * 1024, 1024 * 1024)]
    public int BlobSize { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var blob = GC.AllocateUninitializedArray<byte>(BlobSize);
        blob.AsSpan().Fill(0x5a);

        _connection = new LibSQLConnection("Data Source=:memory:");
        _connection.Open();

        _command = new LibSQLCommand("SELECT length(@value)", _connection);
        _command.Parameters.AddWithValue("@value", blob);
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

    public void Dispose()
    {
        _command?.Dispose();
        _connection?.Dispose();
        GC.SuppressFinalize(this);
    }
}
