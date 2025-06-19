# Contributing to Nelknet.LibSQL

Thank you for your interest in contributing to Nelknet.LibSQL! This document provides guidelines and instructions for contributing to the project.

## Table of Contents

- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [Development Setup](#development-setup)
- [Commit Message Guidelines](#commit-message-guidelines)
- [Pull Request Process](#pull-request-process)
- [Testing Guidelines](#testing-guidelines)
- [Documentation](#documentation)
- [Release Process](#release-process)

## Code of Conduct

Please note that this project is released with a Contributor Code of Conduct. By participating in this project you agree to abide by its terms.

## Getting Started

1. Fork the repository
2. Clone your fork: `git clone https://github.com/YOUR_USERNAME/Nelknet.LibSQL.git`
3. Add the upstream remote: `git remote add upstream https://github.com/nelknet/Nelknet.LibSQL.git`
4. Create a new branch: `git checkout -b feature/your-feature-name`

## Development Setup

### Prerequisites

- .NET 8.0 SDK or later
- Docker (for running integration tests)
- Git

### Building the Project

```bash
# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run tests
dotnet test
```

### Running Integration Tests

Integration tests require a local sqld instance:

```bash
# Start sqld server
docker compose up -d sqld

# Run all tests including integration tests
dotnet test

# Stop sqld server
docker compose down
```

## Commit Message Guidelines

We follow the [Conventional Commits](https://www.conventionalcommits.org/) specification. This leads to more readable messages that are easy to follow when looking through the project history.

### Commit Message Format

Each commit message consists of a **header**, a **body**, and a **footer**:

```
<type>(<scope>): <subject>
<BLANK LINE>
<body>
<BLANK LINE>
<footer>
```

### Type

Must be one of the following:

- **feat**: A new feature
- **fix**: A bug fix
- **docs**: Documentation only changes
- **style**: Changes that do not affect the meaning of the code (white-space, formatting, etc)
- **refactor**: A code change that neither fixes a bug nor adds a feature
- **perf**: A code change that improves performance
- **test**: Adding missing tests or correcting existing tests
- **build**: Changes that affect the build system or external dependencies
- **ci**: Changes to our CI configuration files and scripts
- **chore**: Other changes that don't modify src or test files
- **revert**: Reverts a previous commit

### Scope

The scope should be one of the following:

- **bindings**: Changes to Nelknet.LibSQL.Bindings
- **data**: Changes to Nelknet.LibSQL.Data
- **http**: Changes to HTTP/remote connection support
- **native**: Changes to native library management
- **tests**: Changes to test infrastructure
- **examples**: Changes to example projects
- **docs**: Changes to documentation
- **deps**: Dependency updates
- **release**: Release-related changes
- **ci**: CI/CD changes

### Subject

The subject contains a succinct description of the change:

- Use the imperative, present tense: "change" not "changed" nor "changes"
- Don't capitalize the first letter
- No dot (.) at the end
- Maximum 72 characters

### Body

The body should include the motivation for the change and contrast this with previous behavior. Wrap at 100 characters.

### Footer

The footer should contain any information about **Breaking Changes** and is also the place to reference GitHub issues that this commit closes.

### Examples

```
feat(data): add support for bulk insert operations

Implement IBulkInsert interface to allow efficient bulk data insertion.
This significantly improves performance when inserting large datasets.

Closes #123
```

```
fix(http): handle base64 padding in blob deserialization

The sqld server strips Base64 padding from blob responses, causing
deserialization to fail. This fix adds the missing padding before
decoding.

Fixes #456
```

```
BREAKING CHANGE: refactor(bindings): change native library loading mechanism

The native library loading has been refactored to use a more robust
provider pattern. This changes the public API for custom library providers.

Migration guide:
- Replace LibSQLNative.LoadFrom() with LibSQLNative.SetProvider()
- Implement ILibraryProvider instead of using paths directly
```

## Pull Request Process

1. **Update the CHANGELOG.md** with details of your changes in the Unreleased section
2. **Update documentation** if you're changing functionality
3. **Add tests** for any new functionality
4. **Ensure all tests pass** locally
5. **Update examples** if relevant
6. **Submit the PR** with a clear title and description

### PR Title Format

PR titles should follow the same format as commit messages:

```
<type>(<scope>): <subject>
```

### PR Checklist

- [ ] I have read the contributing guidelines
- [ ] My code follows the project's coding style
- [ ] I have added tests for my changes
- [ ] All tests pass locally
- [ ] I have updated the documentation
- [ ] I have updated the CHANGELOG.md
- [ ] My commits follow the conventional commit format

## Testing Guidelines

### Unit Tests

- Place unit tests in the `Nelknet.LibSQL.Tests` project
- Use descriptive test names that explain what is being tested
- Follow the Arrange-Act-Assert pattern
- Mock external dependencies

### Integration Tests

- Integration tests are in the `RemoteIntegrationTests` and other integration test classes
- These require external services (like sqld)
- Use the `[Collection("RemoteIntegration")]` attribute for tests requiring sqld

### Test Naming Convention

```csharp
public void MethodName_StateUnderTest_ExpectedBehavior()
{
    // Test implementation
}
```

Example:
```csharp
public void ExecuteReader_WithParameterizedQuery_ReturnsCorrectResults()
{
    // Test implementation
}
```

## Documentation

### Code Documentation

- Add XML documentation comments to all public APIs
- Include examples in documentation where helpful
- Document any exceptions that can be thrown

### README Updates

Update the README.md if you're:
- Adding new features
- Changing installation instructions
- Modifying configuration options

## Release Process

We use semantic versioning and automated release workflows:

1. **Version Bumping**: Update version in `Directory.Build.props`
2. **Changelog**: Ensure CHANGELOG.md is updated with all changes
3. **Create Release**: Push a tag or use the release workflow
4. **Automated Publishing**: GitHub Actions will build and publish to NuGet

### Version Guidelines

- **Major** (X.0.0): Breaking changes
- **Minor** (0.X.0): New features, backward compatible
- **Patch** (0.0.X): Bug fixes, backward compatible

## Questions?

Feel free to open an issue for any questions about contributing!