using Nelknet.LibSQL.Data;

namespace Nelknet.LibSQL.Tests;

public sealed class LibSQLDataReaderBlobTests
{
    [Fact]
    public void GetValue_EmptyBlob_ReturnsEmptyByteArray()
    {
        using var connection = new LibSQLConnection("Data Source=:memory:");
        connection.Open();

        using var command = new LibSQLCommand("SELECT X''", connection);
        using var reader = command.ExecuteReader();

        Assert.True(reader.Read());
        Assert.Empty(Assert.IsType<byte[]>(reader.GetValue(0)));
    }

    [Fact]
    public void GetBytes_BlobWithNullBuffer_ReturnsBlobLength()
    {
        WithValueReader("X'000102030405'", reader =>
        {
            Assert.Equal(6, reader.GetBytes(0, 0, null, 0, 0));
        });
    }

    [Fact]
    public void GetBytes_BlobSlice_CopiesRequestedBytes()
    {
        WithValueReader("X'000102030405'", reader =>
        {
            var buffer = new byte[3];

            var bytesRead = reader.GetBytes(0, 2, buffer, 0, buffer.Length);

            Assert.Equal(3, bytesRead);
            Assert.Equal(new byte[] { 2, 3, 4 }, buffer);
        });
    }

    [Fact]
    public void GetBytes_BufferOffset_PreservesOtherBufferBytes()
    {
        WithValueReader("X'000102030405'", reader =>
        {
            var buffer = new byte[] { 0xff, 0xff, 0xff, 0xff, 0xff };

            var bytesRead = reader.GetBytes(0, 1, buffer, 2, 2);

            Assert.Equal(2, bytesRead);
            Assert.Equal(new byte[] { 0xff, 0xff, 1, 2, 0xff }, buffer);
        });
    }

    [Fact]
    public void GetBytes_DataOffsetAtEnd_ReturnsZero()
    {
        WithValueReader("X'000102'", reader =>
        {
            var buffer = new byte[] { 0xff };

            var bytesRead = reader.GetBytes(0, 3, buffer, 0, buffer.Length);

            Assert.Equal(0, bytesRead);
            Assert.Equal(0xff, buffer[0]);
        });
    }

    [Fact]
    public void GetBytes_EmptyBlob_ReturnsZero()
    {
        WithValueReader("X''", reader =>
        {
            Assert.Equal(0, reader.GetBytes(0, 0, null, 0, 0));
            Assert.Equal(0, reader.GetBytes(0, 0, Array.Empty<byte>(), 0, 0));
        });
    }

    [Fact]
    public void GetBytes_NegativeDataOffset_ThrowsArgumentOutOfRangeException()
    {
        WithValueReader("X'00'", reader =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => reader.GetBytes(0, -1, new byte[1], 0, 1));
        });
    }

    private static void WithValueReader(string sqlValue, Action<LibSQLDataReader> assertion)
    {
        using var connection = new LibSQLConnection("Data Source=:memory:");
        connection.Open();

        using var command = new LibSQLCommand($"SELECT {sqlValue}", connection);
        using var reader = command.ExecuteReader();

        Assert.True(reader.Read());
        assertion(reader);
    }
}
