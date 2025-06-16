using System;
using System.Threading.Tasks;
using Xunit;
using Nelknet.LibSQL.Data;

namespace Nelknet.LibSQL.Tests
{
    public class LibSQLMultiStatementTests : IDisposable
    {
        private LibSQLConnection _connection;

        public LibSQLMultiStatementTests()
        {
            _connection = new LibSQLConnection("Data Source=:memory:");
            _connection.Open();
        }

        public void Dispose()
        {
            _connection?.Dispose();
        }

        [Fact]
        public void ExecuteNonQuery_WithMultipleCreateStatements_OnlyExecutesFirst()
        {
            // Arrange
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE table1 (id INTEGER PRIMARY KEY);
                CREATE TABLE table2 (id INTEGER PRIMARY KEY);
                CREATE TABLE table3 (id INTEGER PRIMARY KEY);";

            // Act
            var result = cmd.ExecuteNonQuery();

            // Assert
            Assert.Equal(0, result); // Only first statement's result is returned

            // Verify only first table was created
            using var checkCmd = _connection.CreateCommand();
            checkCmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name LIKE 'table%' ORDER BY name";
            using var reader = checkCmd.ExecuteReader();
            
            var tableCount = 0;
            string? firstName = null;
            while (reader.Read())
            {
                if (firstName == null)
                    firstName = reader.GetString(0);
                tableCount++;
            }

            Assert.Equal(1, tableCount);
            Assert.Equal("table1", firstName);
        }

        [Fact]
        public void ExecuteNonQuery_WithMultipleInsertStatements_OnlyExecutesFirst()
        {
            // Arrange
            CreateTestTable();
            
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO test_table (value) VALUES ('first');
                INSERT INTO test_table (value) VALUES ('second');
                INSERT INTO test_table (value) VALUES ('third');";

            // Act
            var result = cmd.ExecuteNonQuery();

            // Assert
            Assert.Equal(1, result); // Only first insert's affected rows

            // Verify only one row was inserted
            using var checkCmd = _connection.CreateCommand();
            checkCmd.CommandText = "SELECT COUNT(*) FROM test_table";
            var count = Convert.ToInt32(checkCmd.ExecuteScalar());
            Assert.Equal(1, count);
        }

        [Fact]
        public void ExecuteNonQuery_WithMixedStatements_OnlyExecutesFirst()
        {
            // Arrange
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE mixed_test (id INTEGER PRIMARY KEY, name TEXT);
                INSERT INTO mixed_test (name) VALUES ('test');
                CREATE INDEX idx_mixed_test ON mixed_test(name);";

            // Act
            var result = cmd.ExecuteNonQuery();

            // Assert
            Assert.Equal(0, result); // CREATE TABLE returns 0

            // Verify table was created but no data or index
            using var checkCmd = _connection.CreateCommand();
            checkCmd.CommandText = "SELECT COUNT(*) FROM mixed_test";
            var count = Convert.ToInt32(checkCmd.ExecuteScalar());
            Assert.Equal(0, count); // No data inserted

            checkCmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='index' AND name='idx_mixed_test'";
            count = Convert.ToInt32(checkCmd.ExecuteScalar());
            Assert.Equal(0, count); // No index created
        }

        [Fact]
        public void ExecuteReader_WithMultipleSelectStatements_OnlyExecutesFirst()
        {
            // Arrange
            CreateTestTable();
            InsertTestData();

            using var cmd = _connection.CreateCommand();
            cmd.CommandText = @"
                SELECT 'first' as query_id, COUNT(*) as count FROM test_table;
                SELECT 'second' as query_id, COUNT(*) as count FROM test_table WHERE value='A';";

            // Act
            using var reader = cmd.ExecuteReader();

            // Assert
            Assert.True(reader.Read());
            Assert.Equal("first", reader.GetString(0));
            Assert.Equal(3, reader.GetInt32(1));
            
            Assert.False(reader.Read()); // No more rows
            Assert.False(reader.NextResult()); // No second result set
        }

        [Fact]
        public void ExecuteScalar_WithMultipleStatements_ReturnsFirstStatementResult()
        {
            // Arrange
            CreateTestTable();
            InsertTestData();

            using var cmd = _connection.CreateCommand();
            cmd.CommandText = @"
                SELECT 'first result';
                SELECT 'second result';
                SELECT 'third result';";

            // Act
            var result = cmd.ExecuteScalar();

            // Assert
            Assert.NotNull(result);
            Assert.Equal("first result", result.ToString());
        }

        [Fact]
        public async Task ExecuteNonQueryAsync_WithMultipleStatements_OnlyExecutesFirst()
        {
            // Arrange
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE async_table1 (id INTEGER);
                CREATE TABLE async_table2 (id INTEGER);";

            // Act
            var result = await cmd.ExecuteNonQueryAsync();

            // Assert
            Assert.Equal(0, result);

            // Verify only first table exists
            using var checkCmd = _connection.CreateCommand();
            checkCmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name LIKE 'async_table%'";
            var count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
            Assert.Equal(1, count);
        }

        [Fact]
        public void MultiStatement_InTransaction_StillOnlyExecutesFirst()
        {
            // Arrange
            using var transaction = _connection.BeginTransaction();
            using var cmd = _connection.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = @"
                CREATE TABLE trans_table1 (id INTEGER);
                CREATE TABLE trans_table2 (id INTEGER);
                CREATE TABLE trans_table3 (id INTEGER);";

            // Act
            var result = cmd.ExecuteNonQuery();
            transaction.Commit();

            // Assert
            Assert.Equal(0, result);

            // Verify only first table was created
            using var checkCmd = _connection.CreateCommand();
            checkCmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name LIKE 'trans_table%'";
            var count = Convert.ToInt32(checkCmd.ExecuteScalar());
            Assert.Equal(1, count);
        }

        [Fact]
        public void MultiStatement_WithSemicolonSeparatedOnSingleLine_OnlyExecutesFirst()
        {
            // Arrange
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "CREATE TABLE semi1 (id INTEGER); CREATE TABLE semi2 (id INTEGER); CREATE TABLE semi3 (id INTEGER);";

            // Act
            var result = cmd.ExecuteNonQuery();

            // Assert
            Assert.Equal(0, result);

            using var checkCmd = _connection.CreateCommand();
            checkCmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name LIKE 'semi%'";
            var count = Convert.ToInt32(checkCmd.ExecuteScalar());
            Assert.Equal(1, count);
        }

        private void CreateTestTable()
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "CREATE TABLE test_table (id INTEGER PRIMARY KEY AUTOINCREMENT, value TEXT)";
            cmd.ExecuteNonQuery();
        }

        private void InsertTestData()
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "INSERT INTO test_table (value) VALUES ('A')";
            cmd.ExecuteNonQuery();
            cmd.CommandText = "INSERT INTO test_table (value) VALUES ('B')";
            cmd.ExecuteNonQuery();
            cmd.CommandText = "INSERT INTO test_table (value) VALUES ('C')";
            cmd.ExecuteNonQuery();
        }
    }
}