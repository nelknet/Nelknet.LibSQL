# Performance Tuning Guide

This guide provides best practices and techniques for optimizing performance when using Nelknet.LibSQL.

## Statement Caching

Statement caching can significantly improve performance by reusing prepared statements across command executions.

### Enabling Statement Caching

```csharp
// Enable at connection level
var connection = new LibSQLConnection("Data Source=app.db");
connection.EnableStatementCaching = true;
connection.MaxCachedStatements = 200; // Default is 100
connection.Open();

// Or via connection string
var connection = new LibSQLConnection(
    "Data Source=app.db;EnableStatementCaching=true;MaxCachedStatements=200");
```

### When to Use Statement Caching

**Good candidates for caching:**
- Frequently executed queries with named parameters
- Read operations that are executed repeatedly
- Simple INSERT/UPDATE/DELETE with named parameters

```csharp
// Good for caching - uses named parameters
cmd.CommandText = "SELECT * FROM users WHERE id = @id";
cmd.Parameters.AddWithValue("@id", userId);
```

**Avoid caching for:**
- Bulk operations with positional parameters (`?`)
- Commands where you clear parameters in a loop
- One-time or rarely executed queries
- DDL statements (CREATE, ALTER, DROP)

```csharp
// Disable caching for bulk operations
cmd.EnableStatementCaching = false;
for (int i = 0; i < 1000; i++)
{
    cmd.Parameters.Clear();
    cmd.Parameters.AddWithValue("@id", i);
    cmd.ExecuteNonQuery();
}
```

### Statement Caching Limitations

Due to libSQL's experimental API limitations:
- Parameters cannot be cleared from cached statements
- Positional parameters (`?`) are automatically excluded from caching
- Cached statements retain their parameter bindings

## Bulk Insert Operations

For inserting large amounts of data, use the specialized `LibSQLBulkInsert` class:

```csharp
// Insert 100,000 rows efficiently
var columns = new[] { "id", "name", "email", "created_at" };
using var bulkInsert = new LibSQLBulkInsert(connection, "users", columns)
{
    BatchSize = 1000,      // Commit every 1000 rows
    UseTransaction = true   // Wrap in transaction
};

bulkInsert.BeginBulkInsert();

for (int i = 0; i < 100000; i++)
{
    bulkInsert.WriteRow(
        i,
        $"User {i}",
        $"user{i}@example.com",
        DateTime.Now.ToString("O")
    );
}

bulkInsert.Complete();
```

### Bulk Insert Best Practices

1. **Use appropriate batch sizes**: 1000-5000 rows per batch is typically optimal
2. **Enable transactions**: Wrapping batches in transactions improves performance
3. **Prepare data in advance**: Minimize processing during the insert loop
4. **Use async methods** for I/O-bound operations:

```csharp
await bulkInsert.BeginBulkInsertAsync();
await bulkInsert.WriteRowAsync(...);
await bulkInsert.CompleteAsync();
```

## Transaction Optimization

### Use Explicit Transactions

Group multiple operations in a single transaction:

```csharp
using var transaction = connection.BeginTransaction();
try
{
    for (int i = 0; i < 100; i++)
    {
        using var cmd = connection.CreateCommand();
        cmd.Transaction = transaction;
        cmd.CommandText = "INSERT INTO data (value) VALUES (@value)";
        cmd.Parameters.AddWithValue("@value", i);
        cmd.ExecuteNonQuery();
    }
    transaction.Commit();
}
catch
{
    transaction.Rollback();
    throw;
}
```

### Transaction Modes

Choose the appropriate transaction mode for your workload:

```csharp
// DEFERRED (default) - Lock acquired on first write
var tx1 = connection.BeginTransaction(
    IsolationLevel.Serializable, 
    LibSQLTransactionBehavior.Deferred);

// IMMEDIATE - Write lock acquired immediately
var tx2 = connection.BeginTransaction(
    IsolationLevel.Serializable,
    LibSQLTransactionBehavior.Immediate);

// EXCLUSIVE - Exclusive lock acquired immediately
var tx3 = connection.BeginTransaction(
    IsolationLevel.Serializable,
    LibSQLTransactionBehavior.Exclusive);
```

## Query Optimization

### Use Indexes Effectively

```sql
-- Create indexes for frequently queried columns
CREATE INDEX idx_users_email ON users(email);
CREATE INDEX idx_users_created ON users(created_at);

-- Composite index for multi-column queries
CREATE INDEX idx_users_name_email ON users(name, email);
```

### Analyze Query Plans

Use the built-in query plan analysis:

```csharp
using var cmd = connection.CreateCommand();
cmd.CommandText = "SELECT * FROM users WHERE email = @email";
cmd.Parameters.AddWithValue("@email", "user@example.com");

// Get query plan
var queryPlan = cmd.GetQueryPlan();
foreach (DataRow row in queryPlan.Rows)
{
    Console.WriteLine($"{row["detail"]}");
}
```

### Optimize SELECT Queries

1. **Select only needed columns**:
```sql
-- Good
SELECT id, name FROM users WHERE active = 1

-- Avoid
SELECT * FROM users WHERE active = 1
```

2. **Use LIMIT for pagination**:
```sql
SELECT id, name FROM users 
ORDER BY created_at DESC 
LIMIT 20 OFFSET 40
```

3. **Avoid N+1 queries** - use joins instead:
```sql
-- Good - single query
SELECT u.*, p.* FROM users u
LEFT JOIN profiles p ON u.id = p.user_id

-- Avoid - multiple queries
SELECT * FROM users;
-- Then for each user:
SELECT * FROM profiles WHERE user_id = ?
```

## Memory Management

### Connection Pooling

Reuse connections instead of creating new ones:

```csharp
public class DatabaseService
{
    private readonly LibSQLConnection _connection;
    
    public DatabaseService(string connectionString)
    {
        _connection = new LibSQLConnection(connectionString);
        _connection.Open();
    }
    
    public void Dispose()
    {
        _connection?.Dispose();
    }
}
```

### DataReader Best Practices

Always dispose DataReaders promptly:

```csharp
// Use using statement
using (var reader = cmd.ExecuteReader())
{
    while (reader.Read())
    {
        // Process row
    }
} // Reader disposed here

// Or with CommandBehavior.CloseConnection
using var reader = cmd.ExecuteReader(CommandBehavior.CloseConnection);
// Connection will close when reader is disposed
```

## Monitoring Performance

### Use Built-in Events

Monitor command execution times:

```csharp
connection.CommandExecuting += (sender, e) =>
{
    Console.WriteLine($"Executing: {e.CommandText}");
};

connection.CommandExecuted += (sender, e) =>
{
    Console.WriteLine($"Executed in {e.Duration.TotalMilliseconds}ms");
};
```

### External Tools

For detailed performance analysis, use:

1. **dotnet-counters** for runtime metrics:
```bash
dotnet counters monitor -n YourApp
```

2. **PerfView** for detailed profiling:
```bash
PerfView collect -n YourApp
```

3. **BenchmarkDotNet** for micro-benchmarks:
```csharp
[Benchmark]
public void BulkInsertTest()
{
    // Your benchmark code
}
```

## Platform-Specific Optimizations

### Linux
- Use native Linux file systems (ext4, XFS) for better performance
- Consider using tmpfs for temporary databases

### Windows
- Disable antivirus scanning for database files
- Use SSD storage for database files

### macOS
- Ensure sufficient file descriptors: `ulimit -n 2048`
- Use APFS for better performance

## Common Performance Pitfalls

1. **Not using prepared statements** for repeated queries
2. **Opening/closing connections frequently** instead of reusing
3. **Not using transactions** for batch operations
4. **Using synchronous methods** for I/O-bound operations
5. **Not disposing resources** properly (connections, readers, commands)
6. **Inefficient SQL queries** (missing indexes, SELECT *)
7. **Parameter clearing in loops** with statement caching enabled

## Embedded Replica Performance

When using embedded replicas, consider these optimization strategies:

### Sync Frequency
- Sync only when necessary, not after every operation
- Use manual sync control instead of automatic sync for better performance
- Consider batching operations before syncing

```csharp
// Good - batch operations then sync
using var connection = new LibSQLConnection(embeddedReplicaConnectionString);
await connection.OpenAsync();

// Disable automatic sync
connection.AutoSync = false;

// Perform many operations
for (int i = 0; i < 1000; i++)
{
    // ... perform operations
}

// Single sync at the end
await connection.SyncAsync();
```

### Local-First Operations
- Read operations are always local and fast
- Write operations can be batched before syncing
- Use the embedded replica for read-heavy workloads

## Summary

Key performance recommendations:
- Enable statement caching for frequently executed queries
- Use bulk insert for large data operations
- Wrap multiple operations in transactions
- Create appropriate indexes
- For embedded replicas, batch operations and sync strategically
- Monitor and profile your application
- Follow platform-specific best practices