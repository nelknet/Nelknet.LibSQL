using Nelknet.LibSQL.Data;

namespace Nelknet.LibSQL.Tests;

public sealed class DataReaderCloseTests : IDisposable
{
    private readonly string _directoryPath;
    private readonly string _databasePath;

    public DataReaderCloseTests()
    {
        _directoryPath = Path.Combine(Path.GetTempPath(), $"libsql_reader_close_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_directoryPath);
        _databasePath = Path.Combine(_directoryPath, "test.db");

        using var setupConnection = OpenConnection();
        using var setupCommand = setupConnection.CreateCommand();
        setupCommand.CommandText =
            "CREATE TABLE Items (Id INTEGER PRIMARY KEY, Name TEXT NOT NULL)";
        setupCommand.ExecuteNonQuery();

        setupCommand.CommandText = "INSERT INTO Items (Id, Name) VALUES (1, 'ada')";
        setupCommand.ExecuteNonQuery();
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_directoryPath, recursive: true);
        }
        catch (DirectoryNotFoundException)
        {
        }
        catch (IOException)
        {
        }
    }

    [Fact]
    public void ExecuteReader_LiteralSelectDisposedBeforeEnd_ReleasesLockForOtherConnection()
    {
        using var readerConnection = OpenConnection();
        using (var readCommand = readerConnection.CreateCommand())
        {
            readCommand.CommandText = "SELECT Id, Name FROM Items WHERE Id = 1";
            using var reader = readCommand.ExecuteReader();
            Assert.True(reader.Read());
            Assert.Equal(1L, reader.GetInt64(0));
        }

        AssertOtherConnectionCanWrite();
    }

    [Fact]
    public void ExecuteReader_ParameterizedSelectDisposedBeforeEnd_ReleasesLockForOtherConnection()
    {
        using var readerConnection = OpenConnection();
        using (var readCommand = readerConnection.CreateCommand())
        {
            readCommand.CommandText = "SELECT Id, Name FROM Items WHERE Id = @id";
            readCommand.Parameters.Add(new LibSQLParameter("@id", 1));

            using var reader = readCommand.ExecuteReader();
            Assert.True(reader.Read());
            Assert.Equal(1L, reader.GetInt64(0));
        }

        AssertOtherConnectionCanWrite();
    }

    [Fact]
    public void ExecuteScalar_SelectReturnsValue_ReleasesLockForOtherConnection()
    {
        using var readerConnection = OpenConnection();
        using (var readCommand = readerConnection.CreateCommand())
        {
            readCommand.CommandText = "SELECT Name FROM Items WHERE Id = 1";
            Assert.Equal("ada", readCommand.ExecuteScalar());
        }

        AssertOtherConnectionCanWrite();
    }

    private void AssertOtherConnectionCanWrite()
    {
        using var writerConnection = OpenConnection();
        using var writeCommand = writerConnection.CreateCommand();
        writeCommand.CommandText = "UPDATE Items SET Name = 'grace' WHERE Id = 1";

        Assert.Equal(1, writeCommand.ExecuteNonQuery());
    }

    private LibSQLConnection OpenConnection()
    {
        var connection = new LibSQLConnection($"Data Source={_databasePath}");
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA busy_timeout = 100";
        command.ExecuteScalar();

        return connection;
    }
}
