using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Nelknet.LibSQL.Data;

/// <summary>
/// Provides bulk insert functionality for LibSQL databases.
/// </summary>
public class LibSQLBulkInsert : IDisposable
{
    private readonly LibSQLConnection _connection;
    private readonly string _tableName;
    private readonly List<string> _columnNames;
    private LibSQLCommand? _insertCommand;
    private LibSQLTransaction? _transaction;
    private bool _disposed;
    private int _batchSize = 1000;
    private int _currentBatchCount;

    /// <summary>
    /// Gets or sets the batch size for bulk inserts.
    /// </summary>
    public int BatchSize
    {
        get => _batchSize;
        set
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(nameof(value), "Batch size must be greater than zero.");
            _batchSize = value;
        }
    }

    /// <summary>
    /// Gets or sets the command timeout in seconds.
    /// </summary>
    public int CommandTimeout { get; set; } = 30;

    /// <summary>
    /// Gets or sets whether to use a transaction for the bulk insert.
    /// </summary>
    public bool UseTransaction { get; set; } = true;

    /// <summary>
    /// Initializes a new instance of the LibSQLBulkInsert class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="tableName">The name of the table to insert into.</param>
    /// <param name="columnNames">The names of the columns to insert.</param>
    public LibSQLBulkInsert(LibSQLConnection connection, string tableName, IEnumerable<string> columnNames)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(tableName);
        ArgumentNullException.ThrowIfNull(columnNames);
        
        _connection = connection;
        _tableName = tableName;
        _columnNames = new List<string>(columnNames);

        if (_columnNames.Count == 0)
            throw new ArgumentException("At least one column name must be specified.", nameof(columnNames));
    }

    /// <summary>
    /// Begins the bulk insert operation.
    /// </summary>
    public void BeginBulkInsert()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_connection.State != ConnectionState.Open)
            throw new InvalidOperationException("Connection must be open to begin bulk insert.");

        if (UseTransaction && _transaction == null)
        {
            _transaction = (LibSQLTransaction)_connection.BeginTransaction();
        }

        PrepareInsertCommand();
        _currentBatchCount = 0;
    }

    /// <summary>
    /// Begins the bulk insert operation asynchronously.
    /// </summary>
    public async Task BeginBulkInsertAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_connection.State != ConnectionState.Open)
            throw new InvalidOperationException("Connection must be open to begin bulk insert.");

        if (UseTransaction && _transaction == null)
        {
            _transaction = (LibSQLTransaction)await _connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        }

        PrepareInsertCommand();
        _currentBatchCount = 0;
    }

    /// <summary>
    /// Writes a row to the bulk insert operation.
    /// </summary>
    /// <param name="values">The values to insert.</param>
    public void WriteRow(params object?[] values)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_insertCommand == null)
            throw new InvalidOperationException("BeginBulkInsert must be called before WriteRow.");

        if (values.Length != _columnNames.Count)
            throw new ArgumentException($"Expected {_columnNames.Count} values but got {values.Length}.", nameof(values));

        // Set parameter values
        for (int i = 0; i < values.Length; i++)
        {
            _insertCommand.Parameters[i].Value = values[i] ?? DBNull.Value;
        }

        _insertCommand.ExecuteNonQuery();
        _currentBatchCount++;

        // Commit batch if needed
        if (_currentBatchCount >= _batchSize && UseTransaction)
        {
            CommitBatch();
        }
    }

    /// <summary>
    /// Writes a row to the bulk insert operation asynchronously.
    /// </summary>
    /// <param name="values">The values to insert.</param>
    public async Task WriteRowAsync(params object?[] values)
    {
        await WriteRowAsync(CancellationToken.None, values).ConfigureAwait(false);
    }

    /// <summary>
    /// Writes a row to the bulk insert operation asynchronously.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="values">The values to insert.</param>
    public async Task WriteRowAsync(CancellationToken cancellationToken, params object?[] values)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_insertCommand == null)
            throw new InvalidOperationException("BeginBulkInsert must be called before WriteRow.");

        if (values.Length != _columnNames.Count)
            throw new ArgumentException($"Expected {_columnNames.Count} values but got {values.Length}.", nameof(values));

        // Set parameter values
        for (int i = 0; i < values.Length; i++)
        {
            _insertCommand.Parameters[i].Value = values[i] ?? DBNull.Value;
        }

        await _insertCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        _currentBatchCount++;

        // Commit batch if needed
        if (_currentBatchCount >= _batchSize && UseTransaction)
        {
            await CommitBatchAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Writes multiple rows to the bulk insert operation.
    /// </summary>
    /// <param name="rows">The rows to insert.</param>
    public void WriteRows(IEnumerable<object?[]> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);

        foreach (var row in rows)
        {
            WriteRow(row);
        }
    }

    /// <summary>
    /// Writes multiple rows to the bulk insert operation asynchronously.
    /// </summary>
    /// <param name="rows">The rows to insert.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task WriteRowsAsync(IEnumerable<object?[]> rows, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rows);

        foreach (var row in rows)
        {
            await WriteRowAsync(cancellationToken, row).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Writes rows from a data reader.
    /// </summary>
    /// <param name="reader">The data reader to read from.</param>
    public void WriteFromReader(DbDataReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        var values = new object[_columnNames.Count];
        
        while (reader.Read())
        {
            reader.GetValues(values);
            WriteRow(values);
        }
    }

    /// <summary>
    /// Writes rows from a data reader asynchronously.
    /// </summary>
    /// <param name="reader">The data reader to read from.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task WriteFromReaderAsync(DbDataReader reader, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reader);

        var values = new object[_columnNames.Count];
        
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            reader.GetValues(values);
            await WriteRowAsync(cancellationToken, values).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Completes the bulk insert operation.
    /// </summary>
    public void Complete()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_transaction != null)
        {
            _transaction.Commit();
            _transaction.Dispose();
            _transaction = null;
        }

        Cleanup();
    }

    /// <summary>
    /// Completes the bulk insert operation asynchronously.
    /// </summary>
    public async Task CompleteAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_transaction != null)
        {
            await _transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            await _transaction.DisposeAsync().ConfigureAwait(false);
            _transaction = null;
        }

        Cleanup();
    }

    /// <summary>
    /// Aborts the bulk insert operation.
    /// </summary>
    public void Abort()
    {
        if (_disposed)
            return;

        if (_transaction != null)
        {
            _transaction.Rollback();
            _transaction.Dispose();
            _transaction = null;
        }

        Cleanup();
    }

    /// <summary>
    /// Aborts the bulk insert operation asynchronously.
    /// </summary>
    public async Task AbortAsync()
    {
        if (_disposed)
            return;

        if (_transaction != null)
        {
            await _transaction.RollbackAsync().ConfigureAwait(false);
            await _transaction.DisposeAsync().ConfigureAwait(false);
            _transaction = null;
        }

        Cleanup();
    }

    private void PrepareInsertCommand()
    {
        var columnList = string.Join(", ", _columnNames);
        var parameterList = string.Join(", ", GetParameterNames());

        var sql = $"INSERT INTO {_tableName} ({columnList}) VALUES ({parameterList})";

        _insertCommand = _connection.CreateCommand();
        _insertCommand.CommandText = sql;
        _insertCommand.CommandTimeout = CommandTimeout;
        _insertCommand.Transaction = _transaction;
        // Don't prepare the statement - there seems to be an issue with prepared statements
        // _insertCommand.Prepare();

        // Add parameters - using positional parameters
        for (int i = 0; i < _columnNames.Count; i++)
        {
            var parameter = _insertCommand.CreateParameter();
            parameter.ParameterName = $"?{i + 1}";
            _insertCommand.Parameters.Add(parameter);
        }
    }

    private IEnumerable<string> GetParameterNames()
    {
        for (int i = 0; i < _columnNames.Count; i++)
        {
            yield return "?";
        }
    }

    private void CommitBatch()
    {
        if (_transaction != null)
        {
            _transaction.Commit();
            _transaction.Dispose();
            _transaction = (LibSQLTransaction)_connection.BeginTransaction();
            
            if (_insertCommand != null)
            {
                _insertCommand.Transaction = _transaction;
            }
        }
        _currentBatchCount = 0;
    }

    private async Task CommitBatchAsync(CancellationToken cancellationToken)
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            await _transaction.DisposeAsync().ConfigureAwait(false);
            _transaction = (LibSQLTransaction)await _connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            
            if (_insertCommand != null)
            {
                _insertCommand.Transaction = _transaction;
            }
        }
        _currentBatchCount = 0;
    }

    private void Cleanup()
    {
        _insertCommand?.Dispose();
        _insertCommand = null;
        _currentBatchCount = 0;
    }

    /// <summary>
    /// Disposes the bulk insert operation.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the bulk insert operation.
    /// </summary>
    /// <param name="disposing">True if disposing managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            Abort();
        }

        _disposed = true;
    }

    /// <summary>
    /// Creates a bulk insert operation from a DataTable.
    /// </summary>
    public static void BulkInsertDataTable(LibSQLConnection connection, DataTable dataTable, string? tableName = null)
    {
        ArgumentNullException.ThrowIfNull(dataTable);

        tableName ??= dataTable.TableName;
        if (string.IsNullOrEmpty(tableName))
            throw new ArgumentException("Table name must be specified.", nameof(tableName));

        var columnNames = new List<string>();
        foreach (DataColumn column in dataTable.Columns)
        {
            columnNames.Add(column.ColumnName);
        }

        using var bulkInsert = new LibSQLBulkInsert(connection, tableName, columnNames);
        bulkInsert.BeginBulkInsert();

        foreach (DataRow row in dataTable.Rows)
        {
            bulkInsert.WriteRow(row.ItemArray);
        }

        bulkInsert.Complete();
    }

    /// <summary>
    /// Creates a bulk insert operation from a DataTable asynchronously.
    /// </summary>
    public static async Task BulkInsertDataTableAsync(LibSQLConnection connection, DataTable dataTable, string? tableName = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dataTable);

        tableName ??= dataTable.TableName;
        if (string.IsNullOrEmpty(tableName))
            throw new ArgumentException("Table name must be specified.", nameof(tableName));

        var columnNames = new List<string>();
        foreach (DataColumn column in dataTable.Columns)
        {
            columnNames.Add(column.ColumnName);
        }

        using var bulkInsert = new LibSQLBulkInsert(connection, tableName, columnNames);
        await bulkInsert.BeginBulkInsertAsync(cancellationToken).ConfigureAwait(false);

        foreach (DataRow row in dataTable.Rows)
        {
            await bulkInsert.WriteRowAsync(cancellationToken, row.ItemArray).ConfigureAwait(false);
        }

        await bulkInsert.CompleteAsync(cancellationToken).ConfigureAwait(false);
    }
}