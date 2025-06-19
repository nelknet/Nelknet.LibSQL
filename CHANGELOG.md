# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Fixed
- Add missing `contents: write` permission to publish-nuget job in release workflow
- Remove non-existent `*.snupkg` pattern from release file upload

## [0.2.1]

### Added

### Changed

### Deprecated

### Removed

### Fixed

### Security


## [0.2.0]

### Added
- Automated release process with GitHub Actions workflows
- Conventional commits support with commit linting
- Release drafter for automatic release notes generation
- Version bump workflow for automated version management
- Enhanced native library build workflow with detailed version tracking
- CHANGELOG enforcement in CI for pull requests
- Comprehensive CONTRIBUTING.md with commit message guidelines
- RELEASE_PROCESS.md documentation

### Changed
- Moved from alpha to stable pre-1.0 status
- Updated README to reflect production-ready status with pre-1.0 API stability caveat
- Improved build-native-libraries workflow to track libSQL version details (commit SHA, tag, version)
- CI workflow now checks for CHANGELOG updates on PRs

### Removed
- Deleted inconsistent version tags (v1.0.1-v1.0.4) that didn't match package versions
- Removed redundant publish-nuget.yml workflow (superseded by release.yml)

## [0.1.0-alpha] - 2025-06-17

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

[Unreleased]: https://github.com/nelknet/Nelknet.LibSQL/compare/v0.2.1...HEAD
[0.2.1]: https://github.com/nelknet/Nelknet.LibSQL/compare/v0.2.0...v0.2.1
[0.2.0]: https://github.com/nelknet/Nelknet.LibSQL/compare/v0.1.0-alpha...v0.2.0
[0.1.0-alpha]: https://github.com/nelknet/Nelknet.LibSQL/releases/tag/v0.1.0-alpha