using System;
using System.IO;
using Xunit;
using Nelknet.LibSQL.Data;

namespace Nelknet.LibSQL.Tests;

public class LibSQLBackupTests : IDisposable
{
    private readonly string _sourceDbPath;
    private readonly string _destDbPath;
    
    public LibSQLBackupTests()
    {
        _sourceDbPath = Path.Combine(Path.GetTempPath(), $"source_{Guid.NewGuid()}.db");
        _destDbPath = Path.Combine(Path.GetTempPath(), $"dest_{Guid.NewGuid()}.db");
    }
    
    public void Dispose()
    {
        if (File.Exists(_sourceDbPath))
            File.Delete(_sourceDbPath);
        if (File.Exists(_destDbPath))
            File.Delete(_destDbPath);
    }
    
    [Fact]
    public void BackupDatabase_WithValidConnections_CopiesData()
    {
        // Arrange
        using var sourceConn = new LibSQLConnection($"Data Source={_sourceDbPath}");
        sourceConn.Open();
        
        // Create test data
        using (var cmd = sourceConn.CreateCommand())
        {
            cmd.CommandText = @"
                CREATE TABLE test (id INTEGER PRIMARY KEY, value TEXT);
                INSERT INTO test (value) VALUES ('row1'), ('row2'), ('row3');
            ";
            cmd.ExecuteNonQuery();
        }
        
        using var destConn = new LibSQLConnection($"Data Source={_destDbPath}");
        destConn.Open();
        
        // Act
        sourceConn.BackupDatabase(destConn);
        
        // Assert - verify data was copied
        using (var cmd = destConn.CreateCommand())
        {
            cmd.CommandText = "SELECT COUNT(*) FROM test";
            var count = cmd.ExecuteScalar();
            Assert.Equal(3L, count);
        }
        
        using (var cmd = destConn.CreateCommand())
        {
            cmd.CommandText = "SELECT value FROM test WHERE id = 2";
            var value = cmd.ExecuteScalar();
            Assert.Equal("row2", value);
        }
    }
    
    [Fact]
    public void BackupDatabase_WithProgress_ReportsProgress()
    {
        // Arrange
        using var sourceConn = new LibSQLConnection($"Data Source={_sourceDbPath}");
        sourceConn.Open();
        
        // Create larger dataset to ensure multiple progress callbacks
        using (var cmd = sourceConn.CreateCommand())
        {
            cmd.CommandText = "CREATE TABLE test (id INTEGER PRIMARY KEY, data BLOB)";
            cmd.ExecuteNonQuery();
            
            // Insert some data
            for (int i = 0; i < 100; i++)
            {
                cmd.CommandText = $"INSERT INTO test (data) VALUES (randomblob(1024))";
                cmd.ExecuteNonQuery();
            }
        }
        
        using var destConn = new LibSQLConnection($"Data Source={_destDbPath}");
        destConn.Open();
        
        bool progressCalled = false;
        int lastProgress = -1;
        
        // Act
        sourceConn.BackupDatabase(destConn, "main", "main", 10, (current, total) =>
        {
            progressCalled = true;
            Assert.True(current >= 0);
            Assert.True(total > 0);
            Assert.True(current <= total);
            
            // Ensure progress is monotonic
            if (lastProgress >= 0)
            {
                Assert.True(current >= lastProgress);
            }
            lastProgress = current;
        });
        
        // Assert
        Assert.True(progressCalled, "Progress callback should have been called");
    }
    
    [Fact]
    public void BackupDatabase_WithMemoryDatabase_Works()
    {
        // Arrange
        using var sourceConn = new LibSQLConnection("Data Source=:memory:");
        sourceConn.Open();
        
        using (var cmd = sourceConn.CreateCommand())
        {
            cmd.CommandText = @"
                CREATE TABLE test (id INTEGER PRIMARY KEY, value TEXT);
                INSERT INTO test (value) VALUES ('memory data');
            ";
            cmd.ExecuteNonQuery();
        }
        
        using var destConn = new LibSQLConnection($"Data Source={_destDbPath}");
        destConn.Open();
        
        // Act
        sourceConn.BackupDatabase(destConn);
        
        // Assert
        using (var cmd = destConn.CreateCommand())
        {
            cmd.CommandText = "SELECT value FROM test";
            var value = cmd.ExecuteScalar();
            Assert.Equal("memory data", value);
        }
    }
    
    [Fact]
    public void BackupDatabase_WithClosedConnection_ThrowsException()
    {
        // Arrange
        using var sourceConn = new LibSQLConnection($"Data Source={_sourceDbPath}");
        using var destConn = new LibSQLConnection($"Data Source={_destDbPath}");
        
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => 
            sourceConn.BackupDatabase(destConn));
    }
    
    [Fact]
    public void BackupDatabase_WithProgressEvent_RaisesEvent()
    {
        // Arrange
        using var sourceConn = new LibSQLConnection($"Data Source={_sourceDbPath}");
        sourceConn.Open();
        
        using (var cmd = sourceConn.CreateCommand())
        {
            cmd.CommandText = @"
                CREATE TABLE test (id INTEGER PRIMARY KEY, value TEXT);
                INSERT INTO test (value) VALUES ('test1'), ('test2');
            ";
            cmd.ExecuteNonQuery();
        }
        
        using var destConn = new LibSQLConnection($"Data Source={_destDbPath}");
        destConn.Open();
        
        bool eventRaised = false;
        sourceConn.Progress += (sender, e) =>
        {
            eventRaised = true;
            Assert.Equal("Database backup in progress", e.Message);
            Assert.True(e.PercentComplete >= 0 && e.PercentComplete <= 100);
        };
        
        // Act
        sourceConn.BackupDatabase(destConn);
        
        // Assert
        Assert.True(eventRaised, "Progress event should have been raised");
    }
}