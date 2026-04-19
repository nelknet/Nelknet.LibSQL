# Repository Guidelines

## Project Structure & Module Organization
`src/` contains the production libraries: `src/Nelknet.LibSQL.Bindings` for native interop and runtime assets, and `src/Nelknet.LibSQL.Data` for the ADO.NET provider, HTTP support, and packaging metadata. `tests/Nelknet.LibSQL.Tests` holds the xUnit suite. `examples/` contains runnable sample apps. Shared build metadata lives in `Directory.Build.props` and `Directory.Packages.props`; release and packaging helpers live at the repo root and in `scripts/`.

## Build, Test, and Development Commands
- `dotnet restore` restores solution dependencies.
- `dotnet build Nelknet.LibSQL.sln` builds all projects with analyzers and warnings treated as errors.
- `dotnet test` runs the xUnit suite.
- `docker compose up -d sqld` starts the local libSQL server used by remote integration tests.
- `LIBSQL_TEST_URL=http://localhost:8080 LIBSQL_TEST_TOKEN=test dotnet test` runs tests against a live server.
- `./build-all.sh --managed` builds managed assemblies; add `--pack` or `--all` for packaging flows.
- `./pack.sh --package-type ManagedOnly` creates NuGet packages in `artifacts/`.

## Coding Style & Naming Conventions
Target `.NET 8` and follow the existing type-driven design: prefer immutable models, functional decomposition, and APIs that make illegal states unrepresentable. Use nullable reference types correctly and do not suppress warnings casually. Indentation is enforced by `.editorconfig`: 4 spaces for `*.cs`, 2 for project, JSON, and YAML files. Use `PascalCase` for types and members, `camelCase` for locals and parameters, and keep test names in the `Method_State_ExpectedBehavior` style already used in the suite.

## Testing Guidelines
Tests use `xUnit` with `coverlet.collector`. Add unit tests for all public behavior changes and integration tests for remote, sync, or native-library scenarios. Keep fast tests isolated from network dependencies; remote cases should rely on `LIBSQL_TEST_URL` and `LIBSQL_TEST_TOKEN`. When changing packaging or runtime loading, verify the relevant platform-specific tests before opening a PR.

## Commit & Pull Request Guidelines
Use Conventional Commits: `type(scope): subject`. Valid scopes in this repo include `bindings`, `data`, `http`, `native`, `tests`, `examples`, `docs`, `deps`, `release`, and `ci`. Keep subjects lowercase and under 72 characters. Never add co-authors or any AI-generated attribution text to commits. PRs should include a clear description, linked issue when applicable, updated tests, and `CHANGELOG.md` updates for user-facing changes.

## Security & Configuration Tips
Do not commit real auth tokens, connection strings, or local test secrets. Treat files under `src/Nelknet.LibSQL.Bindings/runtimes/` as packaged runtime assets: verify paths and package output carefully when modifying them.
