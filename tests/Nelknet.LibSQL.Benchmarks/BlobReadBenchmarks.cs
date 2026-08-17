using BenchmarkDotNet.Attributes;
using Nelknet.LibSQL.Data;

namespace Nelknet.LibSQL.Benchmarks;

[BenchmarkCategory("Blob", "Native")]
public class BlobReadBenchmarks : IDisposable
{
    private const int SliceSize = 4 * 1024;

    private LibSQLConnection _connection = null!;
    private LibSQLCommand _readCommand = null!;
    private byte[] _fullBuffer = null!;
    private byte[] _sliceBuffer = null!;
    private long _sliceOffset;

    [Params(16, 4 * 1024, 1024 * 1024)]
    public int BlobSize { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var blob = GC.AllocateUninitializedArray<byte>(BlobSize);
        blob.AsSpan().Fill(0x5a);

        _connection = new LibSQLConnection("Data Source=:memory:");
        _connection.Open();

        using (var createCommand = new LibSQLCommand("CREATE TABLE benchmark_blobs (value BLOB NOT NULL)", _connection))
        {
            createCommand.ExecuteNonQuery();
        }

        using (var insertCommand = new LibSQLCommand("INSERT INTO benchmark_blobs (value) VALUES (@value)", _connection))
        {
            insertCommand.Parameters.AddWithValue("@value", blob);
            insertCommand.ExecuteNonQuery();
        }

        _readCommand = new LibSQLCommand("SELECT value FROM benchmark_blobs LIMIT 1", _connection);
        _fullBuffer = GC.AllocateUninitializedArray<byte>(BlobSize);
        _sliceBuffer = GC.AllocateUninitializedArray<byte>(Math.Min(BlobSize, SliceSize));
        _sliceOffset = BlobSize > SliceSize ? (BlobSize - SliceSize) / 2 : 0;
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        Dispose();
    }

    [Benchmark]
    public long GetBytesLength()
    {
        using var reader = OpenReader();
        return reader.GetBytes(0, 0, null, 0, 0);
    }

    [Benchmark]
    public long GetBytesSlice()
    {
        using var reader = OpenReader();
        return reader.GetBytes(0, _sliceOffset, _sliceBuffer, 0, _sliceBuffer.Length);
    }

    [Benchmark]
    public long GetBytesFull()
    {
        using var reader = OpenReader();
        return reader.GetBytes(0, 0, _fullBuffer, 0, _fullBuffer.Length);
    }

    private LibSQLDataReader OpenReader()
    {
        var reader = _readCommand.ExecuteReader();
        if (reader.Read())
        {
            return reader;
        }

        reader.Dispose();
        throw new InvalidOperationException("The BLOB benchmark query returned no rows.");
    }

    public void Dispose()
    {
        _readCommand?.Dispose();
        _connection?.Dispose();
        GC.SuppressFinalize(this);
    }
}
