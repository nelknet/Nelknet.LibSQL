using System;
using System.Threading.Tasks;
using Nelknet.LibSQL.Data;
using Nelknet.LibSQL.Data.Exceptions;
using Xunit;
using Xunit.Abstractions;

namespace Nelknet.LibSQL.Tests
{
    /// <summary>
    /// Tests for batch execution over remote HTTP connections.
    /// </summary>
    [Collection("Sequential")]
    public class RemoteBatchTests : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly string _connectionString;

        public RemoteBatchTests(ITestOutputHelper output)
        {
            _output = output;
            
            // Use environment variables for remote connection testing
            var url = Environment.GetEnvironmentVariable("LIBSQL_TEST_URL") ?? "https://test-db.turso.io";
            var authToken = Environment.GetEnvironmentVariable("LIBSQL_TEST_AUTH_TOKEN") ?? "";
            
            _connectionString = $"Data Source={url};Auth Token={authToken}";
        }

        [Fact]
        public async Task ExecuteBatchAsync_WithMultipleStatements_ExecutesAll()
        {
            // Skip if no test database is configured
            if (!IsTestDatabaseConfigured())
            {
                _output.WriteLine("Skipping test - no test database configured");
                return;
            }

            using var connection = new LibSQLConnection(_connectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            
            // Create test tables and insert data
            var statements = new[]
            {
                "DROP TABLE IF EXISTS batch_test1",
                "DROP TABLE IF EXISTS batch_test2",
                "CREATE TABLE batch_test1 (id INTEGER PRIMARY KEY, name TEXT)",
                "CREATE TABLE batch_test2 (id INTEGER PRIMARY KEY, value INTEGER)",
                "INSERT INTO batch_test1 (name) VALUES ('Alice')",
                "INSERT INTO batch_test1 (name) VALUES ('Bob')",
                "INSERT INTO batch_test2 (value) VALUES (100)",
                "INSERT INTO batch_test2 (value) VALUES (200)"
            };

            var affected = await command.ExecuteBatchAsync(statements);
            _output.WriteLine($"Total affected rows: {affected}");
            
            // Verify data was inserted
            command.CommandText = "SELECT COUNT(*) FROM batch_test1";
            var count1 = Convert.ToInt32(await command.ExecuteScalarAsync());
            Assert.Equal(2, count1);
            
            command.CommandText = "SELECT COUNT(*) FROM batch_test2";
            var count2 = Convert.ToInt32(await command.ExecuteScalarAsync());
            Assert.Equal(2, count2);
            
            // Cleanup
            command.CommandText = "DROP TABLE batch_test1";
            await command.ExecuteNonQueryAsync();
            command.CommandText = "DROP TABLE batch_test2";
            await command.ExecuteNonQueryAsync();
        }

        [Fact]
        public async Task ExecuteTransactionalBatchAsync_WithAllSuccessful_CommitsTransaction()
        {
            // Skip if no test database is configured
            if (!IsTestDatabaseConfigured())
            {
                _output.WriteLine("Skipping test - no test database configured");
                return;
            }

            using var connection = new LibSQLConnection(_connectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            
            // Setup
            command.CommandText = "DROP TABLE IF EXISTS trans_batch_test";
            await command.ExecuteNonQueryAsync();
            command.CommandText = "CREATE TABLE trans_batch_test (id INTEGER PRIMARY KEY, value INTEGER)";
            await command.ExecuteNonQueryAsync();

            // Execute transactional batch
            var statements = new[]
            {
                "INSERT INTO trans_batch_test (value) VALUES (1)",
                "INSERT INTO trans_batch_test (value) VALUES (2)",
                "UPDATE trans_batch_test SET value = value + 10"
            };

            var affected = await command.ExecuteTransactionalBatchAsync(statements);
            _output.WriteLine($"Total affected rows: {affected}");
            
            // Verify all changes were committed
            command.CommandText = "SELECT COUNT(*) FROM trans_batch_test";
            var count = Convert.ToInt32(await command.ExecuteScalarAsync());
            Assert.Equal(2, count);
            
            command.CommandText = "SELECT SUM(value) FROM trans_batch_test";
            var sum = Convert.ToInt32(await command.ExecuteScalarAsync());
            Assert.Equal(23, sum); // (1+10) + (2+10) = 23
            
            // Cleanup
            command.CommandText = "DROP TABLE trans_batch_test";
            await command.ExecuteNonQueryAsync();
        }

        [Fact]
        public async Task ExecuteTransactionalBatchAsync_WithFailure_RollsBackTransaction()
        {
            // Skip if no test database is configured
            if (!IsTestDatabaseConfigured())
            {
                _output.WriteLine("Skipping test - no test database configured");
                return;
            }

            using var connection = new LibSQLConnection(_connectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            
            // Setup
            command.CommandText = "DROP TABLE IF EXISTS trans_rollback_test";
            await command.ExecuteNonQueryAsync();
            command.CommandText = "CREATE TABLE trans_rollback_test (id INTEGER PRIMARY KEY, value INTEGER NOT NULL)";
            await command.ExecuteNonQueryAsync();

            // Execute transactional batch with a statement that will fail
            var statements = new[]
            {
                "INSERT INTO trans_rollback_test (value) VALUES (1)",
                "INSERT INTO trans_rollback_test (value) VALUES (2)",
                "INSERT INTO trans_rollback_test (value) VALUES (NULL)" // This will fail due to NOT NULL constraint
            };

            // Should throw an exception
            await Assert.ThrowsAsync<LibSQLException>(async () =>
            {
                await command.ExecuteTransactionalBatchAsync(statements);
            });
            
            // Verify no changes were committed (rollback occurred)
            command.CommandText = "SELECT COUNT(*) FROM trans_rollback_test";
            var count = Convert.ToInt32(await command.ExecuteScalarAsync());
            Assert.Equal(0, count);
            
            // Cleanup
            command.CommandText = "DROP TABLE trans_rollback_test";
            await command.ExecuteNonQueryAsync();
        }

        [Fact]
        public async Task ExecuteNonQuery_WithMultipleStatements_ExecutesWithSequence()
        {
            // Skip if no test database is configured
            if (!IsTestDatabaseConfigured())
            {
                _output.WriteLine("Skipping test - no test database configured");
                return;
            }

            using var connection = new LibSQLConnection(_connectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            
            // Set multiple statements separated by semicolons
            command.CommandText = @"
                DROP TABLE IF EXISTS multi_stmt_test;
                CREATE TABLE multi_stmt_test (id INTEGER PRIMARY KEY, name TEXT);
                INSERT INTO multi_stmt_test (name) VALUES ('Test1');
                INSERT INTO multi_stmt_test (name) VALUES ('Test2');
            ";

            // This should use the sequence request type internally
            var result = await command.ExecuteNonQueryAsync();
            _output.WriteLine($"ExecuteNonQuery result: {result}");
            
            // Verify the statements were executed
            command.CommandText = "SELECT COUNT(*) FROM multi_stmt_test";
            var count = Convert.ToInt32(await command.ExecuteScalarAsync());
            Assert.Equal(2, count);
            
            // Cleanup
            command.CommandText = "DROP TABLE multi_stmt_test";
            await command.ExecuteNonQueryAsync();
        }

        private bool IsTestDatabaseConfigured()
        {
            return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("LIBSQL_TEST_URL"));
        }

        public void Dispose()
        {
            // Cleanup if needed
        }
    }
}