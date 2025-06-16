using System;
using System.Threading;
using System.Threading.Tasks;
using Nelknet.LibSQL.Data;

namespace EmbeddedReplicaExample;

/// <summary>
/// Example demonstrating libSQL embedded replica functionality.
/// This shows how to create a local database that syncs with a remote primary.
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        // Configuration - replace these with your actual values
        // You can get these from https://turso.tech after creating a database
        var primaryUrl = Environment.GetEnvironmentVariable("LIBSQL_PRIMARY_URL") ?? "libsql://your-database.turso.io";
        var authToken = Environment.GetEnvironmentVariable("LIBSQL_AUTH_TOKEN") ?? "your-auth-token";
        var localDbPath = "local_replica.db";
        
        if (primaryUrl.Contains("your-database") || authToken.Contains("your-auth-token"))
        {
            Console.WriteLine("Please set LIBSQL_PRIMARY_URL and LIBSQL_AUTH_TOKEN environment variables");
            Console.WriteLine("You can get these from https://turso.tech after creating a database");
            return;
        }
        
        Console.WriteLine("=== LibSQL Embedded Replica Example ===\n");
        
        try
        {
            // Example 1: Basic embedded replica with manual sync
            await BasicEmbeddedReplicaExample(localDbPath, primaryUrl, authToken);
            
            // Example 2: Automatic sync with interval
            await AutomaticSyncExample(localDbPath, primaryUrl, authToken);
            
            // Example 3: Offline mode handling
            await OfflineModeExample(localDbPath, primaryUrl, authToken);
            
            // Example 4: Sync events and monitoring
            await SyncEventsExample(localDbPath, primaryUrl, authToken);
            
            // Example 5: Read-your-writes consistency
            await ReadYourWritesExample(localDbPath, primaryUrl, authToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
    
    static async Task BasicEmbeddedReplicaExample(string localPath, string primaryUrl, string authToken)
    {
        Console.WriteLine("1. Basic Embedded Replica Example");
        Console.WriteLine("---------------------------------");
        
        var connectionString = $"Data Source={localPath};SyncUrl={primaryUrl};AuthToken={authToken}";
        
        using var connection = new LibSQLConnection(connectionString);
        connection.Open();
        
        Console.WriteLine($"Connected to embedded replica: {localPath}");
        Console.WriteLine($"Primary database: {primaryUrl}");
        
        // Perform initial sync
        Console.WriteLine("\nPerforming initial sync...");
        var syncResult = await connection.SyncAsync();
        Console.WriteLine($"Synced {syncResult.FramesSynced} frames in {syncResult.Duration.TotalMilliseconds:F2}ms");
        Console.WriteLine($"Current frame: {syncResult.FrameNo}");
        
        // Create a local table
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS local_events (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    event_type TEXT NOT NULL,
                    message TEXT,
                    created_at DATETIME DEFAULT CURRENT_TIMESTAMP
                )";
            cmd.ExecuteNonQuery();
        }
        
        // Insert some local data
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "INSERT INTO local_events (event_type, message) VALUES (@type, @msg)";
            cmd.Parameters.AddWithValue("@type", "info");
            cmd.Parameters.AddWithValue("@msg", "Embedded replica initialized");
            cmd.ExecuteNonQuery();
        }
        
        Console.WriteLine("\nLocal changes made. These will be pushed on next sync.");
        Console.WriteLine();
    }
    
    static async Task AutomaticSyncExample(string localPath, string primaryUrl, string authToken)
    {
        Console.WriteLine("2. Automatic Sync Example");
        Console.WriteLine("-------------------------");
        
        // Configure automatic sync every 30 seconds
        var connectionString = $"Data Source={localPath};SyncUrl={primaryUrl};AuthToken={authToken};SyncInterval=30000";
        
        using var connection = new LibSQLConnection(connectionString);
        
        // Subscribe to sync events
        connection.SyncStarted += (s, e) => Console.WriteLine("[Auto-sync] Started...");
        connection.SyncCompleted += (s, e) => 
            Console.WriteLine($"[Auto-sync] Completed: {e.Result.FramesSynced} frames synced");
        
        connection.Open();
        
        Console.WriteLine("Automatic sync enabled (every 30 seconds)");
        Console.WriteLine("Connection will sync in the background...\n");
        
        // Simulate some work
        await Task.Delay(2000);
    }
    
    static async Task OfflineModeExample(string localPath, string primaryUrl, string authToken)
    {
        Console.WriteLine("3. Offline Mode Example");
        Console.WriteLine("-----------------------");
        
        var connectionString = $"Data Source={localPath};SyncUrl={primaryUrl};AuthToken={authToken}";
        
        using var connection = new LibSQLConnection(connectionString);
        connection.Open();
        
        // Work online first
        Console.WriteLine("Working online - syncing with primary...");
        var result = await connection.SyncAsync();
        Console.WriteLine($"Synced: {result.FramesSynced} frames");
        
        // Switch to offline mode
        Console.WriteLine("\nSwitching to offline mode...");
        connection.OfflineMode = true;
        
        // Make changes while offline
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "INSERT INTO local_events (event_type, message) VALUES ('offline', 'Working offline')";
            cmd.ExecuteNonQuery();
        }
        
        // Try to sync while offline (should return empty result)
        result = connection.Sync();
        Console.WriteLine($"Sync attempt while offline: {result.FramesSynced} frames (expected: 0)");
        
        // Go back online
        Console.WriteLine("\nGoing back online...");
        connection.OfflineMode = false;
        
        // Now sync should work
        result = await connection.SyncAsync();
        Console.WriteLine($"Synced after going online: {result.FramesSynced} frames\n");
    }
    
    static async Task SyncEventsExample(string localPath, string primaryUrl, string authToken)
    {
        Console.WriteLine("4. Sync Events Example");
        Console.WriteLine("----------------------");
        
        var connectionString = $"Data Source={localPath};SyncUrl={primaryUrl};AuthToken={authToken}";
        
        using var connection = new LibSQLConnection(connectionString);
        
        // Subscribe to all sync events
        connection.SyncStarted += (s, e) => 
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Sync started");
        };
        
        connection.SyncCompleted += (s, e) => 
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Sync completed:");
            Console.WriteLine($"  - Frames synced: {e.Result.FramesSynced}");
            Console.WriteLine($"  - Current frame: {e.Result.FrameNo}");
            Console.WriteLine($"  - Duration: {e.Result.Duration.TotalMilliseconds:F2}ms");
        };
        
        connection.SyncFailed += (s, e) => 
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Sync failed: {e.Exception.Message}");
        };
        
        connection.Open();
        
        // Perform a sync
        await connection.SyncAsync();
        Console.WriteLine();
    }
    
    static async Task ReadYourWritesExample(string localPath, string primaryUrl, string authToken)
    {
        Console.WriteLine("5. Read-Your-Writes Consistency Example");
        Console.WriteLine("---------------------------------------");
        
        // Enable read-your-writes consistency
        var connectionString = $"Data Source={localPath};SyncUrl={primaryUrl};AuthToken={authToken};ReadYourWrites=true";
        
        using var connection = new LibSQLConnection(connectionString);
        connection.Open();
        
        // Initial sync
        await connection.SyncAsync();
        
        // Create a test table
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS consistency_test (
                    id INTEGER PRIMARY KEY,
                    value TEXT,
                    timestamp DATETIME DEFAULT CURRENT_TIMESTAMP
                )";
            cmd.ExecuteNonQuery();
        }
        
        // Insert data locally
        var testValue = $"test_{Guid.NewGuid():N}";
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "INSERT INTO consistency_test (value) VALUES (@value)";
            cmd.Parameters.AddWithValue("@value", testValue);
            cmd.ExecuteNonQuery();
        }
        
        Console.WriteLine($"Inserted local value: {testValue}");
        
        // With read-your-writes enabled, we can immediately read our own writes
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "SELECT COUNT(*) FROM consistency_test WHERE value = @value";
            cmd.Parameters.AddWithValue("@value", testValue);
            var count = Convert.ToInt32(cmd.ExecuteScalar());
            
            Console.WriteLine($"Can read local write immediately: {count == 1}");
            Console.WriteLine("(This ensures consistency for local operations)\n");
        }
    }
}