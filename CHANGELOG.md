# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

### Changed

### Deprecated

### Removed

### Fixed

### Security

## [0.2.12] - 2026-08-11

### Added

### Changed
- Merge weekly Dependabot package dependency updates (#106)

### Deprecated

### Removed

### Fixed

### Security

## [0.2.11] - 2026-07-15

### Added

### Changed

### Deprecated

### Removed

### Fixed
- Preserve Hrana HTTP stream state across requests, honor server-provided
  pipeline URLs, and surface top-level protocol errors so remote transactions
  commit and roll back correctly.
- Complete local `INSERT`, `UPDATE`, `DELETE`, and `REPLACE` statements with a
  `RETURNING` clause when readers are closed early, keeping statement handles
  alive long enough for transaction commits to persist their writes.

### Security

## [0.2.10] - 2026-07-14

### Added

### Changed
- Merge weekly Dependabot package dependency updates (#97)

### Deprecated

### Removed

### Fixed

### Security

## [0.2.9] - 2026-06-23

### Added

### Changed
- Merge weekly Dependabot package dependency updates (#89)

### Deprecated

### Removed

### Fixed

### Security

## [0.2.8] - 2026-05-19

### Added
- Add weekly Dependabot maintenance automation that merges eligible green Dependabot PRs after a seven-day cooling-off window and publishes a NuGet release for package dependency updates

### Changed
- Merge weekly Dependabot package dependency updates (#82)
- Add a seven-day Dependabot cooldown for NuGet and GitHub Actions version updates
- Run weekly Dependabot maintenance on Tuesdays so Monday Dependabot PRs exceed the seven-day merge threshold before automation evaluates them

### Deprecated

### Removed

### Fixed
- Fix weekly Dependabot maintenance dry runs so status-check evaluation handles completed check conclusions correctly
- Fix release workflow changelog maintenance so the post-release PR does not duplicate an already dated release heading

### Security

## [0.2.7] - 2026-04-28

### Added

### Changed
- Bump `Microsoft.Extensions.Http` and `System.Text.Json` to `10.0.7`

### Deprecated

### Removed

### Fixed
- Fix release workflow notes extraction so GitHub release notes include only the requested changelog version section
- Fix single-file and NativeAOT publishes by making native library probing bundle-safe, using source-generated Hrana JSON serialization, matching `DbDataReader.GetFieldType` trim annotations, and adding a NativeAOT smoke test for local and HTTP connections ([#64](https://github.com/nelknet/Nelknet.LibSQL/issues/64))
- Fix local and HTTP command binding for named parameters (`@name`, `:name`, `$name`) so values are resolved by SQL marker name and position instead of collection order, preventing silent value swaps when `Parameters.Add` order differs from SQL marker order ([#65](https://github.com/nelknet/Nelknet.LibSQL/issues/65))

### Security

## [0.2.6] - 2026-04-22

### Added
- Add daily automation to detect new upstream libSQL release tags, open update PRs, auto-merge green PRs, and dispatch NuGet releases
- Add helper scripts to generate the bundled libSQL badge metadata and prepare automated release update branches

### Changed
- Prefer published upstream libSQL release tags over `main` when vendoring native libSQL binaries
- Update the bundled libSQL native libraries to upstream release tag `libsql-server-v0.24.32`
- Update the README bundled libSQL badge to show the vendored upstream release tag instead of the Rust cargo workspace version

### Deprecated

### Removed

### Fixed
- Skip redundant manual tag creation in the release workflow after the GitHub release step has already created the tag
- Fix Windows GNU tagged native builds by forcing the MinGW C++ toolchain and Windows CMake hints during libSQL cross-compilation

### Security

## [0.2.5] - 2026-04-20

### Added

### Changed
- Upgrade bundled libSQL native libraries to upstream commit `e4beacaa266fba930b637515e2082b42c2d6a817`
- Publish `Nelknet.LibSQL.Data.Full` alongside the managed and bindings packages during releases

### Deprecated

### Removed

### Fixed
- Fix NULL column type handling in `LibSQLDataReader` metadata and `IsDBNull`
- Run Release Drafter only on pushes to `main` to avoid pull request `target_commitish` failures
- Avoid duplicate `Unreleased` sections in automated post-release changelog updates
- Make the automated changelog preparation PR follow the repo's conventional commit rules

### Security


## [0.2.4] - 2025-06-19 - 2025-06-19

### Changed
- Bump version to 0.2.4 for release

### Fixed
- Fixed native library packaging structure in Nelknet.LibSQL.Bindings NuGet package - libraries were being packaged with duplicate paths (e.g., `runtimes/osx-arm64/native/osx-arm64/native/libsql.dylib`) preventing .NET's automatic native library resolution
- Fixed release workflow version extraction to use portable sed commands instead of GNU grep with PCRE

## [0.2.3] - 2025-06-19

### Fixed
- Add missing `base: main` parameter to Create Pull Request step in release workflow to fix update-changelog job

## [0.2.2] - 2025-06-19

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





[Unreleased]: https://github.com/nelknet/Nelknet.LibSQL/compare/v0.2.12...HEAD
[0.2.12]: https://github.com/nelknet/Nelknet.LibSQL/compare/v0.2.11...v0.2.12
[0.2.11]: https://github.com/nelknet/Nelknet.LibSQL/compare/v0.2.10...v0.2.11
[0.2.10]: https://github.com/nelknet/Nelknet.LibSQL/compare/v0.2.9...v0.2.10
[0.2.9]: https://github.com/nelknet/Nelknet.LibSQL/compare/v0.2.8...v0.2.9
[0.2.8]: https://github.com/nelknet/Nelknet.LibSQL/compare/v0.2.7...v0.2.8
[0.2.7]: https://github.com/nelknet/Nelknet.LibSQL/compare/v0.2.6...v0.2.7
[0.2.6]: https://github.com/nelknet/Nelknet.LibSQL/compare/v0.2.5...v0.2.6
[0.2.5]: https://github.com/nelknet/Nelknet.LibSQL/compare/v0.2.4...v0.2.5
[0.2.4]: https://github.com/nelknet/Nelknet.LibSQL/compare/v0.2.3...v0.2.4
[0.2.3]: https://github.com/nelknet/Nelknet.LibSQL/compare/v0.2.2...v0.2.3
[0.2.2]: https://github.com/nelknet/Nelknet.LibSQL/compare/v0.2.1...v0.2.2
[0.2.1]: https://github.com/nelknet/Nelknet.LibSQL/compare/v0.2.0...v0.2.1
[0.2.0]: https://github.com/nelknet/Nelknet.LibSQL/compare/v0.1.0-alpha...v0.2.0
[0.1.0-alpha]: https://github.com/nelknet/Nelknet.LibSQL/releases/tag/v0.1.0-alpha
