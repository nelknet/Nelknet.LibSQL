using System;
using System.Threading.Tasks;
using Nelknet.LibSQL.Data;

namespace RemoteConnectionExample;

/// <summary>
/// Example demonstrating remote libSQL connections via HTTP.
/// 
/// This example shows how to connect to a remote libSQL server (like Turso)
/// using HTTP-based connections with the Hrana protocol.
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Nelknet.LibSQL Remote Connection Example");
        Console.WriteLine("=========================================");
        Console.WriteLine();

        // Example connection strings for different scenarios
        await DemonstrateConnectionStringFormats();
        Console.WriteLine();

        // NOTE: The examples below require actual server credentials
        // Uncomment and modify with real credentials to test

        // await DemonstrateBasicRemoteConnection();
        // await DemonstrateParameterizedQueries();
        // await DemonstrateTransactions();

        Console.WriteLine("Example completed. Note: Actual remote connections require valid server credentials.");
    }

    /// <summary>
    /// Demonstrates different connection string formats for remote connections.
    /// </summary>
    static async Task DemonstrateConnectionStringFormats()
    {
        Console.WriteLine("1. Connection String Formats");
        Console.WriteLine("----------------------------");

        // Various ways to specify remote connections
        var examples = new[]
        {
            "Data Source=https://your-database.turso.io;Auth Token=your-auth-token",
            "Data Source=libsql://your-database.turso.io;Auth Token=your-auth-token",
            "Data Source=http://localhost:8080;Auth Token=local-dev-token"
        };

        foreach (var connectionString in examples)
        {
            var builder = new LibSQLConnectionStringBuilder(connectionString);
            Console.WriteLine($"Connection String: {connectionString}");
            Console.WriteLine($"  Mode: {builder.Mode}");
            Console.WriteLine($"  Data Source: {builder.DataSource}");
            Console.WriteLine($"  Auth Token: {(string.IsNullOrEmpty(builder.AuthToken) ? "Not set" : "***")}");
            Console.WriteLine();
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Demonstrates basic remote connection usage.
    /// NOTE: Requires valid server credentials to run.
    /// </summary>
    static async Task DemonstrateBasicRemoteConnection()
    {
        Console.WriteLine("2. Basic Remote Connection");
        Console.WriteLine("--------------------------");

        // Replace with your actual Turso database URL and token
        var connectionString = "Data Source=https://your-database.turso.io;Auth Token=your-auth-token";
        
        try
        {
            using var connection = new LibSQLConnection(connectionString);
            
            Console.WriteLine("Opening connection...");
            await connection.OpenAsync();
            
            Console.WriteLine($"Connected! State: {connection.State}");
            
            var builder = new LibSQLConnectionStringBuilder(connection.ConnectionString);
            Console.WriteLine($"Connection Mode: {builder.Mode}");
            
            // Simple query
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT 'Hello from remote libSQL!' as message";
            
            var result = await command.ExecuteScalarAsync();
            Console.WriteLine($"Query result: {result}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Connection failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Demonstrates parameterized queries with remote connections.
    /// NOTE: Requires valid server credentials to run.
    /// </summary>
    static async Task DemonstrateParameterizedQueries()
    {
        Console.WriteLine("3. Parameterized Queries");
        Console.WriteLine("------------------------");

        var connectionString = "Data Source=https://your-database.turso.io;Auth Token=your-auth-token";
        
        try
        {
            using var connection = new LibSQLConnection(connectionString);
            await connection.OpenAsync();
            
            // Create a simple table (if it doesn't exist)
            using var createCmd = connection.CreateCommand();
            createCmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS users (
                    id INTEGER PRIMARY KEY,
                    name TEXT NOT NULL,
                    email TEXT UNIQUE
                )";
            await createCmd.ExecuteNonQueryAsync();
            Console.WriteLine("Table created/verified");
            
            // Insert with parameters
            using var insertCmd = connection.CreateCommand();
            insertCmd.CommandText = "INSERT INTO users (name, email) VALUES (@name, @email)";
            insertCmd.Parameters.Add(new LibSQLParameter("@name", "John Doe"));
            insertCmd.Parameters.Add(new LibSQLParameter("@email", "john@example.com"));
            
            var rowsAffected = await insertCmd.ExecuteNonQueryAsync();
            Console.WriteLine($"Inserted {rowsAffected} row(s)");
            
            // Query with parameters
            using var selectCmd = connection.CreateCommand();
            selectCmd.CommandText = "SELECT name, email FROM users WHERE name LIKE @pattern";
            selectCmd.Parameters.Add(new LibSQLParameter("@pattern", "%John%"));
            
            using var reader = await selectCmd.ExecuteReaderAsync();
            Console.WriteLine("Query results:");
            while (await reader.ReadAsync())
            {
                Console.WriteLine($"  Name: {reader.GetString(0)}, Email: {reader.GetString(1)}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Demonstrates transaction usage with remote connections.
    /// NOTE: Requires valid server credentials to run.
    /// </summary>
    static async Task DemonstrateTransactions()
    {
        Console.WriteLine("4. Transactions");
        Console.WriteLine("---------------");

        var connectionString = "Data Source=https://your-database.turso.io;Auth Token=your-auth-token";
        
        try
        {
            using var connection = new LibSQLConnection(connectionString);
            await connection.OpenAsync();
            
            // Begin transaction
            using var transaction = await connection.BeginTransactionAsync();
            
            try
            {
                // Multiple operations in a transaction
                using var cmd1 = connection.CreateCommand();
                cmd1.Transaction = transaction;
                cmd1.CommandText = "INSERT INTO users (name, email) VALUES ('Alice', 'alice@example.com')";
                await cmd1.ExecuteNonQueryAsync();
                
                using var cmd2 = connection.CreateCommand();
                cmd2.Transaction = transaction;
                cmd2.CommandText = "INSERT INTO users (name, email) VALUES ('Bob', 'bob@example.com')";
                await cmd2.ExecuteNonQueryAsync();
                
                // Commit transaction
                await transaction.CommitAsync();
                Console.WriteLine("Transaction committed successfully");
            }
            catch
            {
                // Rollback on error
                await transaction.RollbackAsync();
                Console.WriteLine("Transaction rolled back due to error");
                throw;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Transaction error: {ex.Message}");
        }
    }
}