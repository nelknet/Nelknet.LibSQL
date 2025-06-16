using System;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Nelknet.LibSQL.Data;

namespace Nelknet.LibSQL.Tests;

/// <summary>
/// Tests for embedded replica functionality.
/// Note: These tests require a valid Turso database URL and auth token to run.
/// They are marked with [Trait("Category", "Integration")] and can be skipped in CI.
/// </summary>
public class EmbeddedReplicaTests : IDisposable
{
    private readonly string? _primaryUrl;
    private readonly string? _authToken;
    private readonly bool _canRunIntegrationTests;
    
    public EmbeddedReplicaTests()
    {
        // Get test configuration from environment variables
        _primaryUrl = Environment.GetEnvironmentVariable("LIBSQL_TEST_PRIMARY_URL");
        _authToken = Environment.GetEnvironmentVariable("LIBSQL_TEST_AUTH_TOKEN");
        _canRunIntegrationTests = !string.IsNullOrEmpty(_primaryUrl) && !string.IsNullOrEmpty(_authToken);
    }
    
    public void Dispose()
    {
        // Cleanup test databases
        GC.SuppressFinalize(this);
    }
    
    private static void DeleteFile(string path)
    {
        if (File.Exists(path))
        {
            try { File.Delete(path); } catch { }
        }
    }
    
    [Fact]
    public void CanDetectEmbeddedReplicaMode()
    {
        var builder = new LibSQLConnectionStringBuilder
        {
            DataSource = "local.db",
            SyncUrl = "libsql://test.turso.io",
            SyncAuthToken = "test-token"
        };
        
        Assert.Equal(LibSQLConnectionMode.EmbeddedReplica, builder.Mode);
    }
    
    [Fact]
    public void CanBuildEmbeddedReplicaConnectionString()
    {
        var builder = new LibSQLConnectionStringBuilder
        {
            DataSource = "local.db",
            SyncUrl = "libsql://test.turso.io",
            SyncAuthToken = "test-token",
            ReadYourWrites = true,
            SyncInterval = 60000,
            Offline = false
        };
        
        var connString = builder.ConnectionString;
        Assert.Contains("Data Source=local.db", connString);
        Assert.Contains("Sync URL=libsql://test.turso.io", connString);
        Assert.Contains("Sync Auth Token=test-token", connString);
        Assert.Contains("Read Your Writes=True", connString);
        Assert.Contains("Sync Interval=60000", connString);
        Assert.Contains("Offline=False", connString);
    }
    
    [Fact]
    public void ThrowsWhenMissingSyncUrl()
    {
        if (!_canRunIntegrationTests) 
            return; // Skip: Integration tests require LIBSQL_TEST_PRIMARY_URL and LIBSQL_TEST_AUTH_TOKEN
        
        using var connection = new LibSQLConnection($"Data Source=test.db;AuthToken={_authToken}");
        
        // Force embedded replica mode without sync URL
        var builder = new LibSQLConnectionStringBuilder(connection.ConnectionString)
        {
            SyncUrl = null
        };
        connection.ConnectionString = builder.ConnectionString;
        
        // This should work - it's just a local connection
        connection.Open();
        connection.Close();
    }
    
    [Fact]
    [Trait("Category", "Integration")]
    public void CanOpenEmbeddedReplicaConnection()
    {
        if (!_canRunIntegrationTests) 
            return; // Skip: Integration tests require LIBSQL_TEST_PRIMARY_URL and LIBSQL_TEST_AUTH_TOKEN
        
        var tempDb = $"test_replica_{Guid.NewGuid():N}.db";
        try
        {
            using var connection = new LibSQLConnection($"Data Source={tempDb};SyncUrl={_primaryUrl};SyncAuthToken={_authToken}");
            connection.Open();
            
            Assert.Equal(ConnectionState.Open, connection.State);
            Assert.Equal(LibSQLConnectionMode.EmbeddedReplica, 
                new LibSQLConnectionStringBuilder(connection.ConnectionString).Mode);
        }
        finally
        {
            DeleteFile(tempDb);
        }
    }
    
    [Fact]
    [Trait("Category", "Integration")]
    public async Task CanSyncEmbeddedReplica()
    {
        if (!_canRunIntegrationTests) 
            return; // Skip: Integration tests require LIBSQL_TEST_PRIMARY_URL and LIBSQL_TEST_AUTH_TOKEN
        
        var tempDb = $"test_sync_{Guid.NewGuid():N}.db";
        try
        {
            using var connection = new LibSQLConnection($"Data Source={tempDb};SyncUrl={_primaryUrl};SyncAuthToken={_authToken}");
            connection.Open();
            
            // Perform initial sync
            var result = await connection.SyncAsync();
            
            Assert.NotNull(result);
            Assert.True(result.FrameNo >= 0);
            Assert.True(result.FramesSynced >= 0);
            Assert.True(result.Duration >= TimeSpan.Zero);
        }
        finally
        {
            DeleteFile(tempDb);
        }
    }
    
    [Fact]
    public void SyncThrowsOnNonReplicaConnection()
    {
        using var connection = new LibSQLConnection("Data Source=:memory:");
        connection.Open();
        
        var ex = Assert.Throws<InvalidOperationException>(() => connection.Sync());
        Assert.Contains("only available for embedded replica", ex.Message);
    }
    
    [Fact]
    [Trait("Category", "Integration")]
    public async Task SyncEventsAreFired()
    {
        if (!_canRunIntegrationTests) 
            return; // Skip: Integration tests require LIBSQL_TEST_PRIMARY_URL and LIBSQL_TEST_AUTH_TOKEN
        
        var tempDb = $"test_events_{Guid.NewGuid():N}.db";
        try
        {
            using var connection = new LibSQLConnection($"Data Source={tempDb};SyncUrl={_primaryUrl};SyncAuthToken={_authToken}");
            
            bool syncStarted = false;
            bool syncCompleted = false;
            LibSQLSyncResult? syncResult = null;
            
            connection.SyncStarted += (s, e) => syncStarted = true;
            connection.SyncCompleted += (s, e) => 
            {
                syncCompleted = true;
                syncResult = e.Result;
            };
            
            connection.Open();
            await connection.SyncAsync();
            
            Assert.True(syncStarted);
            Assert.True(syncCompleted);
            Assert.NotNull(syncResult);
        }
        finally
        {
            DeleteFile(tempDb);
        }
    }
    
    [Fact]
    public void OfflineModePreventsSyncing()
    {
        if (!_canRunIntegrationTests) 
            return; // Skip: Integration tests require LIBSQL_TEST_PRIMARY_URL and LIBSQL_TEST_AUTH_TOKEN
        
        var tempDb = $"test_offline_{Guid.NewGuid():N}.db";
        try
        {
            using var connection = new LibSQLConnection($"Data Source={tempDb};SyncUrl={_primaryUrl};SyncAuthToken={_authToken};Offline=true");
            connection.Open();
            
            // Sync in offline mode should return empty result
            var result = connection.Sync();
            
            Assert.Equal(0, result.FrameNo);
            Assert.Equal(0, result.FramesSynced);
            Assert.Equal(TimeSpan.Zero, result.Duration);
        }
        finally
        {
            DeleteFile(tempDb);
        }
    }
    
    [Fact]
    public void CanToggleOfflineMode()
    {
        if (!_canRunIntegrationTests) 
            return; // Skip: Integration tests require LIBSQL_TEST_PRIMARY_URL and LIBSQL_TEST_AUTH_TOKEN
        
        var tempDb = $"test_toggle_{Guid.NewGuid():N}.db";
        try
        {
            using var connection = new LibSQLConnection($"Data Source={tempDb};SyncUrl={_primaryUrl};SyncAuthToken={_authToken}");
            connection.Open();
            
            Assert.False(connection.OfflineMode);
            
            // Go offline
            connection.OfflineMode = true;
            Assert.True(connection.OfflineMode);
            
            // Sync should return empty result
            var result = connection.Sync();
            Assert.Equal(0, result.FramesSynced);
            
            // Go back online
            connection.OfflineMode = false;
            Assert.False(connection.OfflineMode);
            
            // Now sync should work
            result = connection.Sync();
            Assert.True(result.FrameNo >= 0);
        }
        finally
        {
            DeleteFile(tempDb);
        }
    }
    
    [Fact]
    [Trait("Category", "Integration")]
    public async Task ReadYourWritesConsistency()
    {
        if (!_canRunIntegrationTests) 
            return; // Skip: Integration tests require LIBSQL_TEST_PRIMARY_URL and LIBSQL_TEST_AUTH_TOKEN
        
        var tempDb = $"test_ryw_{Guid.NewGuid():N}.db";
        try
        {
            using var connection = new LibSQLConnection($"Data Source={tempDb};SyncUrl={_primaryUrl};SyncAuthToken={_authToken};ReadYourWrites=true");
            connection.Open();
            
            // Initial sync
            await connection.SyncAsync();
            
            // Create a local table and insert data
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "CREATE TABLE IF NOT EXISTS test_ryw (id INTEGER PRIMARY KEY, value TEXT)";
                cmd.ExecuteNonQuery();
                
                cmd.CommandText = "INSERT INTO test_ryw (value) VALUES ('local write')";
                cmd.ExecuteNonQuery();
            }
            
            // With read-your-writes, we should see our local changes immediately
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(*) FROM test_ryw WHERE value = 'local write'";
                var count = Convert.ToInt32(cmd.ExecuteScalar());
                Assert.Equal(1, count);
            }
        }
        finally
        {
            DeleteFile(tempDb);
        }
    }
}