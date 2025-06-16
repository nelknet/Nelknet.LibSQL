# API Reference

This document provides a quick reference for the main classes and methods in Nelknet.LibSQL.

## Core Classes

### LibSQLConnection

Represents a connection to a libSQL database.

```csharp
public sealed class LibSQLConnection : DbConnection
```

**Key Members:**
- `ConnectionString` - Gets or sets the connection string
- `Database` - Gets the current database name
- `State` - Gets the current connection state
- `EnableStatementCaching` - Enables/disables statement caching
- `MaxCachedStatements` - Maximum number of cached statements
- `OfflineMode` - Gets/sets offline mode for embedded replicas
- `Open()` - Opens the connection
- `Close()` - Closes the connection
- `BeginTransaction()` - Starts a new transaction
- `CreateCommand()` - Creates a new command
- `Sync()` - Synchronizes embedded replica with primary (sync mode only)
- `SyncAsync()` - Asynchronously synchronizes embedded replica

**Events:**
- `Progress` - Raised during long operations
- `CommandExecuting` - Raised before command execution
- `CommandExecuted` - Raised after command execution
- `SyncStarted` - Raised when sync begins (embedded replica)
- `SyncCompleted` - Raised when sync completes successfully
- `SyncFailed` - Raised when sync fails

### LibSQLCommand

Represents a SQL command to execute against a libSQL database.

```csharp
public sealed class LibSQLCommand : DbCommand
```

**Key Members:**
- `CommandText` - Gets or sets the SQL text
- `CommandTimeout` - Command timeout in seconds
- `Connection` - The connection to use
- `Parameters` - Collection of parameters
- `EnableStatementCaching` - Per-command caching control
- `ExecuteNonQuery()` - Executes command returning affected rows
- `ExecuteScalar()` - Executes command returning single value
- `ExecuteReader()` - Executes command returning data reader
- `Prepare()` - Prepares the command
- `GetQueryPlan()` - Gets the query execution plan

### LibSQLDataReader

Provides a forward-only reader for query results.

```csharp
public sealed class LibSQLDataReader : DbDataReader
```

**Key Members:**
- `HasRows` - Indicates if reader has rows
- `FieldCount` - Number of columns
- `Read()` - Advances to next row
- `GetName(ordinal)` - Gets column name
- `GetOrdinal(name)` - Gets column index
- `GetValue(ordinal)` - Gets column value
- `GetString()`, `GetInt32()`, etc. - Typed accessors
- `IsDBNull(ordinal)` - Checks for null values

### LibSQLParameter

Represents a parameter to a command.

```csharp
public sealed class LibSQLParameter : DbParameter
```

**Key Members:**
- `ParameterName` - Parameter name (with or without @)
- `Value` - Parameter value
- `DbType` - Parameter data type
- `Size` - Parameter size (for strings/blobs)
- `IsNullable` - Whether parameter accepts nulls

### LibSQLTransaction

Represents a database transaction.

```csharp
public sealed class LibSQLTransaction : DbTransaction
```

**Key Members:**
- `IsolationLevel` - Transaction isolation level
- `Commit()` - Commits the transaction
- `Rollback()` - Rolls back the transaction
- `Save(savepointName)` - Creates a savepoint
- `Release(savepointName)` - Releases a savepoint
- `RollbackTo(savepointName)` - Rolls back to savepoint

## Specialized Classes

### LibSQLBulkInsert

Provides high-performance bulk insert operations.

```csharp
public sealed class LibSQLBulkInsert : IDisposable
```

**Constructor:**
```csharp
public LibSQLBulkInsert(
    LibSQLConnection connection,
    string tableName,
    string[] columnNames)
```

**Key Members:**
- `BatchSize` - Rows per batch (default: 1000)
- `UseTransaction` - Use transactions (default: true)
- `BeginBulkInsert()` - Start bulk operation
- `WriteRow(params object?[] values)` - Write single row
- `WriteRows(IEnumerable<object?[]> rows)` - Write multiple rows
- `WriteFromReader(IDataReader reader)` - Copy from reader
- `Complete()` - Finish and commit
- `Abort()` - Cancel operation

### LibSQLConnectionStringBuilder

Builds and parses connection strings.

```csharp
public sealed class LibSQLConnectionStringBuilder : DbConnectionStringBuilder
```

**Key Properties:**
- `DataSource` - Database location
- `Mode` - Connection mode (Local/Remote)
- `AuthToken` - Authentication token
- `ToStringSafe()` - Returns string with masked secrets

### LibSQLDataAdapter

Provides data adapter functionality.

```csharp
public sealed class LibSQLDataAdapter : DbDataAdapter
```

**Key Members:**
- `SelectCommand` - Command for SELECT operations
- `InsertCommand` - Command for INSERT operations
- `UpdateCommand` - Command for UPDATE operations
- `DeleteCommand` - Command for DELETE operations
- `Fill(DataSet)` - Fills a DataSet
- `Update(DataSet)` - Updates database from DataSet

## Enumerations

### LibSQLConnectionMode

```csharp
public enum LibSQLConnectionMode
{
    Local,           // Local file or in-memory
    Remote,          // Remote server
    EmbeddedReplica  // Local with sync capabilities
}
```

### LibSQLDbType

```csharp
public enum LibSQLDbType
{
    Integer = 1,  // 64-bit signed integer
    Real = 2,     // 64-bit floating point
    Text = 3,     // UTF-8 text
    Blob = 4,     // Binary data
    Null = 5      // NULL value
}
```

### LibSQLTransactionBehavior

```csharp
public enum LibSQLTransactionBehavior
{
    Deferred,   // Lock on first write (default)
    Immediate,  // Write lock immediately
    Exclusive   // Exclusive lock immediately
}
```

### ExplainVerbosity

```csharp
public enum ExplainVerbosity
{
    Normal,      // Standard EXPLAIN
    QueryPlan,   // EXPLAIN QUERY PLAN
    Detailed     // Detailed EXPLAIN
}
```

## Sync Classes (Embedded Replica)

### LibSQLSyncResult

Result of a sync operation.

```csharp
public class LibSQLSyncResult
```

**Properties:**
- `FrameNo` - Current frame number after sync
- `FramesSynced` - Number of frames synchronized
- `Duration` - Time taken for sync operation

### LibSQLSyncCompletedEventArgs

Event args for successful sync.

```csharp
public class LibSQLSyncCompletedEventArgs : EventArgs
```

**Properties:**
- `Result` - The sync result

### LibSQLSyncFailedEventArgs

Event args for failed sync.

```csharp
public class LibSQLSyncFailedEventArgs : EventArgs
```

**Properties:**
- `Exception` - The exception that caused failure

## Exception Classes

### LibSQLException

Base exception for all libSQL errors.

```csharp
public class LibSQLException : DbException
```

**Properties:**
- `ErrorCode` - libSQL error code
- `SqlStatement` - SQL that caused the error

### LibSQLConnectionException

Connection-specific errors.

```csharp
public sealed class LibSQLConnectionException : LibSQLException
```

**Properties:**
- `ConnectionString` - Connection string (sanitized)

### LibSQLBusyException

Database busy errors.

```csharp
public sealed class LibSQLBusyException : LibSQLException
```

**Properties:**
- `LockType` - Type of lock causing busy state

### LibSQLConstraintException

Constraint violation errors.

```csharp
public sealed class LibSQLConstraintException : LibSQLException
```

**Properties:**
- `ConstraintType` - Type of constraint violated

## Static Classes

### LibSQLVersion

Provides version information.

```csharp
public static class LibSQLVersion
```

**Properties:**
- `LibSQLVersionNumber` - libSQL version number
- `LibSQLVersionString` - libSQL version string
- `SQLiteVersionNumber` - SQLite version number
- `SQLiteVersionString` - SQLite version string

### LibSQLFactory

ADO.NET provider factory.

```csharp
public sealed class LibSQLFactory : DbProviderFactory
```

**Members:**
- `Instance` - Singleton instance
- `CreateConnection()` - Creates new connection
- `CreateCommand()` - Creates new command
- `CreateParameter()` - Creates new parameter
- `CreateDataAdapter()` - Creates new data adapter

## Extension Methods

While not explicitly part of the public API, common usage patterns include:

```csharp
// Parameter collection extensions
command.Parameters.AddWithValue("@name", value);
command.Parameters.AddWithValue("@name", DbType.Text, value);

// Bulk insert from DataTable
LibSQLBulkInsert.BulkInsertDataTable(connection, dataTable);
```