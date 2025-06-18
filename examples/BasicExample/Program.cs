using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Nelknet.LibSQL.Data;
using Nelknet.LibSQL.Data.Exceptions;

namespace BasicExample;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Nelknet.LibSQL Basic Example");
        Console.WriteLine("============================\n");

        // Example 1: Basic Database Operations
        await BasicDatabaseOperations();
        
        // Example 2: Transactions
        await TransactionExample();
        
        // Example 3: Bulk Insert
        await BulkInsertExample();
        
        // Example 4: Query with Parameters
        await ParameterizedQueryExample();
        
        // Example 5: Error Handling
        await ErrorHandlingExample();
        
        // Example 6: Schema Information
        await SchemaInformationExample();

        Console.WriteLine("\nAll examples completed!");
    }

    static async Task BasicDatabaseOperations()
    {
        Console.WriteLine("1. Basic Database Operations");
        Console.WriteLine("---------------------------");
        
        using var connection = new LibSQLConnection("Data Source=example.db");
        await connection.OpenAsync();
        
        // Create table
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS users (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL,
                    email TEXT UNIQUE,
                    age INTEGER,
                    created_at TEXT DEFAULT CURRENT_TIMESTAMP
                )";
            await cmd.ExecuteNonQueryAsync();
            Console.WriteLine("✓ Table created");
        }
        
        // Insert data
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "INSERT INTO users (name, email, age) VALUES (@name, @email, @age)";
            
            var users = new[]
            {
                ("Alice Smith", "alice@example.com", 28),
                ("Bob Johnson", "bob@example.com", 35),
                ("Charlie Brown", "charlie@example.com", 42)
            };
            
            foreach (var (name, email, age) in users)
            {
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@name", name);
                cmd.Parameters.AddWithValue("@email", email);
                cmd.Parameters.AddWithValue("@age", age);
                await cmd.ExecuteNonQueryAsync();
            }
            Console.WriteLine($"✓ Inserted {users.Length} users");
        }
        
        // Query data
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "SELECT id, name, email, age FROM users ORDER BY name";
            using var reader = await cmd.ExecuteReaderAsync();
            
            Console.WriteLine("\nUsers in database:");
            while (await reader.ReadAsync())
            {
                var id = reader.GetInt64(0);
                var name = reader.GetString(1);
                var email = reader.GetString(2);
                var age = reader.IsDBNull(3) ? (int?)null : reader.GetInt32(3);
                
                Console.WriteLine($"  [{id}] {name} ({email}) - Age: {age ?? 0}");
            }
        }
        
        Console.WriteLine();
    }

    static async Task TransactionExample()
    {
        Console.WriteLine("2. Transaction Example");
        Console.WriteLine("---------------------");
        
        using var connection = new LibSQLConnection("Data Source=example.db");
        await connection.OpenAsync();
        
        // Create accounts table
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS accounts (
                    id INTEGER PRIMARY KEY,
                    name TEXT NOT NULL,
                    balance REAL NOT NULL CHECK (balance >= 0)
                )";
            await cmd.ExecuteNonQueryAsync();
            
            // Insert test accounts
            cmd.CommandText = "INSERT OR REPLACE INTO accounts (id, name, balance) VALUES (@id, @name, @balance)";
            
            var accounts = new[] { (1, "Checking", 1000.0), (2, "Savings", 5000.0) };
            foreach (var (id, name, balance) in accounts)
            {
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@name", name);
                cmd.Parameters.AddWithValue("@balance", balance);
                await cmd.ExecuteNonQueryAsync();
            }
        }
        
        // Perform transfer in transaction
        using var transaction = connection.BeginTransaction();
        try
        {
            var transferAmount = 250.0;
            
            using (var cmd = connection.CreateCommand())
            {
                cmd.Transaction = transaction;
                
                // Withdraw from checking
                cmd.CommandText = "UPDATE accounts SET balance = balance - @amount WHERE id = 1";
                cmd.Parameters.AddWithValue("@amount", transferAmount);
                await cmd.ExecuteNonQueryAsync();
                
                // Deposit to savings
                cmd.Parameters.Clear();
                cmd.CommandText = "UPDATE accounts SET balance = balance + @amount WHERE id = 2";
                cmd.Parameters.AddWithValue("@amount", transferAmount);
                await cmd.ExecuteNonQueryAsync();
            }
            
            transaction.Commit();
            Console.WriteLine($"✓ Transferred ${transferAmount} from Checking to Savings");
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            Console.WriteLine($"✗ Transaction failed: {ex.Message}");
        }
        
        // Show final balances
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "SELECT name, balance FROM accounts ORDER BY id";
            using var reader = await cmd.ExecuteReaderAsync();
            
            Console.WriteLine("\nAccount balances:");
            while (await reader.ReadAsync())
            {
                Console.WriteLine($"  {reader.GetString(0)}: ${reader.GetDouble(1):F2}");
            }
        }
        
        Console.WriteLine();
    }

    static async Task BulkInsertExample()
    {
        Console.WriteLine("3. Bulk Insert Example");
        Console.WriteLine("---------------------");
        
        using var connection = new LibSQLConnection("Data Source=example.db");
        connection.Open();
        
        // Create products table
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS products (
                    id INTEGER PRIMARY KEY,
                    name TEXT NOT NULL,
                    category TEXT,
                    price REAL,
                    stock INTEGER DEFAULT 0
                )";
            cmd.ExecuteNonQuery();
        }
        
        // Generate test data
        var products = Enumerable.Range(1, 1000).Select(i => new
        {
            Id = i,
            Name = $"Product {i}",
            Category = (i % 4) switch
            {
                0 => "Electronics",
                1 => "Clothing",
                2 => "Food",
                _ => "Other"
            },
            Price = Math.Round(Random.Shared.NextDouble() * 100 + 10, 2),
            Stock = Random.Shared.Next(0, 100)
        }).ToList();
        
        // Bulk insert
        var columns = new[] { "id", "name", "category", "price", "stock" };
        using var bulkInsert = new LibSQLBulkInsert(connection, "products", columns)
        {
            BatchSize = 500,
            UseTransaction = true
        };
        
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await bulkInsert.BeginBulkInsertAsync();
        
        foreach (var product in products)
        {
            await bulkInsert.WriteRowAsync(
                product.Id,
                product.Name,
                product.Category,
                product.Price,
                product.Stock
            );
        }
        
        await bulkInsert.CompleteAsync();
        stopwatch.Stop();
        
        Console.WriteLine($"✓ Inserted {products.Count} products in {stopwatch.ElapsedMilliseconds}ms");
        
        // Show category summary
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = @"
                SELECT category, COUNT(*) as count, AVG(price) as avg_price
                FROM products
                GROUP BY category
                ORDER BY category";
            
            using var reader = cmd.ExecuteReader();
            Console.WriteLine("\nCategory summary:");
            while (reader.Read())
            {
                Console.WriteLine($"  {reader.GetString(0)}: {reader.GetInt32(1)} products, avg price ${reader.GetDouble(2):F2}");
            }
        }
        
        Console.WriteLine();
    }

    static async Task ParameterizedQueryExample()
    {
        Console.WriteLine("4. Parameterized Query Example");
        Console.WriteLine("-----------------------------");
        
        using var connection = new LibSQLConnection("Data Source=example.db");
        await connection.OpenAsync();
        
        // Search products by various criteria
        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            SELECT name, category, price, stock
            FROM products
            WHERE category = @category
              AND price BETWEEN @minPrice AND @maxPrice
              AND stock > @minStock
            ORDER BY price DESC
            LIMIT 5";
        
        cmd.Parameters.AddWithValue("@category", "Electronics");
        cmd.Parameters.AddWithValue("@minPrice", 50.0);
        cmd.Parameters.AddWithValue("@maxPrice", 100.0);
        cmd.Parameters.AddWithValue("@minStock", 10);
        
        using var reader = await cmd.ExecuteReaderAsync();
        Console.WriteLine("Top 5 Electronics ($50-$100) with stock > 10:");
        
        int count = 0;
        while (await reader.ReadAsync())
        {
            count++;
            var name = reader.GetString("name");
            var category = reader.GetString("category");
            var price = reader.GetDouble("price");
            var stock = reader.GetInt32("stock");
            
            Console.WriteLine($"  {count}. {name} - ${price:F2} ({stock} in stock)");
        }
        
        if (count == 0)
        {
            Console.WriteLine("  No products found matching criteria");
        }
        
        Console.WriteLine();
    }

    static async Task ErrorHandlingExample()
    {
        Console.WriteLine("5. Error Handling Example");
        Console.WriteLine("------------------------");
        
        using var connection = new LibSQLConnection("Data Source=example.db");
        await connection.OpenAsync();
        
        // Example 1: Constraint violation
        try
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "INSERT INTO users (name, email) VALUES (@name, @email)";
            cmd.Parameters.AddWithValue("@name", "Duplicate User");
            cmd.Parameters.AddWithValue("@email", "alice@example.com"); // Already exists
            await cmd.ExecuteNonQueryAsync();
        }
        catch (LibSQLConstraintException ex)
        {
            Console.WriteLine($"✓ Caught constraint violation: {ex.Message}");
            Console.WriteLine($"  Constraint type: {ex.ConstraintType}");
        }
        catch (LibSQLException ex)
        {
            Console.WriteLine($"✓ Caught database error: {ex.Message}");
            Console.WriteLine($"  Error code: {ex.ErrorCode}");
        }
        
        // Example 2: Invalid SQL
        try
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT * FROM non_existent_table";
            await cmd.ExecuteScalarAsync();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("no such table"))
        {
            Console.WriteLine($"✓ Caught invalid table error: {ex.Message}");
        }
        catch (LibSQLException ex)
        {
            Console.WriteLine($"✓ Caught SQL error: {ex.Message}");
            Console.WriteLine($"  Error code: {ex.ErrorCode}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✓ Caught general error: {ex.GetType().Name}: {ex.Message}");
        }
        
        Console.WriteLine();
    }

    static async Task SchemaInformationExample()
    {
        Console.WriteLine("6. Schema Information Example");
        Console.WriteLine("----------------------------");
        
        using var connection = new LibSQLConnection("Data Source=example.db");
        await connection.OpenAsync();
        
        // Get all tables
        var tables = connection.GetSchema("Tables");
        Console.WriteLine("Tables in database:");
        foreach (DataRow table in tables.Rows)
        {
            var tableName = table["TABLE_NAME"].ToString();
            Console.WriteLine($"  - {tableName}");
            
            // Get columns for this table
            var columns = connection.GetSchema("Columns", new[] { null, null, tableName });
            foreach (DataRow column in columns.Rows)
            {
                var columnName = column["COLUMN_NAME"];
                var dataType = column["DATA_TYPE"];
                var isNullable = column["IS_NULLABLE"];
                var defaultValue = column["COLUMN_DEFAULT"];
                
                Console.WriteLine($"    • {columnName} {dataType} {(isNullable.ToString() == "NO" ? "NOT NULL" : "")} {(defaultValue != DBNull.Value ? $"DEFAULT {defaultValue}" : "")}");
            }
        }
        
        // Get indexes
        Console.WriteLine("\nIndexes:");
        var indexes = connection.GetSchema("Indexes");
        foreach (DataRow index in indexes.Rows)
        {
            if (!index["INDEX_NAME"].ToString()?.StartsWith("sqlite_", StringComparison.Ordinal) ?? false)
            {
                Console.WriteLine($"  - {index["INDEX_NAME"]} on {index["TABLE_NAME"]}.{index["COLUMN_NAME"]}");
            }
        }
        
        Console.WriteLine();
    }
}
