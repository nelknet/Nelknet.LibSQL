#nullable disable warnings

using System;
using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;
using Nelknet.LibSQL.Bindings;
using Nelknet.LibSQL.Native;

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
    private LibSQLConnectionString? _parsedConnectionString;

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
            _parsedConnectionString = null; // Reset parsed connection string
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
    public override string Database => ParsedConnectionString.DataSource;

    /// <summary>
    /// Gets the name of the database server.
    /// </summary>
    public override string DataSource => ParsedConnectionString.DataSource;

    /// <summary>
    /// Gets the version of the libSQL server.
    /// </summary>
    public override string ServerVersion => "libSQL"; // TODO: Get actual version from native library

    /// <summary>
    /// Gets the current state of the connection.
    /// </summary>
    public override ConnectionState State => _connectionState;

    /// <summary>
    /// Gets the parsed connection string.
    /// </summary>
    private LibSQLConnectionString ParsedConnectionString
    {
        get
        {
            if (_parsedConnectionString is null)
            {
                _parsedConnectionString = LibSQLConnectionString.Parse(_connectionString);
            }
            return _parsedConnectionString;
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

                var parsed = ParsedConnectionString;
                IntPtr dbHandle;
                IntPtr errorMsg;
                
                // Open the database
                int result;
                if (parsed.IsRemote)
                {
                    if (string.IsNullOrEmpty(parsed.AuthToken))
                    {
                        throw new InvalidOperationException("Auth token is required for remote connections.");
                    }
                    
                    if (parsed.WithWebPKI)
                    {
                        result = LibSQLNative.libsql_open_remote_with_webpki(
                            parsed.DataSource, parsed.AuthToken, out dbHandle, out errorMsg);
                    }
                    else
                    {
                        result = LibSQLNative.libsql_open_remote(
                            parsed.DataSource, parsed.AuthToken, out dbHandle, out errorMsg);
                    }
                }
                else if (parsed.IsFile)
                {
                    result = LibSQLNative.libsql_open_file(parsed.DataSource, out dbHandle, out errorMsg);
                }
                else
                {
                    result = LibSQLNative.libsql_open_ext(parsed.DataSource, out dbHandle, out errorMsg);
                }

                if (result != 0)
                {
                    var errorMessage = LibSQLHelper.GetErrorMessage(errorMsg);
                    LibSQLNative.libsql_free_error_msg(errorMsg);
                    throw new InvalidOperationException($"Failed to open database: {errorMessage}");
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
                    throw new InvalidOperationException($"Failed to connect to database: {errorMessage}");
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
        EnsureConnectionOpen();
        // TODO: Implement LibSQLTransaction
        throw new NotImplementedException("Transaction support will be implemented in Phase 9.");
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