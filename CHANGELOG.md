# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Initial implementation of native libSQL bindings using LibraryImport
- Full ADO.NET provider implementation (DbConnection, DbCommand, DbDataReader, etc.)
- Cross-platform support for Windows, Linux, and macOS (x86, x64, ARM64)
- Comprehensive async/await support throughout the API
- Bulk insert operations for high-performance data loading
- Transaction support with configurable isolation levels
- Named and positional parameter binding
- Schema discovery via GetSchema methods
- Connection pooling for improved performance
- Query plan access for performance analysis
- Connection progress events for monitoring long operations
- Command execution events (CommandExecuting, CommandExecuted)
- **Embedded replica support**
  - Manual sync operations with `Sync()` and `SyncAsync()` methods
  - Automatic sync with configurable intervals via `SyncInterval` connection string option
  - Read-your-writes consistency configuration
  - Offline mode for disconnected operation
  - Sync event notifications (SyncStarted, SyncCompleted, SyncFailed)
  - Connection string support for embedded replicas
- Comprehensive test suite with 318 passing tests
- NuGet package configuration with Source Link support
- Symbol packages (.snupkg) for debugging support

### Known Issues
- Multi-statement commands are not supported when using experimental libSQL API
- Custom SQL functions cannot be registered (requires sqlite3* handle)
- Backup/restore functionality is not available (requires sqlite3* handle)
- Extended error codes are not accessible (requires sqlite3* handle)

## [0.1.0-alpha] - TBD

### Added
- Everything listed in Unreleased section above

[Unreleased]: https://github.com/yourusername/Nelknet.LibSQL/compare/v0.1.0-alpha...HEAD
[0.1.0-alpha]: https://github.com/yourusername/Nelknet.LibSQL/releases/tag/v0.1.0-alpha