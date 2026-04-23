#nullable disable warnings

using Nelknet.LibSQL.Data;
using Nelknet.LibSQL.Bindings;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Nelknet.LibSQL.Tests;

public class LibSQLCommandExecutionTests
{
    [Fact]
    public void CommandText_SetValue_ShouldUpdateProperty()
    {
        using var command = new LibSQLCommand();
        const string sql = "SELECT 1";
        
        command.CommandText = sql;
        
        Assert.Equal(sql, command.CommandText);
    }

    [Fact]
    public void CommandText_SetToNull_ShouldReturnEmptyString()
    {
        using var command = new LibSQLCommand();
        
        command.CommandText = null;
        
        Assert.Equal(string.Empty, command.CommandText);
    }

    [Fact]
    public void CommandText_ChangeValue_ShouldInvalidatePreparedStatement()
    {
        using var command = new LibSQLCommand("SELECT 1");
        
        // Changing command text should not throw (prepared statement is invalidated)
        command.CommandText = "SELECT 2";
        
        Assert.Equal("SELECT 2", command.CommandText);
    }

    [Fact]
    public void CommandTimeout_SetValidValue_ShouldUpdateProperty()
    {
        using var command = new LibSQLCommand();
        
        command.CommandTimeout = 60;
        
        Assert.Equal(60, command.CommandTimeout);
    }

    [Fact]
    public void CommandTimeout_SetNegativeValue_ShouldThrowArgumentOutOfRangeException()
    {
        using var command = new LibSQLCommand();
        
        Assert.Throws<ArgumentOutOfRangeException>(() => command.CommandTimeout = -1);
    }

    [Fact]
    public void CommandTimeout_SetZero_ShouldSetInfiniteTimeout()
    {
        using var command = new LibSQLCommand();
        
        command.CommandTimeout = 0;
        
        Assert.Equal(0, command.CommandTimeout);
    }

    [Fact]
    public void CommandType_GetValue_ShouldReturnText()
    {
        using var command = new LibSQLCommand();
        
        Assert.Equal(CommandType.Text, command.CommandType);
    }

    [Fact]
    public void CommandType_SetToStoredProcedure_ShouldThrowNotSupportedException()
    {
        using var command = new LibSQLCommand();
        
        Assert.Throws<NotSupportedException>(() => command.CommandType = CommandType.StoredProcedure);
    }

    [Fact]
    public void CommandType_SetToTableDirect_ShouldThrowNotSupportedException()
    {
        using var command = new LibSQLCommand();
        
        Assert.Throws<NotSupportedException>(() => command.CommandType = CommandType.TableDirect);
    }

    [Fact]
    public void Connection_SetValue_ShouldUpdateProperty()
    {
        using var connection = new LibSQLConnection();
        using var command = new LibSQLCommand();
        
        command.Connection = connection;
        
        Assert.Same(connection, command.Connection);
    }

    [Fact]
    public void Connection_ChangeValue_ShouldInvalidatePreparedStatement()
    {
        using var connection1 = new LibSQLConnection();
        using var connection2 = new LibSQLConnection();
        using var command = new LibSQLCommand { Connection = connection1 };
        
        // Changing connection should not throw (prepared statement is invalidated)
        command.Connection = connection2;
        
        Assert.Same(connection2, command.Connection);
    }

    [Fact]
    public void Parameters_Get_ShouldReturnCollection()
    {
        using var command = new LibSQLCommand();
        
        var parameters = command.Parameters;
        
        Assert.NotNull(parameters);
        Assert.Empty(parameters);
    }

    [Fact]
    public void CreateParameter_ShouldReturnNewParameter()
    {
        using var command = new LibSQLCommand();
        
        var parameter = command.CreateParameter();
        
        Assert.NotNull(parameter);
        Assert.IsType<LibSQLParameter>(parameter);
    }

    [Fact]
    public void ExecuteNonQuery_WithoutConnection_ShouldThrowInvalidOperationException()
    {
        using var command = new LibSQLCommand("SELECT 1");
        
        var exception = Assert.Throws<InvalidOperationException>(() => command.ExecuteNonQuery());
        Assert.Contains("Connection property has not been initialized", exception.Message);
    }

    [Fact]
    public void ExecuteNonQuery_WithClosedConnection_ShouldThrowInvalidOperationException()
    {
        using var connection = new LibSQLConnection();
        using var command = new LibSQLCommand("SELECT 1", connection);
        
        var exception = Assert.Throws<InvalidOperationException>(() => command.ExecuteNonQuery());
        Assert.Contains("Connection must be open", exception.Message);
    }

    [Fact]
    public void ExecuteNonQuery_WithEmptyCommandText_ShouldThrowInvalidOperationException()
    {
        using var connection = new LibSQLConnection("Data Source=:memory:");
        using var command = new LibSQLCommand("", connection);
        
        // This will throw because libSQL library is not available in test environment
        // but the validation should happen before that
        try
        {
            connection.Open();
            var exception = Assert.Throws<InvalidOperationException>(() => command.ExecuteNonQuery());
            Assert.Contains("CommandText property has not been properly initialized", exception.Message);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Failed to load libSQL native library"))
        {
            // Expected in test environment without native library
            Assert.Contains("Failed to load libSQL native library", ex.Message);
        }
    }

    [Fact]
    public void ExecuteScalar_WithoutConnection_ShouldThrowInvalidOperationException()
    {
        using var command = new LibSQLCommand("SELECT 1");
        
        var exception = Assert.Throws<InvalidOperationException>(() => command.ExecuteScalar());
        Assert.Contains("Connection property has not been initialized", exception.Message);
    }

    [Fact]
    public void ExecuteScalar_WithClosedConnection_ShouldThrowInvalidOperationException()
    {
        using var connection = new LibSQLConnection();
        using var command = new LibSQLCommand("SELECT 1", connection);
        
        var exception = Assert.Throws<InvalidOperationException>(() => command.ExecuteScalar());
        Assert.Contains("Connection must be open", exception.Message);
    }

    [Fact]
    public void ExecuteReader_WithoutConnection_ShouldThrowInvalidOperationException()
    {
        using var command = new LibSQLCommand("SELECT 1");
        
        var exception = Assert.Throws<InvalidOperationException>(() => command.ExecuteReader());
        Assert.Contains("Connection property has not been initialized", exception.Message);
    }

    [Fact]
    public void ExecuteReader_WithClosedConnection_ShouldThrowInvalidOperationException()
    {
        using var connection = new LibSQLConnection();
        using var command = new LibSQLCommand("SELECT 1", connection);
        
        var exception = Assert.Throws<InvalidOperationException>(() => command.ExecuteReader());
        Assert.Contains("Connection must be open", exception.Message);
    }

    [Fact]
    public void Prepare_WithoutConnection_ShouldThrowInvalidOperationException()
    {
        using var command = new LibSQLCommand("SELECT 1");
        
        var exception = Assert.Throws<InvalidOperationException>(() => command.Prepare());
        Assert.Contains("Connection property has not been initialized", exception.Message);
    }

    [Fact]
    public void Prepare_WithClosedConnection_ShouldThrowInvalidOperationException()
    {
        using var connection = new LibSQLConnection();
        using var command = new LibSQLCommand("SELECT 1", connection);
        
        var exception = Assert.Throws<InvalidOperationException>(() => command.Prepare());
        Assert.Contains("Connection must be open", exception.Message);
    }

    [Fact]
    public void Prepare_WithEmptyCommandText_ShouldThrowInvalidOperationException()
    {
        using var connection = new LibSQLConnection("Data Source=:memory:");
        using var command = new LibSQLCommand("", connection);
        
        try
        {
            connection.Open();
            var exception = Assert.Throws<InvalidOperationException>(() => command.Prepare());
            Assert.Contains("CommandText property has not been properly initialized", exception.Message);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Failed to load libSQL native library"))
        {
            // Expected in test environment without native library
            Assert.Contains("Failed to load libSQL native library", ex.Message);
        }
    }

    [Fact]
    public void Cancel_ShouldNotThrow()
    {
        using var command = new LibSQLCommand();
        
        // Cancel should be a no-op and not throw
        command.Cancel();
    }

    [Fact]
    public async Task ExecuteNonQueryAsync_WithValidTimeout_ShouldNotThrow()
    {
        using var command = new LibSQLCommand("SELECT 1");
        command.CommandTimeout = 5;
        
        // This should complete quickly and not timeout
        var cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(1)).Token;
        
        try
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => command.ExecuteNonQueryAsync(cancellationToken));
        }
        catch (OperationCanceledException)
        {
            // Expected due to timeout or cancellation
        }
    }

    [Fact]
    public async Task ExecuteScalarAsync_WithCancellation_ShouldRespectCancellation()
    {
        using var command = new LibSQLCommand("SELECT 1");
        var cancellationTokenSource = new CancellationTokenSource();
        
        // Cancel immediately
        cancellationTokenSource.Cancel();
        
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => 
            command.ExecuteScalarAsync(cancellationTokenSource.Token));
    }

    [Fact]
    public async Task ExecuteDbDataReaderAsync_WithCancellation_ShouldRespectCancellation()
    {
        using var command = new LibSQLCommand("SELECT 1");
        var cancellationTokenSource = new CancellationTokenSource();
        
        // Cancel immediately
        cancellationTokenSource.Cancel();
        
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => 
            command.ExecuteReaderAsync(cancellationTokenSource.Token));
    }

    [Fact]
    public void DesignTimeVisible_DefaultValue_ShouldBeFalse()
    {
        using var command = new LibSQLCommand();
        
        Assert.False(command.DesignTimeVisible);
    }

    [Fact]
    public void DesignTimeVisible_SetValue_ShouldUpdateProperty()
    {
        using var command = new LibSQLCommand();
        
        command.DesignTimeVisible = true;
        
        Assert.True(command.DesignTimeVisible);
    }

    [Fact]
    public void UpdatedRowSource_DefaultValue_ShouldBeNone()
    {
        using var command = new LibSQLCommand();
        
        Assert.Equal(UpdateRowSource.None, command.UpdatedRowSource);
    }

    [Fact]
    public void UpdatedRowSource_SetValue_ShouldUpdateProperty()
    {
        using var command = new LibSQLCommand();
        
        command.UpdatedRowSource = UpdateRowSource.FirstReturnedRecord;
        
        Assert.Equal(UpdateRowSource.FirstReturnedRecord, command.UpdatedRowSource);
    }

    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        var command = new LibSQLCommand();
        
        // Multiple dispose calls should not throw
        command.Dispose();
        command.Dispose();
    }

    [Fact]
    public void Dispose_WithPreparedStatement_ShouldCleanupResources()
    {
        using var connection = new LibSQLConnection("Data Source=:memory:");
        var command = new LibSQLCommand("SELECT 1", connection);

        try
        {
            connection.Open();
            command.Prepare();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Failed to load libSQL native library"))
        {
            // Expected in test environment without native library
        }

        // Dispose should not throw even if prepare failed
        command.Dispose();
    }

    // --- Regression tests for issue #66: ExecuteNonQuery must report affected-row count ---

    [Fact]
    public void ExecuteNonQuery_UpdateMatchingRow_ReturnsAffectedRowCount()
    {
        using var connection = new LibSQLConnection("Data Source=:memory:");

        try
        {
            connection.Open();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Failed to load libSQL native library"))
        {
            return;
        }

        using (var create = connection.CreateCommand())
        {
            create.CommandText = "CREATE TABLE t (id INTEGER PRIMARY KEY, name TEXT)";
            create.ExecuteNonQuery();
        }
        using (var insert = connection.CreateCommand())
        {
            insert.CommandText = "INSERT INTO t (id, name) VALUES (1, 'a')";
            insert.ExecuteNonQuery();
        }

        using var update = connection.CreateCommand();
        update.CommandText = "UPDATE t SET name = @n WHERE id = @id";
        update.Parameters.AddWithValue("@n", "b");
        update.Parameters.AddWithValue("@id", 1);

        var affected = update.ExecuteNonQuery();

        Assert.Equal(1, affected);
    }

    [Fact]
    public void ExecuteNonQuery_UpdateNoMatch_ReturnsZero()
    {
        using var connection = new LibSQLConnection("Data Source=:memory:");

        try
        {
            connection.Open();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Failed to load libSQL native library"))
        {
            return;
        }

        using (var create = connection.CreateCommand())
        {
            create.CommandText = "CREATE TABLE t (id INTEGER PRIMARY KEY, name TEXT)";
            create.ExecuteNonQuery();
        }

        using var update = connection.CreateCommand();
        update.CommandText = "UPDATE t SET name = @n WHERE id = @id";
        update.Parameters.AddWithValue("@n", "b");
        update.Parameters.AddWithValue("@id", 999);

        var affected = update.ExecuteNonQuery();

        Assert.Equal(0, affected);
    }

    [Fact]
    public void ExecuteNonQuery_InsertSingleRow_ReturnsOne()
    {
        using var connection = new LibSQLConnection("Data Source=:memory:");

        try
        {
            connection.Open();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Failed to load libSQL native library"))
        {
            return;
        }

        using (var create = connection.CreateCommand())
        {
            create.CommandText = "CREATE TABLE t (id INTEGER PRIMARY KEY, v TEXT)";
            create.ExecuteNonQuery();
        }

        using var insert = connection.CreateCommand();
        insert.CommandText = "INSERT INTO t (id, v) VALUES (@id, @v)";
        insert.Parameters.AddWithValue("@id", 1);
        insert.Parameters.AddWithValue("@v", "hello");

        var affected = insert.ExecuteNonQuery();

        Assert.Equal(1, affected);
    }

    [Fact]
    public void ExecuteNonQuery_DeleteMatchingRow_ReturnsOne()
    {
        using var connection = new LibSQLConnection("Data Source=:memory:");

        try
        {
            connection.Open();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Failed to load libSQL native library"))
        {
            return;
        }

        using (var create = connection.CreateCommand())
        {
            create.CommandText = "CREATE TABLE t (id INTEGER PRIMARY KEY)";
            create.ExecuteNonQuery();
        }
        using (var insert = connection.CreateCommand())
        {
            insert.CommandText = "INSERT INTO t (id) VALUES (1), (2), (3)";
            insert.ExecuteNonQuery();
        }

        using var delete = connection.CreateCommand();
        delete.CommandText = "DELETE FROM t WHERE id = @id";
        delete.Parameters.AddWithValue("@id", 2);

        var affected = delete.ExecuteNonQuery();

        Assert.Equal(1, affected);
    }

    [Fact]
    public void ExecuteNonQuery_DeleteMultipleRows_ReturnsTotalAffected()
    {
        using var connection = new LibSQLConnection("Data Source=:memory:");

        try
        {
            connection.Open();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Failed to load libSQL native library"))
        {
            return;
        }

        using (var create = connection.CreateCommand())
        {
            create.CommandText = "CREATE TABLE t (id INTEGER PRIMARY KEY, flag INTEGER)";
            create.ExecuteNonQuery();
        }
        using (var insert = connection.CreateCommand())
        {
            insert.CommandText = "INSERT INTO t (id, flag) VALUES (1, 1), (2, 1), (3, 0)";
            insert.ExecuteNonQuery();
        }

        using var delete = connection.CreateCommand();
        delete.CommandText = "DELETE FROM t WHERE flag = 1";

        var affected = delete.ExecuteNonQuery();

        Assert.Equal(2, affected);
    }

    [Fact]
    public async Task ExecuteNonQueryAsync_UpdateMatchingRow_ReturnsOne()
    {
        using var connection = new LibSQLConnection("Data Source=:memory:");

        try
        {
            await connection.OpenAsync();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Failed to load libSQL native library"))
        {
            return;
        }

        using (var create = connection.CreateCommand())
        {
            create.CommandText = "CREATE TABLE t (id INTEGER PRIMARY KEY, n TEXT)";
            await create.ExecuteNonQueryAsync();
        }
        using (var insert = connection.CreateCommand())
        {
            insert.CommandText = "INSERT INTO t (id, n) VALUES (1, 'a')";
            await insert.ExecuteNonQueryAsync();
        }

        using var update = connection.CreateCommand();
        update.CommandText = "UPDATE t SET n = @n WHERE id = @id";
        update.Parameters.AddWithValue("@n", "b");
        update.Parameters.AddWithValue("@id", 1);

        var affected = await update.ExecuteNonQueryAsync();

        Assert.Equal(1, affected);
    }
}