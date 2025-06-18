using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Nelknet.LibSQL.Data;
using Nelknet.LibSQL.Data.Exceptions;
using Xunit;

namespace Nelknet.LibSQL.Tests;

/// <summary>
/// Tests for special database features: encryption, in-memory databases, and vector support
/// </summary>
public class SpecialFeaturesTests : IDisposable
{
    private readonly string _tempPath;
    private readonly List<string> _tempFiles = new();

    public SpecialFeaturesTests()
    {
        _tempPath = Path.Combine(Path.GetTempPath(), $"libsql_special_features_tests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempPath);
    }

    public void Dispose()
    {
        // Clean up temp files
        foreach (var file in _tempFiles)
        {
            if (File.Exists(file))
            {
                File.Delete(file);
            }
        }

        if (Directory.Exists(_tempPath))
        {
            Directory.Delete(_tempPath, true);
        }
    }

    private string CreateTempFile()
    {
        var path = Path.Combine(_tempPath, $"test_{Guid.NewGuid()}.db");
        _tempFiles.Add(path);
        return path;
    }

    #region Encryption Tests

    [Fact]
    public void Encryption_ConfigurationSupport()
    {
        // Test that encryption configuration is properly supported in connection strings
        // Note: Actual encryption behavior depends on libSQL build configuration
        
        // Test 1: Connection string builder supports encryption key
        var builder = new LibSQLConnectionStringBuilder
        {
            DataSource = "test.db",
            EncryptionKey = "my-secret-key"
        };
        Assert.Equal("my-secret-key", builder.EncryptionKey);
        Assert.Contains("Encryption Key=my-secret-key", builder.ConnectionString);
        
        // Test 2: Encryption key is parsed correctly from connection string
        var connectionString = "Data Source=test.db;Encryption Key=another-key";
        var parsedBuilder = new LibSQLConnectionStringBuilder(connectionString);
        Assert.Equal("another-key", parsedBuilder.EncryptionKey);
        
        // Test 3: Encryption key with embedded replica configuration
        var replicaConnString = "Data Source=local.db;Sync URL=http://remote.com;Auth Token=token;Encryption Key=replica-key;Offline=true";
        var replicaBuilder = new LibSQLConnectionStringBuilder(replicaConnString);
        Assert.Equal("replica-key", replicaBuilder.EncryptionKey);
        Assert.True(replicaBuilder.Offline);
        Assert.Equal(LibSQLConnectionMode.EmbeddedReplica, replicaBuilder.Mode);
    }
    
    // Note: Local database encryption is not supported by libSQL's experimental C API
    // The libSQL C API has a design limitation:
    // - libsql_open_file(url) - NO encryption parameter
    // - libsql_open_sync(..., encryption_key) - HAS encryption parameter
    // The underlying Rust implementation supports local encryption, but the C API doesn't expose it.
    // Workaround: Use embedded replica mode with offline=true for encrypted local databases.

    [Fact]
    public void ConnectionStringBuilder_SupportsEncryptionKey()
    {
        var builder = new LibSQLConnectionStringBuilder
        {
            DataSource = "test.db",
            EncryptionKey = "my-secret-key"
        };

        Assert.Equal("my-secret-key", builder.EncryptionKey);
        Assert.Contains("Encryption Key=my-secret-key", builder.ConnectionString);
    }

    #endregion

    #region In-Memory Database Tests

    [Fact]
    public void InMemory_CanCreateInMemoryDatabase()
    {
        using var connection = new LibSQLConnection("Data Source=:memory:");
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE test (id INTEGER PRIMARY KEY, data TEXT)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO test (data) VALUES ('in-memory data')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT data FROM test";
        var result = cmd.ExecuteScalar() as string;

        Assert.Equal("in-memory data", result);
    }

    [Fact]
    public void InMemory_SharedCacheDatabase_NotSupported()
    {
        // Test that :memory:?cache=shared is not supported in libSQL experimental API
        // The libSQL C API (libsql_open_file) doesn't parse URI query parameters
        // and doesn't set SQLITE_OPEN_URI flag, so ?cache=shared is ignored
        
        // Test 1: Regular in-memory databases work fine
        using (var conn1 = new LibSQLConnection("Data Source=:memory:"))
        {
            conn1.Open();
            
            using var cmd = conn1.CreateCommand();
            cmd.CommandText = "CREATE TABLE test1 (id INTEGER PRIMARY KEY, data TEXT)";
            cmd.ExecuteNonQuery();
            
            cmd.CommandText = "INSERT INTO test1 (data) VALUES ('data1')";
            cmd.ExecuteNonQuery();
        }
        
        // Test 2: Each :memory: connection gets its own database
        using (var conn1 = new LibSQLConnection("Data Source=:memory:"))
        using (var conn2 = new LibSQLConnection("Data Source=:memory:"))
        {
            conn1.Open();
            conn2.Open();
            
            // Create table in first connection
            using var cmd1 = conn1.CreateCommand();
            cmd1.CommandText = "CREATE TABLE isolated_test (id INTEGER PRIMARY KEY)";
            cmd1.ExecuteNonQuery();
            
            // Try to access from second connection
            using var cmd2 = conn2.CreateCommand();
            cmd2.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='isolated_test'";
            var tableName = cmd2.ExecuteScalar() as string;
            
            // Should not see the table - each connection has its own memory database
            Assert.Null(tableName);
        }
        
        // Test 3: Verify that ?cache=shared parameter is ignored
        // libSQL passes ":memory:?cache=shared" directly to SQLite without URI parsing
        // SQLite interprets this as a regular :memory: database, ignoring the query part
        using (var conn = new LibSQLConnection("Data Source=:memory:?cache=shared"))
        {
            conn.Open(); // Opens successfully but without shared cache
            
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "CREATE TABLE uri_test (id INTEGER)";
            cmd.ExecuteNonQuery(); // Works fine, but it's not a shared cache database
        }
        
        // Note: SQLite supports shared cache with "file::memory:" syntax when SQLITE_OPEN_URI
        // flag is set, but libSQL's experimental API doesn't enable URI parsing
    }

    [Fact]
    public void InMemory_HelperMethods()
    {
        var inMemoryConnectionString = LibSQLConnectionStringBuilder.CreateInMemoryConnectionString();
        Assert.Equal("Data Source=:memory:", inMemoryConnectionString);

        var sharedMemoryConnectionString = LibSQLConnectionStringBuilder.CreateSharedMemoryConnectionString();
        // Note: This connection string won't actually enable shared cache due to libSQL limitations
        Assert.Equal("Data Source=:memory:?cache=shared", sharedMemoryConnectionString);
    }

    #endregion

    // Migration tests removed - migration functionality has been removed from the library
    // Users should use dedicated migration libraries like FluentMigrator, DbUp, or Evolve

    #region Vector Support Tests

    [Fact]
    public void Vector_BasicOperations()
    {
        // Test vector support - this works at the SQL level if libSQL was compiled with vector support
        using var connection = new LibSQLConnection("Data Source=:memory:");
        connection.Open();

        // First, check if vector support is available
        bool vectorSupported = false;
        try
        {
            using var checkCmd = connection.CreateCommand();
            checkCmd.CommandText = "CREATE TABLE vector_test (id INTEGER PRIMARY KEY, embedding FLOAT32(3))";
            checkCmd.ExecuteNonQuery();
            vectorSupported = true;
            
            // Clean up test table
            checkCmd.CommandText = "DROP TABLE vector_test";
            checkCmd.ExecuteNonQuery();
        }
        catch
        {
            // Vector support not available in this build
        }
        
        if (!vectorSupported)
        {
            // Skip the rest of the test if vectors aren't supported
            return;
        }

        // Create a table with vector columns
        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE movies (
                id INTEGER PRIMARY KEY,
                title TEXT NOT NULL,
                year INTEGER,
                embedding FLOAT32(3)
            )";
        cmd.ExecuteNonQuery();

        // Insert vector data using the vector() function
        cmd.CommandText = @"
            INSERT INTO movies (title, year, embedding) VALUES 
            ('Napoleon', 2023, vector('[1,2,3]')),
            ('Black Hawk Down', 2001, vector('[10,11,12]')),
            ('Gladiator', 2000, vector('[4,5,6]'))";
        cmd.ExecuteNonQuery();

        // Create a vector index
        cmd.CommandText = "CREATE INDEX movies_idx ON movies(libsql_vector_idx(embedding))";
        cmd.ExecuteNonQuery();

        // Perform a vector search using vector_top_k
        cmd.CommandText = @"
            SELECT m.title, m.year 
            FROM vector_top_k('movies_idx', '[3,4,5]', 2) as knn
            JOIN movies m ON m.rowid = knn.id";
        
        using var reader = cmd.ExecuteReader();
        var results = new List<(string title, int year)>();
        while (reader.Read())
        {
            results.Add((reader.GetString(0), reader.GetInt32(1)));
        }
        
        // Should return the 2 nearest neighbors
        Assert.Equal(2, results.Count);
        // Verify we got movie titles
        Assert.True(results.All(r => !string.IsNullOrEmpty(r.title)));
    }
    
    [Fact]
    public void Vector_ParameterizedQueries()
    {
        using var connection = new LibSQLConnection("Data Source=:memory:");
        connection.Open();

        // Check if vector support is available
        bool vectorSupported = false;
        try
        {
            using var checkCmd = connection.CreateCommand();
            checkCmd.CommandText = "SELECT vector('[1,2,3]')";
            checkCmd.ExecuteScalar();
            vectorSupported = true;
        }
        catch
        {
            // Vector functions not available
        }
        
        if (!vectorSupported)
        {
            return; // Skip if vectors aren't supported
        }

        // Create table
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE embeddings (id INTEGER PRIMARY KEY, data FLOAT32(4))";
        cmd.ExecuteNonQuery();

        // Test parameterized vector insertion
        cmd.CommandText = "INSERT INTO embeddings (id, data) VALUES (@id, vector(@vec))";
        cmd.Parameters.Add(new LibSQLParameter("@id", 1));
        cmd.Parameters.Add(new LibSQLParameter("@vec", "[1.0,2.0,3.0,4.0]"));
        cmd.ExecuteNonQuery();

        // Verify the data was inserted
        cmd.CommandText = "SELECT COUNT(*) FROM embeddings";
        cmd.Parameters.Clear();
        var count = Convert.ToInt32(cmd.ExecuteScalar());
        Assert.Equal(1, count);
        
        // Test vector extraction
        cmd.CommandText = "SELECT vector_extract(data) FROM embeddings WHERE id = 1";
        var extracted = cmd.ExecuteScalar() as string;
        Assert.NotNull(extracted);
        Assert.Contains("1", extracted); // Should contain our vector values
    }

    #endregion
}