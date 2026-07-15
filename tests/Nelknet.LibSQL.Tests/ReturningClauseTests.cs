using System;
using System.IO;
using Nelknet.LibSQL.Data;
using Xunit;

namespace Nelknet.LibSQL.Tests;

/// <summary>
/// Regression coverage for INSERT…RETURNING under an open transaction.
/// Consumers such as EF Core SaveChanges typically Read() once, dispose the
/// reader without a final Read() to SQLITE_DONE, then Commit — the reader must
/// drain remaining rows on Close or the write stays "in progress" and rolls back.
/// </summary>
public sealed class ReturningClauseTests : IDisposable
{
    private readonly string _tempPath;
    private readonly string _dbPath;

    public ReturningClauseTests()
    {
        _tempPath = Path.Combine(Path.GetTempPath(), $"libsql_returning_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempPath);
        _dbPath = Path.Combine(_tempPath, "test.db");
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempPath))
            {
                Directory.Delete(_tempPath, recursive: true);
            }
        }
        catch
        {
            // Best-effort cleanup when the native library still holds the file.
        }
    }

    [Fact]
    public void InsertReturning_ReadOnce_DisposeReader_ThenCommit_PersistsRow()
    {
        using var connection = new LibSQLConnection($"Data Source={_dbPath}");
        connection.Open();
        CreateItemsTable(connection);

        using var transaction = connection.BeginTransaction();
        long returnedId;
        using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = "INSERT INTO Items (Name) VALUES (@name) RETURNING Id";
            var parameter = command.CreateParameter();
            parameter.ParameterName = "@name";
            parameter.Value = "ada";
            command.Parameters.Add(parameter);

            // EF-style: single Read, then dispose — no extra Read() to DONE.
            using (var reader = command.ExecuteReader())
            {
                Assert.True(reader.Read());
                returnedId = reader.GetInt64(0);
                Assert.Equal(1L, returnedId);
            }
        }

        transaction.Commit();

        Assert.False(File.Exists(_dbPath + "-journal"), "rollback journal should not remain after commit");
        Assert.Equal(1L, ScalarCount(connection));
    }

    [Fact]
    public void InsertReturning_LiteralValues_ReadOnce_DisposeReader_ThenCommit_PersistsRow()
    {
        using var connection = new LibSQLConnection($"Data Source={_dbPath}");
        connection.Open();
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

        Assert.False(File.Exists(_dbPath + "-journal"));
        Assert.Equal(1L, ScalarCount(connection));
    }

    [Fact]
    public void InsertReturning_AfterConnectionClose_RowSurvivesReconnect()
    {
        using (var connection = new LibSQLConnection($"Data Source={_dbPath}"))
        {
            connection.Open();
            CreateItemsTable(connection);

            using var transaction = connection.BeginTransaction();
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = "INSERT INTO Items (Name) VALUES (@name) RETURNING Id";
                var parameter = command.CreateParameter();
                parameter.ParameterName = "@name";
                parameter.Value = "linus";
                command.Parameters.Add(parameter);

                using var reader = command.ExecuteReader();
                Assert.True(reader.Read());
                Assert.Equal(1L, reader.GetInt64(0));
            }

            transaction.Commit();
        }

        Assert.False(File.Exists(_dbPath + "-journal"));

        using var reopen = new LibSQLConnection($"Data Source={_dbPath}");
        reopen.Open();
        Assert.Equal(1L, ScalarCount(reopen));

        using var select = reopen.CreateCommand();
        select.CommandText = "SELECT Name FROM Items WHERE Id = 1";
        Assert.Equal("linus", select.ExecuteScalar());
    }

    [Fact]
    public void InsertReturning_ExecuteScalar_UnderTransaction_PersistsRow()
    {
        using var connection = new LibSQLConnection($"Data Source={_dbPath}");
        connection.Open();
        CreateItemsTable(connection);

        using var transaction = connection.BeginTransaction();
        using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = "INSERT INTO Items (Name) VALUES (@name) RETURNING Id";
            var parameter = command.CreateParameter();
            parameter.ParameterName = "@name";
            parameter.Value = "scalar";
            command.Parameters.Add(parameter);

            Assert.Equal(1L, Convert.ToInt64(command.ExecuteScalar()));
        }

        transaction.Commit();

        Assert.False(File.Exists(_dbPath + "-journal"));
        Assert.Equal(1L, ScalarCount(connection));
    }

    private static void CreateItemsTable(LibSQLConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            "CREATE TABLE Items (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL)";
        command.ExecuteNonQuery();
    }

    private static long ScalarCount(LibSQLConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM Items";
        return Convert.ToInt64(command.ExecuteScalar());
    }
}
