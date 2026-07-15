using Nelknet.LibSQL.Data;

namespace Nelknet.LibSQL.Tests;

public sealed class ReturningClauseTests : IDisposable
{
    private readonly string _directoryPath;
    private readonly string _databasePath;

    public ReturningClauseTests()
    {
        _directoryPath = Path.Combine(Path.GetTempPath(), $"libsql_returning_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_directoryPath);
        _databasePath = Path.Combine(_directoryPath, "test.db");
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
    public void ExecuteReader_ParameterizedInsertReturningReadOnce_PersistsAfterCommit()
    {
        using var connection = OpenConnection();
        CreateItemsTable(connection);

        using var transaction = connection.BeginTransaction();
        using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = "INSERT INTO Items (Name) VALUES (@name) RETURNING Id";
            command.Parameters.Add(new LibSQLParameter("@name", "ada"));

            using var reader = command.ExecuteReader();
            Assert.True(reader.Read());
            Assert.Equal(1L, reader.GetInt64(0));
        }

        transaction.Commit();

        Assert.False(File.Exists(_databasePath + "-journal"));
        Assert.Equal(1L, CountItems(connection));
    }

    [Fact]
    public void ExecuteReader_LiteralInsertReturningReadOnce_PersistsAfterCommit()
    {
        using var connection = OpenConnection();
        CreateItemsTable(connection);

        using var transaction = connection.BeginTransaction();
        using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = "INSERT INTO Items (Name) VALUES ('grace') RETURNING Id";

            using var reader = command.ExecuteReader();
            Assert.True(reader.Read());
            Assert.Equal(1L, reader.GetInt64(0));
        }

        transaction.Commit();

        Assert.False(File.Exists(_databasePath + "-journal"));
        Assert.Equal(1L, CountItems(connection));
    }

    [Fact]
    public void ExecuteReader_MultiRowInsertReturningReadOnce_PersistsEveryRow()
    {
        using var connection = OpenConnection();
        CreateItemsTable(connection);

        using var transaction = connection.BeginTransaction();
        using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText =
                "INSERT INTO Items (Name) VALUES ('ada'), ('grace'), ('linus') RETURNING Id";

            using var reader = command.ExecuteReader();
            Assert.True(reader.Read());
            Assert.Equal(1L, reader.GetInt64(0));
        }

        transaction.Commit();

        Assert.False(File.Exists(_databasePath + "-journal"));
        Assert.Equal(3L, CountItems(connection));
    }

    [Fact]
    public void ExecuteScalar_ParameterizedInsertReturning_PersistsAfterCommit()
    {
        using var connection = OpenConnection();
        CreateItemsTable(connection);

        using var transaction = connection.BeginTransaction();
        using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = "INSERT INTO Items (Name) VALUES (@name) RETURNING Id";
            command.Parameters.Add(new LibSQLParameter("@name", "scalar"));

            Assert.Equal(1L, Convert.ToInt64(command.ExecuteScalar()));
        }

        transaction.Commit();

        Assert.False(File.Exists(_databasePath + "-journal"));
        Assert.Equal(1L, CountItems(connection));
    }

    [Fact]
    public void ExecuteReader_ParameterizedInsertReturning_PersistsAfterReconnect()
    {
        using (var connection = OpenConnection())
        {
            CreateItemsTable(connection);

            using var transaction = connection.BeginTransaction();
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = "INSERT INTO Items (Name) VALUES (@name) RETURNING Id";
                command.Parameters.Add(new LibSQLParameter("@name", "linus"));

                using var reader = command.ExecuteReader();
                Assert.True(reader.Read());
                Assert.Equal(1L, reader.GetInt64(0));
            }

            transaction.Commit();
        }

        Assert.False(File.Exists(_databasePath + "-journal"));

        using var reopenedConnection = OpenConnection();
        Assert.Equal(1L, CountItems(reopenedConnection));
    }

    private LibSQLConnection OpenConnection()
    {
        var connection = new LibSQLConnection($"Data Source={_databasePath}");
        connection.Open();
        return connection;
    }

    private static void CreateItemsTable(LibSQLConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            "CREATE TABLE Items (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL)";
        command.ExecuteNonQuery();
    }

    private static long CountItems(LibSQLConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM Items";
        return Convert.ToInt64(command.ExecuteScalar());
    }
}
