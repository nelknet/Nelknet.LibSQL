using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Nelknet.LibSQL.Data;

namespace Nelknet.LibSQL.Tests;

public class LibSQLQueryPlanTests : IDisposable
{
    private readonly LibSQLConnection _connection;
    
    public LibSQLQueryPlanTests()
    {
        _connection = new LibSQLConnection("Data Source=:memory:");
        _connection.Open();
        
        // Create test table
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE users (
                id INTEGER PRIMARY KEY,
                name TEXT NOT NULL,
                email TEXT UNIQUE,
                created_at DATETIME DEFAULT CURRENT_TIMESTAMP
            );
            CREATE INDEX idx_users_name ON users(name);
            INSERT INTO users (name, email) VALUES 
                ('Alice', 'alice@example.com'),
                ('Bob', 'bob@example.com'),
                ('Charlie', 'charlie@example.com');
        ";
        cmd.ExecuteNonQuery();
    }
    
    public void Dispose()
    {
        _connection?.Dispose();
    }
    
    [Fact]
    public void GetQueryPlan_WithSimpleQuery_ReturnsQueryPlan()
    {
        // Arrange
        using var command = _connection.CreateCommand();
        command.CommandText = "SELECT * FROM users WHERE name = 'Alice'";
        
        // Act
        var queryPlan = command.GetQueryPlan();
        
        // Assert
        Assert.NotNull(queryPlan);
        Assert.Equal("QueryPlan", queryPlan.TableName);
        Assert.True(queryPlan.Rows.Count > 0, "Query plan should have at least one row");
        Assert.True(queryPlan.Columns.Count > 0, "Query plan should have columns");
        
        // Check if the plan contains expected information
        var planText = queryPlan.Rows[0].ItemArray.Select(x => x?.ToString() ?? "").ToArray();
        var planString = string.Join(" ", planText);
        Assert.Contains("users", planString, StringComparison.OrdinalIgnoreCase);
    }
    
    [Fact]
    public void GetQueryPlan_WithJoinQuery_ShowsJoinPlan()
    {
        // Arrange
        using (var cmd = _connection.CreateCommand())
        {
            cmd.CommandText = @"
                CREATE TABLE orders (
                    id INTEGER PRIMARY KEY,
                    user_id INTEGER,
                    total REAL,
                    FOREIGN KEY (user_id) REFERENCES users(id)
                );
                INSERT INTO orders (user_id, total) VALUES (1, 100.0), (2, 200.0);
            ";
            cmd.ExecuteNonQuery();
        }
        
        using var command = _connection.CreateCommand();
        command.CommandText = @"
            SELECT u.name, o.total 
            FROM users u 
            JOIN orders o ON u.id = o.user_id 
            WHERE o.total > 150";
        
        // Act
        var queryPlan = command.GetQueryPlan();
        
        // Assert
        Assert.NotNull(queryPlan);
        Assert.True(queryPlan.Rows.Count > 0);
        
        // Convert plan to string for checking
        var planRows = new List<string>();
        foreach (DataRow row in queryPlan.Rows)
        {
            planRows.Add(string.Join(" ", row.ItemArray.Select(x => x?.ToString() ?? "")));
        }
        var fullPlan = string.Join("\n", planRows);
        
        // Should show both tables being accessed
        Assert.Contains("users", fullPlan, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("orders", fullPlan, StringComparison.OrdinalIgnoreCase);
    }
    
    [Fact]
    public void GetQueryPlan_WithDifferentVerbosity_ReturnsDifferentDetails()
    {
        // Arrange
        using var command = _connection.CreateCommand();
        command.CommandText = "SELECT * FROM users WHERE id = 1";
        
        // Act - Normal query plan
        command.ExplainVerbosity = ExplainVerbosity.QueryPlan;
        var queryPlan = command.GetQueryPlan();
        
        // Act - Detailed plan
        command.ExplainVerbosity = ExplainVerbosity.Detailed;
        var detailedPlan = command.GetQueryPlan();
        
        // Assert
        Assert.NotNull(queryPlan);
        Assert.NotNull(detailedPlan);
        
        // Detailed plan should have more rows (opcodes)
        Assert.True(detailedPlan.Rows.Count >= queryPlan.Rows.Count);
    }
    
    [Fact]
    public async Task GetQueryPlanAsync_WithSimpleQuery_ReturnsQueryPlan()
    {
        // Arrange
        using var command = _connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM users";
        
        // Act
        var queryPlan = await command.GetQueryPlanAsync();
        
        // Assert
        Assert.NotNull(queryPlan);
        Assert.True(queryPlan.Rows.Count > 0);
    }
    
    [Fact]
    public void GetQueryPlan_WithIndexedQuery_ShowsIndexUsage()
    {
        // Arrange
        using var command = _connection.CreateCommand();
        command.CommandText = "SELECT * FROM users WHERE name = 'Bob'";
        command.ExplainVerbosity = ExplainVerbosity.QueryPlan;
        
        // Act
        var queryPlan = command.GetQueryPlan();
        
        // Assert
        Assert.NotNull(queryPlan);
        var planText = string.Join(" ", queryPlan.Rows[0].ItemArray.Select(x => x?.ToString() ?? ""));
        
        // Should use the index we created
        Assert.Contains("idx_users_name", planText, StringComparison.OrdinalIgnoreCase);
    }
    
    [Fact]
    public void ExplainMode_Property_WorksCorrectly()
    {
        // Arrange
        using var command = _connection.CreateCommand();
        
        // Act & Assert
        Assert.False(command.ExplainMode);
        
        command.ExplainMode = true;
        Assert.True(command.ExplainMode);
        
        command.ExplainMode = false;
        Assert.False(command.ExplainMode);
    }
    
    [Fact]
    public void GetQueryPlan_WithParameterizedQuery_HandlesParameters()
    {
        // Arrange
        using var command = _connection.CreateCommand();
        command.CommandText = "SELECT * FROM users WHERE email = @email";
        command.Parameters.AddWithValue("@email", "alice@example.com");
        
        // Act
        var queryPlan = command.GetQueryPlan();
        
        // Assert
        Assert.NotNull(queryPlan);
        Assert.True(queryPlan.Rows.Count > 0);
    }
}