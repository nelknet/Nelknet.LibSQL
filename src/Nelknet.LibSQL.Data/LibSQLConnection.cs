#nullable disable warnings

using System;
using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;
using Nelknet.LibSQL.Bindings;
using Nelknet.LibSQL.Data.Exceptions;

namespace Nelknet.LibSQL.Data;

/// <summary>
/// Represents a connection to a libSQL database.
/// </summary>
public sealed class LibSQLConnection : DbConnection
{
    private readonly object _lockObject = new();
    private LibSQLDatabaseHandle? _databaseHandle;
    private LibSQLConnectionHandle? _connectionHandle;
    private ConnectionState _connectionState = ConnectionState.Closed;
    private string _connectionString = string.Empty;
    private LibSQLConnectionStringBuilder? _connectionStringBuilder;
    internal LibSQLTransaction? _currentTransaction;

    // Connection state change event args for performance
    private static readonly StateChangeEventArgs FromClosedToOpenEventArgs = 
        new(ConnectionState.Closed, ConnectionState.Open);
    private static readonly StateChangeEventArgs FromOpenToClosedEventArgs = 
        new(ConnectionState.Open, ConnectionState.Closed);

    /// <summary>
    /// Initializes a new instance of the <see cref="LibSQLConnection"/> class.
    /// </summary>
    public LibSQLConnection()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LibSQLConnection"/> class with the specified connection string.
    /// </summary>
    /// <param name="connectionString">The connection string used to open the database.</param>
    public LibSQLConnection(string connectionString) : this()
    {
        ConnectionString = connectionString;
    }

    /// <summary>
    /// Gets or sets the string used to open the database.
    /// </summary>
    public override string ConnectionString
    {
        get => _connectionString;
        set
        {
            EnsureConnectionClosed();
            _connectionString = value ?? string.Empty;
            _connectionStringBuilder = null; // Reset parsed connection string
        }
    }

    /// <summary>
    /// Changes the current database for an open connection.
    /// </summary>
    /// <param name="databaseName">The database name.</param>
    public override void ChangeDatabase(string databaseName)
    {
        throw new NotSupportedException("libSQL does not support changing databases on an open connection.");
    }

    /// <summary>
    /// Gets the name of the database after a connection is opened.
    /// </summary>
    public override string Database => ConnectionStringBuilder.DataSource ?? string.Empty;

    /// <summary>
    /// Gets the name of the database server.
    /// </summary>
    public override string DataSource => ConnectionStringBuilder.DataSource ?? string.Empty;

    /// <summary>
    /// Gets the version of the libSQL server.
    /// </summary>
    public override string ServerVersion => "libSQL"; // TODO: Get actual version from native library

    /// <summary>
    /// Gets the current state of the connection.
    /// </summary>
    public override ConnectionState State => _connectionState;

    /// <summary>
    /// Gets the connection string builder.
    /// </summary>
    private LibSQLConnectionStringBuilder ConnectionStringBuilder
    {
        get
        {
            if (_connectionStringBuilder is null)
            {
                _connectionStringBuilder = new LibSQLConnectionStringBuilder(_connectionString);
            }
            return _connectionStringBuilder;
        }
    }

    /// <summary>
    /// Opens a database connection with the settings specified by the <see cref="ConnectionString"/>.
    /// </summary>
    public override void Open()
    {
        lock (_lockObject)
        {
            EnsureConnectionClosed();

            try
            {
                // Initialize the native library if not already done
                LibSQLNative.Initialize();

                var builder = ConnectionStringBuilder;
                IntPtr dbHandle;
                IntPtr errorMsg;
                
                // Open the database
                int result;
                var dataSource = builder.DataSource ?? throw new InvalidOperationException("Data source is required.");
                
                switch (builder.Mode)
                {
                    case LibSQLConnectionMode.Remote:
                        if (string.IsNullOrEmpty(builder.AuthToken))
                        {
                            throw new InvalidOperationException("Auth token is required for remote connections.");
                        }
                        
                        // For now, always use WebPKI for remote connections
                        result = LibSQLNative.libsql_open_remote_with_webpki(
                            dataSource, builder.AuthToken, out dbHandle, out errorMsg);
                        break;
                        
                    case LibSQLConnectionMode.EmbeddedReplica:
                        // For embedded replica, we need to use the sync configuration
                        // This will be implemented when we add sync support
                        throw new NotSupportedException("Embedded replica mode is not yet supported.");
                        
                    case LibSQLConnectionMode.Local:
                    default:
                        if (dataSource == ":memory:" || dataSource.StartsWith(":memory:?"))
                        {
                            result = LibSQLNative.libsql_open_ext(dataSource, out dbHandle, out errorMsg);
                        }
                        else
                        {
                            result = LibSQLNative.libsql_open_file(dataSource, out dbHandle, out errorMsg);
                        }
                        break;
                }

                if (result != 0)
                {
                    var errorMessage = LibSQLHelper.GetErrorMessage(errorMsg);
                    LibSQLNative.libsql_free_error_msg(errorMsg);
                    throw new LibSQLConnectionException($"Failed to open database: {errorMessage}", result, dataSource);
                }

                _databaseHandle = new LibSQLDatabaseHandle(dbHandle);

                // Connect to the database
                IntPtr connHandle;
                result = LibSQLNative.libsql_connect(_databaseHandle, out connHandle, out errorMsg);
                if (result != 0)
                {
                    var errorMessage = LibSQLHelper.GetErrorMessage(errorMsg);
                    LibSQLNative.libsql_free_error_msg(errorMsg);
                    _databaseHandle.Dispose();
                    _databaseHandle = null;
                    throw new LibSQLConnectionException($"Failed to connect to database: {errorMessage}", result, dataSource);
                }

                _connectionHandle = new LibSQLConnectionHandle(connHandle);
                _connectionState = ConnectionState.Open;

                OnStateChange(FromClosedToOpenEventArgs);
            }
            catch
            {
                // Cleanup on failure
                _connectionHandle?.Dispose();
                _connectionHandle = null;
                _databaseHandle?.Dispose();
                _databaseHandle = null;
                throw;
            }
        }
    }

    /// <summary>
    /// Closes the connection to the database.
    /// </summary>
    public override void Close()
    {
        lock (_lockObject)
        {
            if (_connectionState == ConnectionState.Closed)
                return;

            try
            {
                // Rollback any active transaction
                if (_currentTransaction != null && !_currentTransaction.IsCompleted)
                {
                    try
                    {
                        _currentTransaction.Rollback();
                    }
                    catch
                    {
                        // Suppress exceptions during cleanup
                    }
                }
                _currentTransaction = null;

                _connectionHandle?.Dispose();
                _connectionHandle = null;

                _databaseHandle?.Dispose();
                _databaseHandle = null;

                _connectionState = ConnectionState.Closed;
                OnStateChange(FromOpenToClosedEventArgs);
            }
            catch
            {
                // Always ensure we're marked as closed, even if cleanup fails
                _connectionState = ConnectionState.Closed;
                throw;
            }
        }
    }

    /// <summary>
    /// Creates and returns a <see cref="DbCommand"/> object associated with this connection.
    /// </summary>
    /// <returns>A <see cref="DbCommand"/> object.</returns>
    protected override DbCommand CreateDbCommand()
    {
        return CreateCommand();
    }

    /// <summary>
    /// Creates and returns a <see cref="LibSQLCommand"/> object associated with this connection.
    /// </summary>
    /// <returns>A <see cref="LibSQLCommand"/> object.</returns>
    public new LibSQLCommand CreateCommand()
    {
        return new LibSQLCommand
        {
            Connection = this
        };
    }

    /// <summary>
    /// Starts a database transaction.
    /// </summary>
    /// <param name="isolationLevel">The isolation level for the transaction.</param>
    /// <returns>A <see cref="DbTransaction"/> representing the new transaction.</returns>
    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
    {
        return BeginTransaction(isolationLevel);
    }

    /// <summary>
    /// Starts a database transaction with the specified isolation level.
    /// </summary>
    /// <param name="isolationLevel">The isolation level for the transaction.</param>
    /// <returns>A <see cref="LibSQLTransaction"/> representing the new transaction.</returns>
    public new LibSQLTransaction BeginTransaction(IsolationLevel isolationLevel = IsolationLevel.Serializable)
    {
        return BeginTransaction(isolationLevel, LibSQLTransactionBehavior.Deferred);
    }

    /// <summary>
    /// Starts a database transaction with the specified isolation level and behavior.
    /// </summary>
    /// <param name="isolationLevel">The isolation level for the transaction.</param>
    /// <param name="behavior">The transaction behavior for lock acquisition.</param>
    /// <returns>A <see cref="LibSQLTransaction"/> representing the new transaction.</returns>
    public LibSQLTransaction BeginTransaction(IsolationLevel isolationLevel, LibSQLTransactionBehavior behavior)
    {
        // Validate isolation level first before checking connection state
        ValidateIsolationLevel(isolationLevel);
        
        EnsureConnectionOpen();

        if (_currentTransaction != null)
        {
            throw new InvalidOperationException("A transaction is already active on this connection. Nested transactions are not supported.");
        }

        var transaction = new LibSQLTransaction(this, isolationLevel, behavior);
        
        try
        {
            // Execute the BEGIN statement
            var beginStatement = transaction.GetBeginStatement();
            var result = LibSQLNative.libsql_execute(_connectionHandle!, beginStatement, out var errorMsg);
            
            if (result != 0)
            {
                var errorMessage = LibSQLHelper.GetErrorMessage(errorMsg);
                LibSQLNative.libsql_free_error_msg(errorMsg);
                throw LibSQLException.FromErrorCode(result, $"Failed to begin transaction: {errorMessage}", beginStatement);
            }

            _currentTransaction = transaction;
            return transaction;
        }
        catch
        {
            // If starting the transaction failed, dispose the transaction object
            transaction.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Validates that the specified isolation level is supported by libSQL.
    /// </summary>
    /// <param name="isolationLevel">The isolation level to validate.</param>
    /// <exception cref="NotSupportedException">Thrown when the isolation level is not supported.</exception>
    private static void ValidateIsolationLevel(IsolationLevel isolationLevel)
    {
        switch (isolationLevel)
        {
            case IsolationLevel.Serializable:
            case IsolationLevel.ReadUncommitted:
            case IsolationLevel.Unspecified: // Default to Serializable
                break;
            case IsolationLevel.ReadCommitted:
                throw new NotSupportedException("ReadCommitted isolation level is not supported by libSQL. Use Serializable instead.");
            case IsolationLevel.RepeatableRead:
                throw new NotSupportedException("RepeatableRead isolation level is not supported by libSQL. Use Serializable instead.");
            case IsolationLevel.Snapshot:
                throw new NotSupportedException("Snapshot isolation level is not supported by libSQL. Use Serializable instead.");
            case IsolationLevel.Chaos:
                throw new NotSupportedException("Chaos isolation level is not supported by libSQL.");
            default:
                throw new NotSupportedException($"Isolation level {isolationLevel} is not supported by libSQL.");
        }
    }

    /// <summary>
    /// Gets the database handle for internal use.
    /// </summary>
    internal LibSQLDatabaseHandle? DatabaseHandle => _databaseHandle;

    /// <summary>
    /// Gets the connection handle for internal use.
    /// </summary>
    internal LibSQLConnectionHandle? ConnectionHandle => _connectionHandle;

    /// <summary>
    /// Gets the connection handle for transaction operations.
    /// </summary>
    internal LibSQLConnectionHandle Handle => _connectionHandle ?? throw new InvalidOperationException("Connection is not open.");

    /// <summary>
    /// Releases the unmanaged resources used by the <see cref="LibSQLConnection"/> and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && _connectionState == ConnectionState.Open)
        {
            Close();
        }
        base.Dispose(disposing);
    }

    /// <summary>
    /// Ensures the connection is in the closed state.
    /// </summary>
    /// <param name="operation">The name of the operation for error messages.</param>
    private void EnsureConnectionClosed([CallerMemberName] string operation = "")
    {
        if (_connectionState != ConnectionState.Closed)
        {
            throw new InvalidOperationException($"{operation} requires a closed connection.");
        }
    }

    /// <summary>
    /// Ensures the connection is in the open state.
    /// </summary>
    /// <param name="operation">The name of the operation for error messages.</param>
    private void EnsureConnectionOpen([CallerMemberName] string operation = "")
    {
        if (_connectionState != ConnectionState.Open)
        {
            throw new InvalidOperationException($"{operation} requires an open connection.");
        }
    }
}