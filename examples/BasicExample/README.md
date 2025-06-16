# Nelknet.LibSQL Basic Example

This example demonstrates the basic usage of Nelknet.LibSQL, including:

1. **Basic Database Operations** - Creating tables, inserting data, and querying
2. **Transactions** - Using transactions with commit and rollback
3. **Bulk Insert** - High-performance bulk data insertion
4. **Parameterized Queries** - Safe queries with parameters
5. **Error Handling** - Handling specific database errors
6. **Schema Information** - Retrieving database metadata

## Prerequisites

This example requires the libSQL native library to be available on your system. Until the NuGet package with bundled native libraries is available (Phase 19), you need to either:

1. Install libSQL system-wide
2. Place the appropriate native library (`libsql.dylib` on macOS, `libsql.so` on Linux, or `libsql.dll` on Windows) in the example's output directory

## Running the Example

From the BasicExample directory:

```bash
dotnet run
```

This will create an `example.db` file in the current directory and demonstrate various features of the library.

**Note**: If you get a `DllNotFoundException`, ensure the libSQL native library is available as described in Prerequisites above.

## Key Features Demonstrated

### Basic CRUD Operations
- Creating tables with constraints
- Inserting data with parameters
- Querying and reading results
- Handling NULL values

### Transactions
- Beginning transactions
- Multiple operations in a transaction
- Commit and rollback
- Check constraints

### Bulk Operations
- Inserting 1000 records efficiently
- Batch processing with transactions
- Performance timing

### Advanced Queries
- Parameterized queries with multiple conditions
- Using BETWEEN, ORDER BY, and LIMIT
- Aggregate functions (COUNT, AVG)
- Reading by column name

### Error Handling
- Catching specific exception types
- Handling constraint violations
- Dealing with SQL errors

### Schema Metadata
- Listing all tables
- Getting column information
- Viewing indexes
- Filtering system tables

## Output

The example produces detailed output showing each operation:

```
Nelknet.LibSQL Basic Example
============================

1. Basic Database Operations
---------------------------
✓ Table created
✓ Inserted 3 users

Users in database:
  [1] Alice Smith (alice@example.com) - Age: 28
  [2] Bob Johnson (bob@example.com) - Age: 35
  [3] Charlie Brown (charlie@example.com) - Age: 42

...
```

## Cleanup

To remove the created database file:

```bash
rm example.db
```