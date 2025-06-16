using System;
using System.Data;
using System.Linq;
using Xunit;
using Nelknet.LibSQL.Data;

namespace Nelknet.LibSQL.Tests;

public class LibSQLSchemaTests : IDisposable
{
    private readonly LibSQLConnection _connection;
    
    public LibSQLSchemaTests()
    {
        _connection = new LibSQLConnection("Data Source=:memory:");
        _connection.Open();
        
        // Create test schema - libSQL doesn't support multi-statement commands
        using (var cmd = _connection.CreateCommand())
        {
            // Create customers table
            cmd.CommandText = @"
                CREATE TABLE customers (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL,
                    email TEXT UNIQUE,
                    created_at DATETIME DEFAULT CURRENT_TIMESTAMP
                )";
            cmd.ExecuteNonQuery();
            
            // Create orders table
            cmd.CommandText = @"
                CREATE TABLE orders (
                    id INTEGER PRIMARY KEY,
                    customer_id INTEGER NOT NULL,
                    order_date DATE NOT NULL,
                    total DECIMAL(10,2),
                    FOREIGN KEY (customer_id) REFERENCES customers(id)
                )";
            cmd.ExecuteNonQuery();
            
            // Create indexes
            cmd.CommandText = "CREATE INDEX idx_orders_customer ON orders(customer_id)";
            cmd.ExecuteNonQuery();
            
            cmd.CommandText = "CREATE INDEX idx_orders_date ON orders(order_date)";
            cmd.ExecuteNonQuery();
            
            cmd.CommandText = "CREATE UNIQUE INDEX idx_customers_email ON customers(email)";
            cmd.ExecuteNonQuery();
            
            // Create a view
            cmd.CommandText = @"
                CREATE VIEW customer_orders AS 
                SELECT c.name, c.email, o.order_date, o.total
                FROM customers c
                JOIN orders o ON c.id = o.customer_id";
            cmd.ExecuteNonQuery();
            
            // Create a trigger
            cmd.CommandText = @"
                CREATE TRIGGER order_update_timestamp
                AFTER UPDATE ON orders
                BEGIN
                    UPDATE orders SET order_date = CURRENT_TIMESTAMP WHERE id = NEW.id;
                END";
            cmd.ExecuteNonQuery();
        }
    }
    
    public void Dispose()
    {
        _connection?.Dispose();
    }
    
    [Fact]
    public void GetSchema_WithoutParameters_ReturnsMetaDataCollections()
    {
        // Act
        var schema = _connection.GetSchema();
        
        // Assert
        Assert.NotNull(schema);
        Assert.Equal("MetaDataCollections", schema.TableName);
        Assert.True(schema.Rows.Count > 0);
        
        // Check for expected collections
        var collectionNames = schema.Rows.Cast<DataRow>()
            .Select(r => r["CollectionName"].ToString())
            .ToList();
        
        Assert.Contains("Tables", collectionNames);
        Assert.Contains("Columns", collectionNames);
        Assert.Contains("Views", collectionNames);
        Assert.Contains("Indexes", collectionNames);
        Assert.Contains("Triggers", collectionNames);
    }
    
    [Fact]
    public void GetSchema_Tables_ReturnsAllTables()
    {
        // Act
        var tables = _connection.GetSchema("Tables");
        
        // Assert
        Assert.NotNull(tables);
        Assert.Equal("Tables", tables.TableName);
        Assert.Equal(2, tables.Rows.Count); // customers and orders
        
        var tableNames = tables.Rows.Cast<DataRow>()
            .Select(r => r["TABLE_NAME"].ToString())
            .OrderBy(n => n)
            .ToList();
        
        Assert.Equal(new[] { "customers", "orders" }, tableNames.ToArray());
        
        // Check table type
        foreach (DataRow row in tables.Rows)
        {
            Assert.Equal("TABLE", row["TABLE_TYPE"]);
            Assert.Equal("main", row["TABLE_SCHEMA"]);
        }
    }
    
    [Fact]
    public void GetSchema_Columns_ReturnsAllColumns()
    {
        // Act
        var columns = _connection.GetSchema("Columns");
        
        // Assert
        Assert.NotNull(columns);
        Assert.True(columns.Rows.Count > 0);
        
        // Check customers table columns
        var customerColumns = columns.Rows.Cast<DataRow>()
            .Where(r => r["TABLE_NAME"].ToString() == "customers")
            .OrderBy(r => Convert.ToInt32(r["ORDINAL_POSITION"]))
            .ToList();
        
        Assert.Equal(4, customerColumns.Count);
        
        // Check first column (id)
        var idColumn = customerColumns[0];
        Assert.Equal("id", idColumn["COLUMN_NAME"]);
        Assert.Equal("INTEGER", idColumn["DATA_TYPE"]);
        Assert.Equal("NO", idColumn["IS_NULLABLE"]);
        Assert.True(Convert.ToBoolean(idColumn["PRIMARY_KEY"]));
    }
    
    [Fact]
    public void GetSchema_ColumnsWithRestrictions_FiltersCorrectly()
    {
        // Act - Get only columns for orders table
        var columns = _connection.GetSchema("Columns", new[] { null, null, "orders", null });
        
        // Assert
        Assert.NotNull(columns);
        var columnNames = columns.Rows.Cast<DataRow>()
            .Select(r => r["COLUMN_NAME"].ToString())
            .OrderBy(n => n)
            .ToList();
        
        Assert.Equal(new[] { "customer_id", "id", "order_date", "total" }, columnNames.ToArray());
        
        // All should be from orders table
        foreach (DataRow row in columns.Rows)
        {
            Assert.Equal("orders", row["TABLE_NAME"]);
        }
    }
    
    [Fact]
    public void GetSchema_Views_ReturnsAllViews()
    {
        // Act
        var views = _connection.GetSchema("Views");
        
        // Assert
        Assert.NotNull(views);
        Assert.Single(views.Rows);
        
        var viewRow = views.Rows[0];
        Assert.Equal("customer_orders", viewRow["TABLE_NAME"]);
        Assert.Equal("main", viewRow["TABLE_SCHEMA"]);
        Assert.NotNull(viewRow["VIEW_DEFINITION"]);
        Assert.Contains("SELECT", viewRow["VIEW_DEFINITION"].ToString());
    }
    
    [Fact]
    public void GetSchema_Indexes_ReturnsAllIndexes()
    {
        // Act
        var indexes = _connection.GetSchema("Indexes");
        
        // Assert
        Assert.NotNull(indexes);
        Assert.True(indexes.Rows.Count > 0);
        
        // Get non-sqlite indexes
        var userIndexes = indexes.Rows.Cast<DataRow>()
            .Where(r => !r["INDEX_NAME"]?.ToString()?.StartsWith("sqlite_") ?? false)
            .Select(r => r["INDEX_NAME"]?.ToString() ?? "")
            .Distinct()
            .OrderBy(n => n)
            .ToList();
        
        Assert.Contains("idx_customers_email", userIndexes);
        Assert.Contains("idx_orders_customer", userIndexes);
        Assert.Contains("idx_orders_date", userIndexes);
        
        // Check unique index
        var uniqueIndex = indexes.Rows.Cast<DataRow>()
            .FirstOrDefault(r => r["INDEX_NAME"].ToString() == "idx_customers_email");
        Assert.NotNull(uniqueIndex);
        Assert.True(Convert.ToBoolean(uniqueIndex["UNIQUE"]));
    }
    
    [Fact]
    public void GetSchema_IndexesWithTableRestriction_FiltersCorrectly()
    {
        // Act
        var indexes = _connection.GetSchema("Indexes", new[] { null, "orders", null });
        
        // Assert
        var indexNames = indexes.Rows.Cast<DataRow>()
            .Select(r => r["INDEX_NAME"]?.ToString() ?? "")
            .Where(n => !n.StartsWith("sqlite_"))
            .Distinct()
            .OrderBy(n => n)
            .ToList();
        
        Assert.Equal(new[] { "idx_orders_customer", "idx_orders_date" }, indexNames.ToArray());
    }
    
    [Fact]
    public void GetSchema_Triggers_ReturnsAllTriggers()
    {
        // Act
        var triggers = _connection.GetSchema("Triggers");
        
        // Assert
        Assert.NotNull(triggers);
        Assert.Single(triggers.Rows);
        
        var triggerRow = triggers.Rows[0];
        Assert.Equal("order_update_timestamp", triggerRow["TRIGGER_NAME"]);
        Assert.Equal("orders", triggerRow["TABLE_NAME"]);
        Assert.Equal("AFTER", triggerRow["TRIGGER_TYPE"]);
        Assert.Equal("UPDATE", triggerRow["TRIGGERING_EVENT"]);
        Assert.NotNull(triggerRow["TRIGGER_DEFINITION"]);
    }
    
    [Fact]
    public void GetSchema_WithInvalidCollection_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _connection.GetSchema("InvalidCollection"));
    }
    
    [Fact]
    public void GetSchema_OnClosedConnection_ThrowsException()
    {
        // Arrange
        using var closedConnection = new LibSQLConnection("Data Source=:memory:");
        
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => closedConnection.GetSchema());
    }
}