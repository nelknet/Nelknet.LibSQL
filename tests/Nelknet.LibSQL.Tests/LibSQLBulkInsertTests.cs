using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Nelknet.LibSQL.Data;
using Xunit;

namespace Nelknet.LibSQL.Tests;

public class LibSQLBulkInsertTests : IDisposable
{
    private readonly string _tempDbPath;
    private readonly LibSQLConnection _connection;

    public LibSQLBulkInsertTests()
    {
        _tempDbPath = Path.Combine(Path.GetTempPath(), $"bulktest_{Guid.NewGuid()}.db");
        _connection = new LibSQLConnection($"Data Source={_tempDbPath}");
        _connection.Open();
        CreateTestTable();
    }

    public void Dispose()
    {
        _connection?.Dispose();
        if (File.Exists(_tempDbPath))
        {
            try { File.Delete(_tempDbPath); } catch { }
        }
    }

    private void CreateTestTable()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE test_bulk (
                id INTEGER PRIMARY KEY,
                name TEXT,
                value REAL,
                created_at TEXT
            )";
        cmd.ExecuteNonQuery();
    }

    [Fact]
    public void BulkInsert_SimpleRows_ShouldInsertAll()
    {
        // Arrange
        var columns = new[] { "id", "name", "value", "created_at" };
        using var bulkInsert = new LibSQLBulkInsert(_connection, "test_bulk", columns);

        // Act
        bulkInsert.BeginBulkInsert();
        
        for (int i = 1; i <= 100; i++)
        {
            bulkInsert.WriteRow(i, $"Item {i}", i * 1.5, DateTime.Now.ToString("O"));
        }
        
        bulkInsert.Complete();

        // Assert
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM test_bulk";
        var count = Convert.ToInt32(cmd.ExecuteScalar());
        Assert.Equal(100, count);

        cmd.CommandText = "SELECT SUM(value) FROM test_bulk";
        var sum = Convert.ToDouble(cmd.ExecuteScalar());
        Assert.Equal(7575.0, sum); // Sum of 1.5 + 3.0 + 4.5 + ... + 150.0
    }

    [Fact]
    public async Task BulkInsert_AsyncOperations_ShouldWork()
    {
        // Arrange
        var columns = new[] { "id", "name", "value", "created_at" };
        using var bulkInsert = new LibSQLBulkInsert(_connection, "test_bulk", columns);

        // Act
        await bulkInsert.BeginBulkInsertAsync();
        
        for (int i = 1; i <= 50; i++)
        {
            await bulkInsert.WriteRowAsync(i, $"Async Item {i}", i * 2.0, DateTime.Now.ToString("O"));
        }
        
        await bulkInsert.CompleteAsync();

        // Assert
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM test_bulk WHERE name LIKE 'Async%'";
        var count = Convert.ToInt32(cmd.ExecuteScalar());
        Assert.Equal(50, count);
    }

    [Fact]
    public void BulkInsert_WithBatching_ShouldCommitInBatches()
    {
        // Arrange
        var columns = new[] { "id", "name", "value", "created_at" };
        using var bulkInsert = new LibSQLBulkInsert(_connection, "test_bulk", columns)
        {
            BatchSize = 25,
            UseTransaction = true
        };

        // Act
        bulkInsert.BeginBulkInsert();
        
        for (int i = 1; i <= 100; i++)
        {
            bulkInsert.WriteRow(i, $"Batch Item {i}", i * 0.5, DateTime.Now.ToString("O"));
        }
        
        bulkInsert.Complete();

        // Assert
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM test_bulk";
        var count = Convert.ToInt32(cmd.ExecuteScalar());
        Assert.Equal(100, count);
    }

    [Fact]
    public void BulkInsert_WithNullValues_ShouldHandleNulls()
    {
        // Arrange
        var columns = new[] { "id", "name", "value", "created_at" };
        using var bulkInsert = new LibSQLBulkInsert(_connection, "test_bulk", columns);

        // Act
        bulkInsert.BeginBulkInsert();
        bulkInsert.WriteRow(1, null, null, null);
        bulkInsert.WriteRow(2, "Item 2", 2.5, DateTime.Now.ToString("O"));
        bulkInsert.WriteRow(3, "Item 3", null, null);
        bulkInsert.Complete();

        // Assert
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM test_bulk WHERE name IS NULL";
        var nullCount = Convert.ToInt32(cmd.ExecuteScalar());
        Assert.Equal(1, nullCount);

        cmd.CommandText = "SELECT COUNT(*) FROM test_bulk WHERE value IS NULL";
        var nullValueCount = Convert.ToInt32(cmd.ExecuteScalar());
        Assert.Equal(2, nullValueCount);
    }

    [Fact]
    public void BulkInsert_Abort_ShouldRollback()
    {
        // Arrange
        var columns = new[] { "id", "name", "value", "created_at" };
        using var bulkInsert = new LibSQLBulkInsert(_connection, "test_bulk", columns)
        {
            UseTransaction = true
        };

        // Act
        bulkInsert.BeginBulkInsert();
        
        for (int i = 1; i <= 10; i++)
        {
            bulkInsert.WriteRow(i, $"Abort Item {i}", i * 1.0, DateTime.Now.ToString("O"));
        }
        
        bulkInsert.Abort();

        // Assert
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM test_bulk";
        var count = Convert.ToInt32(cmd.ExecuteScalar());
        Assert.Equal(0, count); // Should be rolled back
    }

    [Fact]
    public void BulkInsert_FromDataTable_ShouldWork()
    {
        // Arrange
        var dataTable = new DataTable("test_bulk");
        dataTable.Columns.Add("id", typeof(int));
        dataTable.Columns.Add("name", typeof(string));
        dataTable.Columns.Add("value", typeof(double));
        dataTable.Columns.Add("created_at", typeof(string));

        for (int i = 1; i <= 25; i++)
        {
            dataTable.Rows.Add(i, $"DataTable Item {i}", i * 3.0, DateTime.Now.ToString("O"));
        }

        // Act
        LibSQLBulkInsert.BulkInsertDataTable(_connection, dataTable);

        // Assert
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM test_bulk WHERE name LIKE 'DataTable%'";
        var count = Convert.ToInt32(cmd.ExecuteScalar());
        Assert.Equal(25, count);
    }

    [Fact]
    public void BulkInsert_InvalidColumnCount_ShouldThrow()
    {
        // Arrange
        var columns = new[] { "id", "name", "value", "created_at" };
        using var bulkInsert = new LibSQLBulkInsert(_connection, "test_bulk", columns);

        // Act & Assert
        bulkInsert.BeginBulkInsert();
        
        var ex = Assert.Throws<ArgumentException>(() => 
            bulkInsert.WriteRow(1, "Too few values")); // Only 2 values for 4 columns
        
        Assert.Contains("Expected 4 values but got 2", ex.Message);
    }

    [Fact]
    public void BulkInsert_WriteRowsMethod_ShouldInsertMultiple()
    {
        // Arrange
        var columns = new[] { "id", "name", "value", "created_at" };
        using var bulkInsert = new LibSQLBulkInsert(_connection, "test_bulk", columns);

        var rows = Enumerable.Range(1, 20).Select(i => new object?[]
        {
            i,
            $"Multi Item {i}",
            i * 0.25,
            DateTime.Now.ToString("O")
        }).ToList();

        // Act
        bulkInsert.BeginBulkInsert();
        bulkInsert.WriteRows(rows);
        bulkInsert.Complete();

        // Assert
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM test_bulk WHERE name LIKE 'Multi%'";
        var count = Convert.ToInt32(cmd.ExecuteScalar());
        Assert.Equal(20, count);
    }

    [Fact]
    public void BulkInsert_WithoutTransaction_ShouldStillWork()
    {
        // Arrange
        var columns = new[] { "id", "name", "value", "created_at" };
        using var bulkInsert = new LibSQLBulkInsert(_connection, "test_bulk", columns)
        {
            UseTransaction = false
        };

        // Act
        bulkInsert.BeginBulkInsert();
        
        for (int i = 1; i <= 15; i++)
        {
            bulkInsert.WriteRow(i, $"No Transaction Item {i}", i * 4.0, DateTime.Now.ToString("O"));
        }
        
        bulkInsert.Complete();

        // Assert
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM test_bulk";
        var count = Convert.ToInt32(cmd.ExecuteScalar());
        Assert.Equal(15, count);
    }

    [Fact]
    public void BulkInsert_FromDataReader_ShouldCopyData()
    {
        // Arrange - Insert some initial data
        using (var cmd = _connection.CreateCommand())
        {
            for (int i = 1; i <= 10; i++)
            {
                cmd.CommandText = "INSERT INTO test_bulk (id, name, value, created_at) VALUES (@id, @name, @value, @created)";
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@id", i);
                cmd.Parameters.AddWithValue("@name", $"Source Item {i}");
                cmd.Parameters.AddWithValue("@value", i * 5.0);
                cmd.Parameters.AddWithValue("@created", DateTime.Now.ToString("O"));
                cmd.ExecuteNonQuery();
            }
        }

        // Create a second table
        using (var cmd = _connection.CreateCommand())
        {
            cmd.CommandText = @"
                CREATE TABLE test_bulk_copy (
                    id INTEGER PRIMARY KEY,
                    name TEXT,
                    value REAL,
                    created_at TEXT
                )";
            cmd.ExecuteNonQuery();
        }

        // Act - Copy data using bulk insert
        using (var cmd = _connection.CreateCommand())
        {
            cmd.CommandText = "SELECT id + 100, name || ' (Copy)', value * 2, created_at FROM test_bulk";
            using var reader = cmd.ExecuteReader();
            
            var columns = new[] { "id", "name", "value", "created_at" };
            using var bulkInsert = new LibSQLBulkInsert(_connection, "test_bulk_copy", columns);
            
            bulkInsert.BeginBulkInsert();
            bulkInsert.WriteFromReader(reader);
            bulkInsert.Complete();
        }

        // Assert
        using (var cmd = _connection.CreateCommand())
        {
            cmd.CommandText = "SELECT COUNT(*) FROM test_bulk_copy";
            var count = Convert.ToInt32(cmd.ExecuteScalar());
            Assert.Equal(10, count);

            cmd.CommandText = "SELECT name FROM test_bulk_copy WHERE id = 101";
            var name = cmd.ExecuteScalar()?.ToString();
            Assert.Equal("Source Item 1 (Copy)", name);
        }
    }
}