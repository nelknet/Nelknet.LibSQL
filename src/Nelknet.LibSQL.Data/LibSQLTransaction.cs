#nullable disable warnings

using System;
using System.Data;
using System.Data.Common;
using Nelknet.LibSQL.Bindings;
using Nelknet.LibSQL.Data.Exceptions;

namespace Nelknet.LibSQL.Data;

/// <summary>
/// Represents a transaction to be performed at a libSQL database.
/// </summary>
public sealed class LibSQLTransaction : DbTransaction
{
    private LibSQLConnection? _connection;
    private IsolationLevel _isolationLevel;
    private LibSQLTransactionBehavior _behavior;
    private bool _completed;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="LibSQLTransaction"/> class.
    /// </summary>
    /// <param name="connection">The connection associated with the transaction.</param>
    /// <param name="isolationLevel">The isolation level for the transaction.</param>
    /// <param name="behavior">The transaction behavior for lock acquisition.</param>
    internal LibSQLTransaction(LibSQLConnection connection, IsolationLevel isolationLevel, LibSQLTransactionBehavior behavior = LibSQLTransactionBehavior.Deferred)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _isolationLevel = isolationLevel == IsolationLevel.Unspecified ? IsolationLevel.Serializable : isolationLevel;
        _behavior = behavior;
        _completed = false;
        _disposed = false;
    }

    /// <summary>
    /// Gets the <see cref="LibSQLConnection"/> object associated with the transaction.
    /// </summary>
    public new LibSQLConnection? Connection => _connection;

    /// <summary>
    /// Gets the <see cref="DbConnection"/> object associated with the transaction.
    /// </summary>
    protected override DbConnection? DbConnection => _connection;

    /// <summary>
    /// Gets the isolation level for this transaction.
    /// </summary>
    public override IsolationLevel IsolationLevel => _isolationLevel;

    /// <summary>
    /// Gets the transaction behavior for this transaction.
    /// </summary>
    public LibSQLTransactionBehavior Behavior => _behavior;

    /// <summary>
    /// Commits the database transaction.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the transaction has already been completed or the connection is invalid.</exception>
    /// <exception cref="LibSQLException">Thrown when the commit operation fails.</exception>
    public override void Commit()
    {
        ValidateTransaction();

        try
        {
            if (_connection.IsHttpConnection)
            {
                // For HTTP connections, use SQL commands
                using var command = _connection.CreateCommand();
                command.CommandText = "COMMIT";
                command.ExecuteNonQuery();
            }
            else
            {
                // For native connections, use libSQL API
                var result = LibSQLNative.libsql_execute(_connection!.Handle, "COMMIT", out var errorMessage);
                if (result != 0)
                {
                    var errorMsg = LibSQLHelper.GetErrorMessage(errorMessage);
                    LibSQLNative.libsql_free_error_msg(errorMessage);
                    throw new LibSQLException($"Failed to commit transaction: {errorMsg}");
                }
            }

            _completed = true;
            _connection._currentTransaction = null;
        }
        catch (Exception ex) when (!(ex is LibSQLException))
        {
            throw new LibSQLException("An error occurred while committing the transaction.", ex);
        }
    }

    /// <summary>
    /// Rolls back the database transaction.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the transaction has already been completed or the connection is invalid.</exception>
    /// <exception cref="LibSQLException">Thrown when the rollback operation fails.</exception>
    public override void Rollback()
    {
        ValidateTransaction();

        try
        {
            if (_connection.IsHttpConnection)
            {
                // For HTTP connections, use SQL commands
                using var command = _connection.CreateCommand();
                command.CommandText = "ROLLBACK";
                command.ExecuteNonQuery();
            }
            else
            {
                // For native connections, use libSQL API
                var result = LibSQLNative.libsql_execute(_connection!.Handle, "ROLLBACK", out var errorMessage);
                if (result != 0)
                {
                    var errorMsg = LibSQLHelper.GetErrorMessage(errorMessage);
                    LibSQLNative.libsql_free_error_msg(errorMessage);
                    throw new LibSQLException($"Failed to rollback transaction: {errorMsg}");
                }
            }

            _completed = true;
            _connection._currentTransaction = null;
        }
        catch (Exception ex) when (!(ex is LibSQLException))
        {
            throw new LibSQLException("An error occurred while rolling back the transaction.", ex);
        }
    }

    /// <summary>
    /// Validates that the transaction is in a valid state for operations.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the transaction has been completed or disposed, or the connection is invalid.</exception>
    public void ValidateTransaction()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_completed)
        {
            throw new InvalidOperationException("The transaction has already been completed.");
        }

        if (_connection == null)
        {
            throw new InvalidOperationException("The transaction is not associated with a connection.");
        }

        if (_connection.State != ConnectionState.Open)
        {
            throw new InvalidOperationException("The connection is not open.");
        }

        if (_connection._currentTransaction != this)
        {
            throw new InvalidOperationException("This transaction is not the current transaction for the connection.");
        }
    }

    /// <summary>
    /// Gets a value indicating whether the transaction has been completed (committed or rolled back).
    /// </summary>
    public bool IsCompleted => _completed;


    /// <summary>
    /// Generates the appropriate BEGIN SQL statement based on isolation level and behavior.
    /// </summary>
    /// <returns>The SQL statement to begin the transaction.</returns>
    internal string GetBeginStatement()
    {
        var behaviorSql = _behavior switch
        {
            LibSQLTransactionBehavior.Deferred => "BEGIN DEFERRED",
            LibSQLTransactionBehavior.Immediate => "BEGIN IMMEDIATE",
            LibSQLTransactionBehavior.Exclusive => "BEGIN EXCLUSIVE",
            LibSQLTransactionBehavior.ReadOnly => "BEGIN READONLY",
            _ => "BEGIN"
        };

        // Note: libSQL isolation is always Serializable by default
        // ReadUncommitted requires pragma read_uncommitted=1 which should be set at connection level
        return behaviorSql;
    }

    /// <summary>
    /// Releases the unmanaged resources used by the <see cref="LibSQLTransaction"/> and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected override void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing && !_completed && _connection != null)
        {
            try
            {
                // Auto-rollback on disposal if not completed
                Rollback();
            }
            catch
            {
                // Suppress exceptions during disposal
                // The transaction will be considered completed to prevent further operations
                _completed = true;
                if (_connection != null)
                {
                    _connection._currentTransaction = null;
                }
            }
        }

        _connection = null;
        _disposed = true;

        base.Dispose(disposing);
    }
}