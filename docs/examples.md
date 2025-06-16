# Code Examples

This document provides practical code examples for common scenarios when using Nelknet.LibSQL.

## Basic Operations

### Creating and Populating a Database

```csharp
using Nelknet.LibSQL.Data;

// Create a new database
using var connection = new LibSQLConnection("Data Source=mydatabase.db");
connection.Open();

// Create table
using (var cmd = connection.CreateCommand())
{
    cmd.CommandText = @"
        CREATE TABLE IF NOT EXISTS products (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            name TEXT NOT NULL,
            price REAL NOT NULL,
            stock INTEGER DEFAULT 0,
            created_at TEXT DEFAULT CURRENT_TIMESTAMP
        )";
    cmd.ExecuteNonQuery();
}

// Insert data
using (var cmd = connection.CreateCommand())
{
    cmd.CommandText = @"
        INSERT INTO products (name, price, stock) 
        VALUES (@name, @price, @stock)";
    
    var products = new[]
    {
        ("Widget", 9.99, 100),
        ("Gadget", 19.99, 50),
        ("Doohickey", 14.99, 75)
    };
    
    foreach (var (name, price, stock) in products)
    {
        cmd.Parameters.Clear();
        cmd.Parameters.AddWithValue("@name", name);
        cmd.Parameters.AddWithValue("@price", price);
        cmd.Parameters.AddWithValue("@stock", stock);
        cmd.ExecuteNonQuery();
    }
}
```

### Querying with Parameters

```csharp
// Search products by name pattern
using var cmd = connection.CreateCommand();
cmd.CommandText = @"
    SELECT id, name, price, stock 
    FROM products 
    WHERE name LIKE @pattern 
    ORDER BY price";

cmd.Parameters.AddWithValue("@pattern", "%et%");

using var reader = cmd.ExecuteReader();
while (reader.Read())
{
    var id = reader.GetInt64(0);
    var name = reader.GetString(1);
    var price = reader.GetDouble(2);
    var stock = reader.GetInt32(3);
    
    Console.WriteLine($"{id}: {name} - ${price:F2} ({stock} in stock)");
}
```

## Working with Transactions

### Basic Transaction

```csharp
using var transaction = connection.BeginTransaction();
try
{
    // Transfer stock between products
    using (var cmd = connection.CreateCommand())
    {
        cmd.Transaction = transaction;
        
        // Decrease stock from source
        cmd.CommandText = "UPDATE products SET stock = stock - @amount WHERE id = @id";
        cmd.Parameters.AddWithValue("@amount", 10);
        cmd.Parameters.AddWithValue("@id", 1);
        cmd.ExecuteNonQuery();
        
        // Increase stock in destination
        cmd.Parameters.Clear();
        cmd.CommandText = "UPDATE products SET stock = stock + @amount WHERE id = @id";
        cmd.Parameters.AddWithValue("@amount", 10);
        cmd.Parameters.AddWithValue("@id", 2);
        cmd.ExecuteNonQuery();
    }
    
    transaction.Commit();
    Console.WriteLine("Stock transfer completed");
}
catch (Exception ex)
{
    transaction.Rollback();
    Console.WriteLine($"Transaction failed: {ex.Message}");
}
```

### Using Savepoints

```csharp
using var transaction = connection.BeginTransaction();
try
{
    // First operation
    using (var cmd = connection.CreateCommand())
    {
        cmd.Transaction = transaction;
        cmd.CommandText = "INSERT INTO orders (customer_id, total) VALUES (@cid, @total)";
        cmd.Parameters.AddWithValue("@cid", 123);
        cmd.Parameters.AddWithValue("@total", 99.99);
        cmd.ExecuteNonQuery();
    }
    
    // Create savepoint
    transaction.Save("before_items");
    
    try
    {
        // Add order items
        using (var cmd = connection.CreateCommand())
        {
            cmd.Transaction = transaction;
            cmd.CommandText = "INSERT INTO order_items (order_id, product_id, quantity) VALUES (@oid, @pid, @qty)";
            // ... add items
        }
    }
    catch
    {
        // Rollback only to savepoint
        transaction.RollbackTo("before_items");
        Console.WriteLine("Order items failed, but order is preserved");
    }
    
    transaction.Commit();
}
catch
{
    transaction.Rollback();
    throw;
}
```

## Bulk Operations

### Bulk Insert from Collection

```csharp
// Generate test data
var customers = Enumerable.Range(1, 10000).Select(i => new
{
    Name = $"Customer {i}",
    Email = $"customer{i}@example.com",
    Phone = $"555-{i:D4}",
    CreatedAt = DateTime.UtcNow
}).ToList();

// Bulk insert
var columns = new[] { "name", "email", "phone", "created_at" };
using var bulkInsert = new LibSQLBulkInsert(connection, "customers", columns)
{
    BatchSize = 1000,
    UseTransaction = true
};

var stopwatch = Stopwatch.StartNew();
bulkInsert.BeginBulkInsert();

foreach (var customer in customers)
{
    bulkInsert.WriteRow(
        customer.Name,
        customer.Email,
        customer.Phone,
        customer.CreatedAt.ToString("O")
    );
}

bulkInsert.Complete();
stopwatch.Stop();

Console.WriteLine($"Inserted {customers.Count} records in {stopwatch.ElapsedMilliseconds}ms");
```

### Bulk Copy Between Tables

```csharp
// Copy data from one table to another with transformation
using (var selectCmd = connection.CreateCommand())
{
    selectCmd.CommandText = @"
        SELECT 
            name || ' (Archived)' as name,
            email,
            phone,
            created_at
        FROM customers
        WHERE created_at < @cutoff";
    
    selectCmd.Parameters.AddWithValue("@cutoff", DateTime.UtcNow.AddYears(-1));
    
    using var reader = selectCmd.ExecuteReader();
    
    var columns = new[] { "name", "email", "phone", "created_at" };
    using var bulkInsert = new LibSQLBulkInsert(connection, "archived_customers", columns);
    
    bulkInsert.BeginBulkInsert();
    bulkInsert.WriteFromReader(reader);
    bulkInsert.Complete();
}
```

## Advanced Queries

### Using Common Table Expressions (CTEs)

```csharp
using var cmd = connection.CreateCommand();
cmd.CommandText = @"
    WITH category_totals AS (
        SELECT 
            category_id,
            SUM(price * stock) as total_value,
            COUNT(*) as product_count
        FROM products
        GROUP BY category_id
    )
    SELECT 
        c.name as category,
        ct.product_count,
        ct.total_value
    FROM category_totals ct
    JOIN categories c ON c.id = ct.category_id
    WHERE ct.total_value > @min_value
    ORDER BY ct.total_value DESC";

cmd.Parameters.AddWithValue("@min_value", 1000);

using var reader = cmd.ExecuteReader();
while (reader.Read())
{
    Console.WriteLine($"{reader["category"]}: {reader["product_count"]} products, ${reader["total_value"]:F2}");
}
```

### Window Functions

```csharp
using var cmd = connection.CreateCommand();
cmd.CommandText = @"
    SELECT 
        name,
        price,
        category_id,
        ROW_NUMBER() OVER (PARTITION BY category_id ORDER BY price DESC) as price_rank,
        AVG(price) OVER (PARTITION BY category_id) as avg_category_price
    FROM products
    WHERE stock > 0";

using var reader = cmd.ExecuteReader();
while (reader.Read())
{
    var name = reader.GetString(0);
    var price = reader.GetDouble(1);
    var rank = reader.GetInt64(3);
    var avgPrice = reader.GetDouble(4);
    
    Console.WriteLine($"{name}: ${price:F2} (Rank: {rank}, Category Avg: ${avgPrice:F2})");
}
```

## Error Handling

### Handling Specific Error Types

```csharp
try
{
    using var cmd = connection.CreateCommand();
    cmd.CommandText = "INSERT INTO users (email) VALUES (@email)";
    cmd.Parameters.AddWithValue("@email", "duplicate@example.com");
    cmd.ExecuteNonQuery();
}
catch (LibSQLConstraintException ex) when (ex.ConstraintType == ConstraintType.Unique)
{
    Console.WriteLine("Email already exists");
}
catch (LibSQLBusyException ex)
{
    Console.WriteLine($"Database is busy: {ex.LockType}");
    // Implement retry logic
}
catch (LibSQLException ex)
{
    Console.WriteLine($"Database error {ex.ErrorCode}: {ex.Message}");
}
```

### Retry Logic for Busy Database

```csharp
public async Task<T> ExecuteWithRetryAsync<T>(
    Func<Task<T>> operation,
    int maxRetries = 3,
    int delayMs = 100)
{
    for (int i = 0; i < maxRetries; i++)
    {
        try
        {
            return await operation();
        }
        catch (LibSQLBusyException) when (i < maxRetries - 1)
        {
            await Task.Delay(delayMs * (i + 1));
        }
    }
    
    return await operation(); // Last attempt, let it throw
}

// Usage
var result = await ExecuteWithRetryAsync(async () =>
{
    using var cmd = connection.CreateCommand();
    cmd.CommandText = "UPDATE products SET stock = stock - 1 WHERE id = @id";
    cmd.Parameters.AddWithValue("@id", productId);
    return await cmd.ExecuteNonQueryAsync();
});
```

## Performance Optimization

### Using Statement Caching

```csharp
// Enable statement caching for frequently executed queries
connection.EnableStatementCaching = true;
connection.MaxCachedStatements = 200;

// Execute same query multiple times
for (int userId = 1; userId <= 1000; userId++)
{
    using var cmd = connection.CreateCommand();
    cmd.CommandText = "SELECT name, email FROM users WHERE id = @id";
    cmd.Parameters.AddWithValue("@id", userId);
    
    using var reader = cmd.ExecuteReader();
    if (reader.Read())
    {
        ProcessUser(reader.GetString(0), reader.GetString(1));
    }
}
```

### Query Plan Analysis

```csharp
using var cmd = connection.CreateCommand();
cmd.CommandText = @"
    SELECT p.*, c.name as category_name 
    FROM products p
    JOIN categories c ON p.category_id = c.id
    WHERE p.price > @min_price";

cmd.Parameters.AddWithValue("@min_price", 50);

// Get query plan
var queryPlan = cmd.GetQueryPlan();
Console.WriteLine("Query Plan:");
foreach (DataRow row in queryPlan.Rows)
{
    Console.WriteLine($"  {row["id"]}: {row["detail"]}");
}

// Execute the actual query
using var reader = cmd.ExecuteReader();
// Process results...
```

## Working with Remote Databases

### Connecting to Turso

```csharp
// Load credentials from environment
var url = Environment.GetEnvironmentVariable("TURSO_DATABASE_URL");
var token = Environment.GetEnvironmentVariable("TURSO_AUTH_TOKEN");

var builder = new LibSQLConnectionStringBuilder
{
    DataSource = url,
    Mode = LibSQLConnectionMode.Remote,
    AuthToken = token
};

using var connection = new LibSQLConnection(builder.ConnectionString);
connection.Open();

// Use connection normally
using var cmd = connection.CreateCommand();
cmd.CommandText = "SELECT datetime('now') as server_time";
var serverTime = cmd.ExecuteScalar();
Console.WriteLine($"Server time: {serverTime}");
```

## Schema Management

### Getting Table Information

```csharp
// Get all tables
var tables = connection.GetSchema("Tables");
foreach (DataRow table in tables.Rows)
{
    Console.WriteLine($"Table: {table["TABLE_NAME"]}");
}

// Get columns for a specific table
var columns = connection.GetSchema("Columns", new[] { null, null, "products" });
foreach (DataRow column in columns.Rows)
{
    Console.WriteLine($"  {column["COLUMN_NAME"]} {column["DATA_TYPE"]} {(column["IS_NULLABLE"].ToString() == "NO" ? "NOT NULL" : "NULL")}");
}

// Get indexes
var indexes = connection.GetSchema("Indexes", new[] { null, null, "products" });
foreach (DataRow index in indexes.Rows)
{
    Console.WriteLine($"Index: {index["INDEX_NAME"]} on {index["COLUMN_NAME"]}");
}
```

## Integration with Entity Framework Core

While Nelknet.LibSQL doesn't include an EF Core provider, you can use it with Dapper:

```csharp
using Dapper;

// Query with Dapper
var products = connection.Query<Product>(@"
    SELECT id, name, price, stock 
    FROM products 
    WHERE price BETWEEN @min AND @max",
    new { min = 10, max = 100 });

foreach (var product in products)
{
    Console.WriteLine($"{product.Name}: ${product.Price}");
}

// Insert with Dapper
var newProduct = new Product { Name = "New Widget", Price = 29.99, Stock = 50 };
var id = connection.QuerySingle<long>(@"
    INSERT INTO products (name, price, stock) 
    VALUES (@Name, @Price, @Stock);
    SELECT last_insert_rowid()",
    newProduct);

newProduct.Id = id;
```

## Testing

### Using In-Memory Database for Tests

```csharp
[Fact]
public void TestProductRepository()
{
    // Create unique in-memory database for this test
    using var connection = new LibSQLConnection("Data Source=:memory:");
    connection.Open();
    
    // Create schema
    CreateTestSchema(connection);
    
    // Test repository
    var repository = new ProductRepository(connection);
    
    // Insert test
    var product = new Product { Name = "Test", Price = 9.99 };
    var id = repository.Insert(product);
    Assert.True(id > 0);
    
    // Query test
    var loaded = repository.GetById(id);
    Assert.NotNull(loaded);
    Assert.Equal("Test", loaded.Name);
}

private void CreateTestSchema(LibSQLConnection connection)
{
    using var cmd = connection.CreateCommand();
    cmd.CommandText = @"
        CREATE TABLE products (
            id INTEGER PRIMARY KEY,
            name TEXT NOT NULL,
            price REAL NOT NULL
        )";
    cmd.ExecuteNonQuery();
}
```