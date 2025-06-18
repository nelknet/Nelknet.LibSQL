# Nelknet.LibSQL

A native .NET client library for [libSQL](https://github.com/tursodatabase/libsql) databases, providing a complete ADO.NET implementation for seamless integration with existing .NET applications.

[![NuGet](https://img.shields.io/nuget/v/Nelknet.LibSQL.Data.svg)](https://www.nuget.org/packages/Nelknet.LibSQL.Data/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Nelknet.LibSQL.Data.svg)](https://www.nuget.org/packages/Nelknet.LibSQL.Data/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

## Overview

Nelknet.LibSQL is a C# client library that provides native bindings to libSQL, a fork of SQLite that adds support for replication, remote connections, and other distributed features. This library follows the ADO.NET pattern, making it easy to use for developers familiar with other .NET database providers.

### Key Features

- **Native Performance**: Direct P/Invoke bindings to the libSQL C library
- **ADO.NET Compliant**: Implements standard interfaces (`DbConnection`, `DbCommand`, `DbDataReader`, etc.)
- **Modern C#**: Uses `LibraryImport` for better performance and AOT compatibility
- **Cross-Platform**: Supports Windows, Linux, and macOS (x64 and ARM64)
- **libSQL Features**: Access to remote databases, embedded replicas, and sync capabilities
- **Type Safe**: Comprehensive type mapping between libSQL and .NET types

## Installation

Install the Nelknet.LibSQL.Data package via NuGet:

```bash
dotnet add package Nelknet.LibSQL.Data
```

Or via Package Manager Console:

```powershell
Install-Package Nelknet.LibSQL.Data
```

> **Note**: The package is currently in alpha. To install pre-release versions:
> ```bash
> dotnet add package Nelknet.LibSQL.Data --prerelease
> ```

## Quick Start

### Basic Usage

```csharp
using Nelknet.LibSQL.Data;

// Create and open a connection
using var connection = new LibSQLConnection("Data Source=local.db");
connection.Open();

// Create a table
using var createCmd = connection.CreateCommand();
createCmd.CommandText = @"
    CREATE TABLE users (
        id INTEGER PRIMARY KEY,
        name TEXT NOT NULL,
        email TEXT UNIQUE
    )";
createCmd.ExecuteNonQuery();

// Insert data
using var insertCmd = connection.CreateCommand();
insertCmd.CommandText = "INSERT INTO users (name, email) VALUES (@name, @email)";
insertCmd.Parameters.AddWithValue("@name", "Alice");
insertCmd.Parameters.AddWithValue("@email", "alice@example.com");
insertCmd.ExecuteNonQuery();

// Query data
using var queryCmd = connection.CreateCommand();
queryCmd.CommandText = "SELECT * FROM users";
using var reader = queryCmd.ExecuteReader();

while (reader.Read())
{
    var id = reader.GetInt32(0);
    var name = reader.GetString(1);
    var email = reader.GetString(2);
    Console.WriteLine($"User {id}: {name} ({email})");
}
```

### Remote Database Connection

```csharp
// Connect to a remote libSQL database
var connectionString = "Data Source=https://your-database.turso.io;Auth Token=your-auth-token";
using var connection = new LibSQLConnection(connectionString);
connection.Open();

// Use the connection normally...
```

### Embedded Replica with Sync

```csharp
// Create an embedded replica that syncs with a remote primary
var connectionString = @"
    Data Source=local-replica.db;
    SyncUrl=https://your-database.turso.io;
    AuthToken=your-auth-token;
    ReadYourWrites=true";

using var connection = new LibSQLConnection(connectionString);
connection.Open();

// Sync with remote
await connection.SyncAsync();

// Perform local reads with remote consistency
using var cmd = connection.CreateCommand();
cmd.CommandText = "SELECT * FROM products WHERE price > 100";
using var reader = cmd.ExecuteReader();
// ... process results ...
```

### Using Transactions

```csharp
using var connection = new LibSQLConnection("Data Source=local.db");
connection.Open();

using var transaction = connection.BeginTransaction();
try
{
    using var cmd = connection.CreateCommand();
    cmd.Transaction = transaction;
    
    // Multiple operations in a transaction
    cmd.CommandText = "UPDATE accounts SET balance = balance - 100 WHERE id = 1";
    cmd.ExecuteNonQuery();
    
    cmd.CommandText = "UPDATE accounts SET balance = balance + 100 WHERE id = 2";
    cmd.ExecuteNonQuery();
    
    transaction.Commit();
}
catch
{
    transaction.Rollback();
    throw;
}
```

### Async Operations

```csharp
using var connection = new LibSQLConnection("Data Source=local.db");
await connection.OpenAsync();

using var cmd = connection.CreateCommand();
cmd.CommandText = "SELECT COUNT(*) FROM users";
var count = await cmd.ExecuteScalarAsync();

Console.WriteLine($"Total users: {count}");
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

## Connection String Reference

Connection strings follow the standard key-value pair format: `key1=value1;key2=value2`

### Connection Modes

**Local Mode (Default)**
```csharp
// File-based database
var connection = new LibSQLConnection("Data Source=mydatabase.db");

// In-memory database
var connection = new LibSQLConnection("Data Source=:memory:");
```

**Remote Mode**
```csharp
// Connect to remote libSQL server (e.g., Turso)
var connection = new LibSQLConnection(
    "Data Source=https://mydb.turso.io;Auth Token=your-auth-token");
```

**Embedded Replica Mode**
```csharp
// Local database that syncs with remote
var connection = new LibSQLConnection(
    "Data Source=local.db;SyncUrl=https://mydb.turso.io;AuthToken=your-token");
```

### Connection String Properties

| Property | Description | Default | Example |
|----------|-------------|---------|---------|
| `Data Source` | Database location (file path, URL, or :memory:) | Required | `mydatabase.db`, `https://mydb.turso.io` |
| `Auth Token` | Authentication token for remote connections | None | `Auth Token=eyJ0eXAi...` |
| `SyncUrl` | URL of the primary database for embedded replicas | None | `SyncUrl=https://mydb.turso.io` |
| `SyncInterval` | Automatic sync interval in milliseconds | None (manual) | `SyncInterval=60000` |
| `ReadYourWrites` | Enable read-your-writes consistency | `true` | `ReadYourWrites=false` |
| `Offline` | Start embedded replica in offline mode | `false` | `Offline=true` |
| `EnableStatementCaching` | Enable prepared statement caching | `false` | `EnableStatementCaching=true` |
| `MaxCachedStatements` | Maximum number of cached statements | `100` | `MaxCachedStatements=200` |

**Property Aliases**: `Data Source` = `DataSource`, `Filename`, `Database` | `Auth Token` = `AuthToken`, `Token`

### Connection String Builder

For programmatic construction:

```csharp
var builder = new LibSQLConnectionStringBuilder
{
    DataSource = "https://mydb.turso.io",
    AuthToken = "your-auth-token",
    EnableStatementCaching = true
};

var connection = new LibSQLConnection(builder.ConnectionString);
```

## Design Choices

### Why the Experimental libSQL API?

This library uses libSQL's experimental C API (`libsql_*` functions) rather than the SQLite C API (`sqlite3_*` functions). This design choice was made to:

1. **Access libSQL-specific features**: The experimental API provides access to replication, remote connections, and other distributed features not available in SQLite.

2. **Follow official patterns**: The official Go client uses the same experimental API, validating this as the intended approach for native clients.

3. **Maintain consistency**: Using a single API throughout the library avoids potential conflicts between different memory management and state handling approaches.

### Trade-offs and Limitations

This library, like other official libSQL clients (Python, Go, etc.), focuses on core database functionality and libSQL-specific features rather than exposing the full SQLite API. The following SQLite features are intentionally not supported:

- **Custom SQL functions** (`sqlite3_create_function`)
- **SQLite backup API** (`sqlite3_backup_*`)
- **Extended result codes** (`sqlite3_extended_result_codes`)
- **Blob I/O** (`sqlite3_blob_*`)
- **Load extensions** (`sqlite3_load_extension`)
- **Authorization callbacks** (`sqlite3_set_authorizer`)
- **Progress handlers** (`sqlite3_progress_handler`)

If you need these advanced SQLite features, consider using Microsoft.Data.Sqlite or System.Data.SQLite instead.

### ADO.NET Implementation

The library implements the standard ADO.NET interfaces to ensure compatibility with existing .NET code and patterns:
- Familiar API for .NET developers
- Compatible with ORMs like Dapper
- Follows Microsoft.Data.Sqlite patterns where applicable
- Supports both synchronous and asynchronous operations

## Performance Tips

1. **Use Prepared Statements**: For repeated queries, prepare statements or enable statement caching
2. **Bulk Operations**: Use `LibSQLBulkInsert` for inserting large amounts of data
3. **Connection Pooling**: Reuse connections when possible, especially for remote databases
4. **Transactions**: Group multiple operations in transactions for better performance
5. **Statement Caching**: Enable with `EnableStatementCaching=true` in connection string

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

## Code Quality

The project uses the following analyzers to maintain code quality:
- **Microsoft.CodeAnalysis.NetAnalyzers** - Built-in .NET analyzers for security, performance, and reliability
- **Roslynator.Analyzers** - Additional code quality and style checks

These analyzers help catch potential issues at compile time. Configuration is in `.editorconfig`.

## Architecture

```
Nelknet.LibSQL/
├── Nelknet.LibSQL.Bindings/     # P/Invoke bindings to libSQL C API
│   ├── LibSQLNative.cs          # Native method declarations
│   ├── SafeHandles.cs           # Safe handle wrappers
│   └── NativeStructs.cs         # Native structure definitions
└── Nelknet.LibSQL.Data/         # ADO.NET implementation
    ├── LibSQLConnection.cs      # DbConnection implementation
    ├── LibSQLCommand.cs         # DbCommand implementation
    ├── LibSQLDataReader.cs      # DbDataReader implementation
    └── LibSQLParameter.cs       # DbParameter implementation
```

## Requirements

- .NET 8.0 or later
- libSQL native library (automatically included via NuGet)
- Supported platforms:
  - Windows x64/ARM64
  - Linux x64/ARM64
  - macOS x64/ARM64

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- [libSQL](https://github.com/tursodatabase/libsql) - The underlying database engine
- [DuckDB.NET](https://github.com/Giorgi/DuckDB.NET) - Inspiration for native binding patterns
- [Microsoft.Data.Sqlite](https://github.com/dotnet/efcore) - Reference for ADO.NET implementation

## Example Projects

- [BasicExample](examples/BasicExample) - Comprehensive console application demonstrating all major features
- [EmbeddedReplicaExample](examples/EmbeddedReplicaExample) - Demonstrates embedded replica functionality
- [RemoteConnectionExample](examples/RemoteConnectionExample) - Shows remote connection usage

## Links

- [libSQL Documentation](https://docs.turso.tech/libsql)
- [Turso Database](https://turso.tech/) - Managed libSQL hosting
- [NuGet Package](https://www.nuget.org/packages/Nelknet.LibSQL.Data/)
- [Report Issues](https://github.com/nelknet/Nelknet.LibSQL/issues)