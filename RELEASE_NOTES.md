# Release Notes

## Version 0.1.0-alpha (In Development)

### Overview
Initial alpha release of Nelknet.LibSQL, a native C# client library for libSQL that follows the ADO.NET pattern.

### Features
- **Native libSQL Integration**: Direct native bindings to libSQL library using modern LibraryImport
- **ADO.NET Compliance**: Full implementation of standard ADO.NET interfaces (DbConnection, DbCommand, DbDataReader, etc.)
- **Cross-Platform Support**: Windows (x64/x86/ARM64), Linux (x64/ARM64), macOS (x64/ARM64)
- **Async Support**: Comprehensive async/await support throughout the API
- **Embedded Replicas**: Local database with sync capabilities to remote libSQL servers
- **Bulk Operations**: High-performance bulk insert functionality
- **Transaction Support**: Full transaction support with isolation levels
- **Parameter Binding**: Named and positional parameter support
- **Schema Discovery**: GetSchema methods for tables, columns, indexes, and views
- **Connection Pooling**: Built-in connection pooling for improved performance
- **Query Plan Access**: Ability to retrieve and analyze query execution plans
- **Progress Monitoring**: Connection progress events for long-running operations
- **Command Events**: Pre/post execution events for monitoring and logging

### Known Limitations
When using the experimental libSQL API:
- Multi-statement commands are not supported (execute statements separately)
- Custom SQL functions cannot be registered (requires sqlite3* handle)
- Backup/restore functionality is not available (requires sqlite3* handle)
- Extended error codes are not accessible (requires sqlite3* handle)

### Breaking Changes
- N/A (Initial release)

### Bug Fixes
- N/A (Initial release)

### Dependencies
- .NET 8.0 or later
- Native libSQL library (automatically downloaded via MSBuild targets)

### Installation
```bash
dotnet add package Nelknet.LibSQL.Data --version 0.1.0-alpha
```

### Usage Example
```csharp
using Nelknet.LibSQL.Data;

// Local database
using var connection = new LibSQLConnection("Data Source=local.db");
await connection.OpenAsync();

// Embedded replica (local with remote sync)
using var replicaConnection = new LibSQLConnection(
    "Data Source=replica.db;SyncUrl=libsql://mydb-user.turso.io;AuthToken=your-token");
await replicaConnection.OpenAsync();

// Sync with remote database
await replicaConnection.SyncAsync();

using var command = connection.CreateCommand();
command.CommandText = "SELECT * FROM users WHERE age > @age";
command.Parameters.AddWithValue("@age", 18);

using var reader = await command.ExecuteReaderAsync();
while (await reader.ReadAsync())
{
    Console.WriteLine($"Name: {reader["name"]}, Age: {reader["age"]}");
}
```

### Contributors
- [Your contributions are welcome!]

### License
MIT License - See LICENSE file for details

---

## Version Template (For Future Releases)

## Version X.Y.Z - YYYY-MM-DD

### Overview
Brief description of the release focus and major changes.

### Features
- Feature 1: Description
- Feature 2: Description

### Improvements
- Improvement 1: Description
- Improvement 2: Description

### Bug Fixes
- Fixed: Issue description (#issue-number)
- Fixed: Issue description (#issue-number)

### Breaking Changes
- Breaking change description and migration guide

### Deprecated
- Deprecated feature/API and replacement

### Security
- Security fix description (CVE-YYYY-NNNNN)

### Performance
- Performance improvement description with metrics

### Dependencies
- Updated dependency X to version Y
- Added new dependency Z

### Contributors
- @contributor1 - Description of contribution
- @contributor2 - Description of contribution

### Known Issues
- Issue description and workaround if available