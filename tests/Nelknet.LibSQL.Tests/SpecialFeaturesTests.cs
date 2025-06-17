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

    [Fact(Skip = "Encryption support requires further investigation of libSQL experimental API behavior")]
    public void Encryption_CanCreateAndOpenEncryptedDatabase()
    {
        var dbPath = CreateTempFile();
        var encryptionKey = "my-secret-encryption-key";

        // Create encrypted database
        using (var connection = new LibSQLConnection($"Data Source={dbPath};Encryption Key={encryptionKey}"))
        {
            connection.Open();
            
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "CREATE TABLE test (id INTEGER PRIMARY KEY, data TEXT)";
            cmd.ExecuteNonQuery();
            
            cmd.CommandText = "INSERT INTO test (data) VALUES ('encrypted data')";
            cmd.ExecuteNonQuery();
        }

        // Verify we can open with the correct key
        using (var connection = new LibSQLConnection($"Data Source={dbPath};Encryption Key={encryptionKey}"))
        {
            connection.Open();
            
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT data FROM test";
            var result = cmd.ExecuteScalar() as string;
            
            Assert.Equal("encrypted data", result);
        }

        // Verify we cannot open without the key
        using (var connection = new LibSQLConnection($"Data Source={dbPath}"))
        {
            // This should fail as the database is encrypted
            var ex = Assert.Throws<LibSQLConnectionException>(() => connection.Open());
            Assert.NotNull(ex);
        }
    }

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

    [Fact(Skip = "Shared cache in-memory databases may not be supported in libSQL experimental API")]
    public void InMemory_SharedCacheDatabase()
    {
        var connectionString = "Data Source=:memory:?cache=shared";
        
        // Create table in first connection
        using (var conn1 = new LibSQLConnection(connectionString))
        {
            conn1.Open();
            
            using var cmd = conn1.CreateCommand();
            cmd.CommandText = "CREATE TABLE shared_test (id INTEGER PRIMARY KEY, data TEXT)";
            cmd.ExecuteNonQuery();
            
            cmd.CommandText = "INSERT INTO shared_test (data) VALUES ('shared data')";
            cmd.ExecuteNonQuery();
        }

        // Access from second connection
        using (var conn2 = new LibSQLConnection(connectionString))
        {
            conn2.Open();
            
            using var cmd = conn2.CreateCommand();
            cmd.CommandText = "SELECT data FROM shared_test";
            var result = cmd.ExecuteScalar() as string;
            
            Assert.Equal("shared data", result);
        }
    }

    [Fact]
    public void InMemory_HelperMethods()
    {
        var inMemoryConnectionString = LibSQLConnectionStringBuilder.CreateInMemoryConnectionString();
        Assert.Equal("Data Source=:memory:", inMemoryConnectionString);

        var sharedMemoryConnectionString = LibSQLConnectionStringBuilder.CreateSharedMemoryConnectionString();
        Assert.Equal("Data Source=:memory:?cache=shared", sharedMemoryConnectionString);
    }

    #endregion

    // Migration tests removed - migration functionality has been removed from the library
    // Users should use dedicated migration libraries like FluentMigrator, DbUp, or Evolve

    #region Vector Support Tests

    [Fact(Skip = "Vector features require SQL-level support, not available through experimental C API")]
    public void Vector_NotSupportedThroughExperimentalApi()
    {
        // Vector features like FLOAT32(3) and libsql_vector_idx() are SQL-level features
        // that work through the SQLite API. Since we're using the experimental libSQL API
        // which doesn't expose sqlite3* handles, we cannot add specific support for these.
        // 
        // However, users can still use vector features through regular SQL commands if
        // the libSQL library was compiled with vector support enabled.
        
        using var connection = new LibSQLConnection("Data Source=:memory:");
        connection.Open();

        // This would work if libSQL was compiled with vector support,
        // but we can't specifically enable or detect it through the experimental API
        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE vectors (
                id INTEGER PRIMARY KEY,
                embedding FLOAT32(3)
            )";
        
        // May succeed or fail depending on libSQL compilation options
        try
        {
            cmd.ExecuteNonQuery();
            // If it succeeds, vector support is available in the compiled library
        }
        catch
        {
            // If it fails, vector support is not available
        }
    }

    #endregion
}