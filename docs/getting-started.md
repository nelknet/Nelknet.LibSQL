# Getting Started with Nelknet.LibSQL

Nelknet.LibSQL is a native .NET client library for libSQL databases that follows the ADO.NET pattern. This guide will help you get started with basic operations.

## Installation

Install the Nelknet.LibSQL package via NuGet:

```bash
dotnet add package Nelknet.LibSQL
```

## Basic Usage

### Creating a Connection

```csharp
using Nelknet.LibSQL.Data;

// Local database file
using var connection = new LibSQLConnection("Data Source=mydatabase.db");
connection.Open();

// In-memory database
using var memConnection = new LibSQLConnection("Data Source=:memory:");
memConnection.Open();

// Embedded replica (local database with remote sync)
using var replicaConnection = new LibSQLConnection(
    "Data Source=replica.db;SyncUrl=libsql://mydb-user.turso.io;AuthToken=your-token");
replicaConnection.Open();

// Remote database (Turso or libSQL server) - Coming in Phase 21
// using var remoteConnection = new LibSQLConnection(
//     "Data Source=mydb.turso.io;Auth Token=your-auth-token");
// remoteConnection.Open();
```

### Executing Commands

```csharp
// Create a table
using var createCmd = connection.CreateCommand();
createCmd.CommandText = @"
    CREATE TABLE users (
        id INTEGER PRIMARY KEY,
        name TEXT NOT NULL,
        email TEXT UNIQUE,
        created_at TEXT DEFAULT CURRENT_TIMESTAMP
    )";
createCmd.ExecuteNonQuery();

// Insert data
using var insertCmd = connection.CreateCommand();
insertCmd.CommandText = "INSERT INTO users (name, email) VALUES (@name, @email)";
insertCmd.Parameters.AddWithValue("@name", "John Doe");
insertCmd.Parameters.AddWithValue("@email", "john@example.com");
var rowsAffected = insertCmd.ExecuteNonQuery();
```

### Querying Data

```csharp
using var queryCmd = connection.CreateCommand();
queryCmd.CommandText = "SELECT id, name, email FROM users WHERE name LIKE @pattern";
queryCmd.Parameters.AddWithValue("@pattern", "%John%");

using var reader = queryCmd.ExecuteReader();
while (reader.Read())
{
    var id = reader.GetInt64(0);
    var name = reader.GetString(1);
    var email = reader.IsDBNull(2) ? null : reader.GetString(2);
    
    Console.WriteLine($"User {id}: {name} ({email})");
}
```

### Using Transactions

```csharp
using var transaction = connection.BeginTransaction();
try
{
    using var cmd1 = connection.CreateCommand();
    cmd1.Transaction = transaction;
    cmd1.CommandText = "INSERT INTO users (name, email) VALUES (@name, @email)";
    cmd1.Parameters.AddWithValue("@name", "Jane Doe");
    cmd1.Parameters.AddWithValue("@email", "jane@example.com");
    cmd1.ExecuteNonQuery();

    using var cmd2 = connection.CreateCommand();
    cmd2.Transaction = transaction;
    cmd2.CommandText = "UPDATE users SET email = @email WHERE name = @name";
    cmd2.Parameters.AddWithValue("@email", "newemail@example.com");
    cmd2.Parameters.AddWithValue("@name", "John Doe");
    cmd2.ExecuteNonQuery();

    transaction.Commit();
}
catch
{
    transaction.Rollback();
    throw;
}
```

### Bulk Insert Operations

For inserting large amounts of data efficiently:

```csharp
var columns = new[] { "name", "email", "created_at" };
using var bulkInsert = new LibSQLBulkInsert(connection, "users", columns);

bulkInsert.BeginBulkInsert();

for (int i = 0; i < 10000; i++)
{
    bulkInsert.WriteRow(
        $"User {i}",
        $"user{i}@example.com",
        DateTime.Now.ToString("O")
    );
}

bulkInsert.Complete();
```

### Working with Embedded Replicas

Embedded replicas allow you to have a local database that can sync with a remote libSQL server:

```csharp
// Create embedded replica connection
using var connection = new LibSQLConnection(
    "Data Source=local_replica.db;SyncUrl=libsql://mydb-user.turso.io;AuthToken=your-token");
await connection.OpenAsync();

// Sync with remote database (pull latest changes)
await connection.SyncAsync();

// Work with local data (fast!)
using var cmd = connection.CreateCommand();
cmd.CommandText = "SELECT COUNT(*) FROM users";
var count = await cmd.ExecuteScalarAsync();
Console.WriteLine($"Local users count: {count}");

// Make local changes
cmd.CommandText = "INSERT INTO users (name, email) VALUES (@name, @email)";
cmd.Parameters.AddWithValue("@name", "Local User");
cmd.Parameters.AddWithValue("@email", "local@example.com");
await cmd.ExecuteNonQueryAsync();

// Sync changes back to remote
await connection.SyncAsync();
```

### Using DataAdapter

```csharp
using var adapter = new LibSQLDataAdapter("SELECT * FROM users", connection);
var dataSet = new DataSet();
adapter.Fill(dataSet, "users");

// Modify data
var table = dataSet.Tables["users"];
var newRow = table.NewRow();
newRow["name"] = "New User";
newRow["email"] = "newuser@example.com";
table.Rows.Add(newRow);

// Update database
using var cmdBuilder = new LibSQLCommandBuilder(adapter);
adapter.Update(dataSet, "users");
```

## Connection String Options

See the [Connection String Documentation](connection-strings.md) for detailed information about all available options.

## Performance Tips

1. **Use Prepared Statements**: For repeated queries, use `Prepare()` or enable statement caching
2. **Bulk Operations**: Use `LibSQLBulkInsert` for inserting large amounts of data
3. **Connection Pooling**: Reuse connections when possible, especially for remote databases
4. **Transactions**: Group multiple operations in transactions for better performance

## Error Handling

```csharp
try
{
    // Database operations
}
catch (LibSQLException ex)
{
    Console.WriteLine($"Database error: {ex.Message}");
    Console.WriteLine($"Error code: {ex.ErrorCode}");
    
    // Handle specific error types
    if (ex is LibSQLBusyException busyEx)
    {
        Console.WriteLine($"Database is busy: {busyEx.LockType}");
    }
    else if (ex is LibSQLConstraintException constraintEx)
    {
        Console.WriteLine($"Constraint violation: {constraintEx.ConstraintType}");
    }
}
```

## Next Steps

- Read the [Connection String Documentation](connection-strings.md) for advanced connection options
- Check out the [Performance Tuning Guide](performance-tuning.md) for optimization tips
- See [Platform-Specific Notes](platform-specific.md) for OS-specific considerations
- Browse [Code Examples](examples.md) for more usage patterns