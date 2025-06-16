# Connection String Documentation

This document describes all available connection string options for Nelknet.LibSQL.

## Connection String Format

Connection strings in Nelknet.LibSQL follow the standard key-value pair format:

```
key1=value1;key2=value2;key3=value3
```

## Connection Modes

Nelknet.LibSQL supports three connection modes:

### Local Mode (Default)
Connect to a local database file or in-memory database.

```csharp
// File-based database
var connection = new LibSQLConnection("Data Source=mydatabase.db");

// In-memory database
var connection = new LibSQLConnection("Data Source=:memory:");

// Shared in-memory database
var connection = new LibSQLConnection("Data Source=:memory:?cache=shared");
```

### Remote Mode
Connect to a remote libSQL server (such as Turso).

```csharp
var connection = new LibSQLConnection(
    "Data Source=mydb.turso.io;Auth Token=your-auth-token;Mode=Remote");
```

### Embedded Replica Mode
*(Not yet implemented)* - Connection string parsing is supported but the functionality is not yet implemented. Will support local replica with remote sync.

**Current Status**: 
- ✅ Connection string parameters (`SyncUrl`, `SyncAuthToken`) are parsed correctly
- ✅ Native struct definitions are in place
- ⚠️ Some native function bindings are declared but not all
- ❌ `LibSQLConnection.Open()` throws `NotSupportedException` for embedded replica mode
- ❌ Sync operations are not implemented

## Connection String Properties

### Core Properties

| Property | Description | Default | Example |
|----------|-------------|---------|---------|
| `Data Source` | Database location (file path, URL, or :memory:) | Required | `mydatabase.db`, `mydb.turso.io`, `:memory:` |
| `Mode` | Connection mode: Local, Remote, or EmbeddedReplica | `Local` | `Mode=Remote` |
| `Auth Token` | Authentication token for remote connections | None | `Auth Token=eyJ0eXAi...` |

### Aliases

The following aliases are supported for common properties:

- `Data Source`: `DataSource`, `Filename`, `Database`
- `Auth Token`: `AuthToken`, `Token`

### Advanced Options

| Property | Description | Default | Example |
|----------|-------------|---------|---------|
| `EnableStatementCaching` | Enable prepared statement caching | `false` | `EnableStatementCaching=true` |
| `MaxCachedStatements` | Maximum number of cached statements | `100` | `MaxCachedStatements=200` |

## Examples

### Local Database Examples

```csharp
// Simple local database
"Data Source=app.db"

// Database in specific directory
"Data Source=/var/data/app.db"

// In-memory database
"Data Source=:memory:"

// Shared in-memory database (multiple connections can access)
"Data Source=:memory:?cache=shared"

// With statement caching enabled
"Data Source=app.db;EnableStatementCaching=true;MaxCachedStatements=50"
```

### Remote Database Examples

```csharp
// Basic remote connection
"Data Source=mydb.turso.io;Auth Token=your-token;Mode=Remote"

// Remote with custom settings
"Data Source=mydb.turso.io;Auth Token=your-token;Mode=Remote;EnableStatementCaching=true"

// Using aliases
"Database=mydb.turso.io;Token=your-token;Mode=Remote"
```

## Connection String Builder

For programmatic connection string construction, use `LibSQLConnectionStringBuilder`:

```csharp
var builder = new LibSQLConnectionStringBuilder
{
    DataSource = "mydb.turso.io",
    Mode = LibSQLConnectionMode.Remote,
    AuthToken = "your-auth-token"
};

// Add advanced options
builder["EnableStatementCaching"] = true;
builder["MaxCachedStatements"] = 200;

var connection = new LibSQLConnection(builder.ConnectionString);
```

## Security Considerations

1. **Auth Tokens**: Never hardcode authentication tokens in your source code. Use environment variables or secure configuration:

```csharp
var token = Environment.GetEnvironmentVariable("LIBSQL_AUTH_TOKEN");
var connectionString = $"Data Source=mydb.turso.io;Auth Token={token};Mode=Remote";
```

2. **Connection String Logging**: Be careful not to log connection strings that contain auth tokens:

```csharp
// Use the builder to get a safe version for logging
var builder = new LibSQLConnectionStringBuilder(connectionString);
var safeString = builder.ToStringSafe(); // Masks sensitive values
```

## Platform-Specific Paths

When using file paths in connection strings:

- **Windows**: Use either forward slashes or escaped backslashes
  - `Data Source=C:/data/app.db`
  - `Data Source=C:\\data\\app.db`

- **Linux/macOS**: Use forward slashes
  - `Data Source=/home/user/app.db`

- **Relative paths**: Resolved from the current working directory
  - `Data Source=./data/app.db`

## Troubleshooting

### Common Issues

1. **"Data source is required"**: Ensure you've specified a `Data Source` property
2. **"Auth token is required for remote connections"**: Add `Auth Token` when `Mode=Remote`
3. **"Failed to open database"**: Check file permissions and path existence
4. **"Statement caching error"**: Disable caching for bulk operations with parameter clearing

### Validation

Use the connection string builder to validate your connection string:

```csharp
try
{
    var builder = new LibSQLConnectionStringBuilder(connectionString);
    // Connection string is valid
}
catch (ArgumentException ex)
{
    Console.WriteLine($"Invalid connection string: {ex.Message}");
}
```