# Nelknet.LibSQL Examples

This folder contains example projects demonstrating various features and usage patterns of Nelknet.LibSQL.

## Important Note

These examples require the libSQL native library to be available. Until the NuGet package with bundled native libraries is complete, you'll need to have libSQL installed on your system or manually place the native library in the example's output directory.

## Available Examples

### BasicExample
A comprehensive console application that demonstrates:
- Basic CRUD operations
- Transactions with commit/rollback
- Bulk insert operations
- Parameterized queries
- Error handling
- Schema metadata retrieval

### EmbeddedReplicaExample
Demonstrates embedded replica functionality:
- Setting up an embedded replica connection
- Syncing with remote libSQL/Turso databases
- Working with local data for performance
- Pushing local changes back to remote

## Running Examples

Each example is a standalone console application. To run an example:

```bash
cd BasicExample
dotnet run
```

## Building All Examples

From the repository root:

```bash
dotnet build
```

This will build the entire solution including all examples.

## Adding New Examples

To add a new example:

1. Create a new console application:
   ```bash
   cd examples
   dotnet new console -n YourExampleName
   ```

2. Add reference to Nelknet.LibSQL.Data:
   ```bash
   cd YourExampleName
   dotnet add reference ../../src/Nelknet.LibSQL.Data/Nelknet.LibSQL.Data.csproj
   ```

3. Add to the solution:
   ```bash
   cd ../..
   dotnet sln add examples/YourExampleName/YourExampleName.csproj
   ```

