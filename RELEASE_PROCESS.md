# Release Process Documentation

This document describes the release process for Nelknet.LibSQL after the improvements implemented on 2025-06-19.

## Overview

The release process is now largely automated with proper version tracking, changelog management, and release workflows.

## Key Components

### 1. Version Management

- **Version Location**: `Directory.Build.props` (VersionPrefix and VersionSuffix)
- **Version Format**: Semantic Versioning (MAJOR.MINOR.PATCH[-PRERELEASE])
- **Native Library Tracking**: `src/Nelknet.LibSQL.Bindings/runtimes/LIBSQL_VERSION`

### 2. Workflows

#### a. Build Native Libraries (`build-native-libraries.yml`)
- **Purpose**: Build native libSQL libraries from source
- **Trigger**: Manual (workflow_dispatch)
- **Input**: libSQL ref (branch, tag, or commit)
- **Output**: 
  - Native libraries committed to repo
  - Detailed version info in LIBSQL_VERSION file
- **Usage**: `gh workflow run build-native-libraries.yml -f libsql_ref=v0.6.2`

#### b. Version Bump (`version-bump.yml`)
- **Purpose**: Bump version numbers and prepare for release
- **Trigger**: Manual (workflow_dispatch)
- **Options**: major, minor, patch, prerelease
- **Output**: PR with version updates and CHANGELOG preparation
- **Usage**: `gh workflow run version-bump.yml -f version_type=minor`

#### c. Release (`release.yml`)
- **Purpose**: Create GitHub release and publish to NuGet
- **Trigger**: 
  - Push of tags matching `v*.*.*`
  - Manual (workflow_dispatch)
- **Process**:
  1. Validates version consistency
  2. Checks CHANGELOG entry exists
  3. Creates GitHub release
  4. Publishes to NuGet
  5. Creates PR to prepare CHANGELOG for next version

#### d. Release Drafter (`release-drafter.yml`)
- **Purpose**: Automatically draft release notes from PRs
- **Trigger**: Push to main, PR events
- **Output**: Draft release with categorized changes

### 3. Commit Convention

We use Conventional Commits format:
```
<type>(<scope>): <subject>
```

**Types**: feat, fix, docs, style, refactor, perf, test, build, ci, chore, revert

**Scopes**: bindings, data, http, native, tests, examples, docs, deps, release, ci

### 4. Changelog Management

- **Format**: Keep a Changelog
- **Location**: `CHANGELOG.md`
- **Requirement**: PRs must update CHANGELOG (unless labeled with `skip-changelog`)
- **Automation**: Release workflow adds dates and prepares for next version

## Release Process Steps

### 1. Regular Development
1. Create feature branch
2. Make changes following conventional commits
3. Update CHANGELOG.md in PR
4. Get PR reviewed and merged

### 2. Prepare for Release
1. Run version bump workflow:
   ```bash
   gh workflow run version-bump.yml -f version_type=minor
   ```
2. Review and merge the version bump PR
3. Update CHANGELOG with any last-minute changes

### 3. Create Release
**Option A - Tag Push**:
```bash
git tag v0.2.0
git push origin v0.2.0
```

**Option B - Manual Workflow**:
```bash
gh workflow run release.yml -f version=0.2.0
```

### 4. Post-Release
- Release workflow automatically:
  - Creates GitHub release
  - Publishes to NuGet
  - Creates PR to prepare CHANGELOG for next version
- Merge the CHANGELOG preparation PR

## Updating Native Libraries

When updating to a new libSQL version:

1. Check libSQL releases: https://github.com/tursodatabase/libsql/releases
2. Run the build workflow:
   ```bash
   gh workflow run build-native-libraries.yml -f libsql_ref=v0.6.2
   ```
3. The workflow will:
   - Build libraries for all platforms
   - Commit them to the repository
   - Update LIBSQL_VERSION with detailed info

## CI/CD Features

### PR Checks
- **Changelog Check**: Ensures CHANGELOG is updated (skip with `skip-changelog` label)
- **Commit Lint**: Validates conventional commit format
- **Build & Test**: Runs on Linux, Windows, macOS

### Automated Features
- **Release Notes**: Draft created automatically from PR titles
- **Version Validation**: Ensures consistency across files
- **NuGet Publishing**: Automatic on release creation

## Secrets Required

Add these secrets to GitHub repository settings:
- `NUGET_API_KEY`: API key for publishing to nuget.org
  - Get your API key from: https://www.nuget.org/account/apikeys
  - Permissions needed: "Push" and "Push new packages"
  - Glob pattern: "Nelknet.LibSQL*"

## Labels for PRs

Create these labels for better automation:
- `skip-changelog`: Skip CHANGELOG check
- `dependencies`: Dependency updates (skip CHANGELOG)
- `documentation`: Documentation changes (skip CHANGELOG)
- `breaking-change`: Breaking changes (affects version bumping)
- `feature`, `enhancement`: New features
- `fix`, `bug`: Bug fixes
- `chore`, `maintenance`: Maintenance tasks

## Tips

1. **Pre-release Versions**: Use the version bump workflow with `prerelease` type
2. **Hotfixes**: Create release directly from hotfix branch
3. **Release Notes**: Can be edited in GitHub after creation
4. **Failed Releases**: NuGet push uses `--skip-duplicate` so reruns are safe

## Troubleshooting

**Version Mismatch Error**: Ensure Directory.Build.props version matches the release tag

**CHANGELOG Check Fails**: Either update CHANGELOG.md or add `skip-changelog` label

**Native Library Build Fails**: Check the libSQL ref exists and is valid

**NuGet Push Fails**: Verify NUGET_API_KEY secret is set correctly