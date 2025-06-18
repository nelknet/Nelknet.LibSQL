#nullable disable warnings

using System;
using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Nelknet.LibSQL.Bindings;
using Nelknet.LibSQL.Data.Exceptions;
using Nelknet.LibSQL.Data.Internal;
using Nelknet.LibSQL.Data.Http;

namespace Nelknet.LibSQL.Data;

/// <summary>
/// Represents a connection to a libSQL database.
/// </summary>
public sealed class LibSQLConnection : DbConnection
{
    private readonly object _lockObject = new();
    private LibSQLDatabaseHandle? _databaseHandle;
    private LibSQLConnectionHandle? _connectionHandle;
    private LibSQLHttpClient? _httpClient;
    private ConnectionState _connectionState = ConnectionState.Closed;
    private string _connectionString = string.Empty; // Empty string is not default for reference types
    private LibSQLConnectionStringBuilder? _connectionStringBuilder;
    internal LibSQLTransaction? _currentTransaction;
    private LibSQLStatementCache? _statementCache;
    private bool _enableStatementCaching; // false by default
    private Timer? _syncTimer;
    private bool _isSyncing;
    private bool _isHttpConnection;
    // Commented out - libSQL doesn't support direct SQLite function registration
    // private LibSQLFunctionManager? _functionManager;
    // private bool _extendedResultCodes = false;

    // Connection state change event args for performance
    private static readonly StateChangeEventArgs FromClosedToOpenEventArgs = 
        new(ConnectionState.Closed, ConnectionState.Open);
    private static readonly StateChangeEventArgs FromOpenToClosedEventArgs = 
        new(ConnectionState.Open, ConnectionState.Closed);
    
    /// <summary>
    /// Occurs when the connection receives a progress update during long-running operations.
    /// </summary>
    public event EventHandler<LibSQLProgressEventArgs>? Progress;
    
    /// <summary>
    /// Occurs when the connection is about to execute a command.
    /// </summary>
    public event EventHandler<LibSQLCommandEventArgs>? CommandExecuting;
    
    /// <summary>
    /// Occurs after the connection has executed a command.
    /// </summary>
    public event EventHandler<LibSQLCommandEventArgs>? CommandExecuted;

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
    public override string ServerVersion
    {
        get
        {
            try
            {
                // Get both libSQL and SQLite versions for comprehensive information
                var libsqlVersion = LibSQLVersion.LibSQLVersionString;
                var sqliteVersion = LibSQLVersion.SQLiteVersionString;
                
                // If we have a libSQL-specific version, use it with SQLite info
                if (!string.IsNullOrEmpty(libsqlVersion) && libsqlVersion != sqliteVersion)
                {
                    return $"libSQL {libsqlVersion} (SQLite {sqliteVersion})";
                }
                
                // Otherwise just return the SQLite version
                return $"libSQL (SQLite {sqliteVersion})";
            }
            catch
            {
                // Fallback if version retrieval fails
                return "libSQL";
            }
        }
    }

    /// <summary>
    /// Gets the current state of the connection.
    /// </summary>
    public override ConnectionState State => _connectionState;

    /// <summary>
    /// Gets or sets whether to enable statement caching for improved performance.
    /// </summary>
    /// <remarks>
    /// When enabled, prepared statements are cached and reused across command executions.
    /// This can significantly improve performance for frequently executed queries.
    /// Default is false to ensure compatibility with patterns like parameter clearing in loops.
    /// Enable this when you know your usage patterns are compatible with statement caching.
    /// </remarks>
    public bool EnableStatementCaching
    {
        get => _enableStatementCaching;
        set
        {
            _enableStatementCaching = value;
            if (!value && _statementCache != null)
            {
                _statementCache.Clear();
            }
        }
    }

    /// <summary>
    /// Gets or sets the maximum number of statements to cache.
    /// </summary>
    /// <remarks>
    /// Only takes effect when the connection is opened. Default is 100.
    /// </remarks>
    public int MaxCachedStatements { get; set; } = 100;

    /// <summary>
    /// Gets the statement cache for this connection (internal use).
    /// </summary>
    internal LibSQLStatementCache? StatementCache => _statementCache;
    
    /// <summary>
    /// Gets or sets whether the connection is in offline mode (for embedded replicas).
    /// </summary>
    public bool OfflineMode
    {
        get => ConnectionStringBuilder.Offline;
        set
        {
            ConnectionStringBuilder.Offline = value;
            
            // If we're switching offline mode and have automatic sync, update the timer
            if (_syncTimer != null)
            {
                if (value)
                {
                    // Going offline - stop the timer
                    _syncTimer.Change(Timeout.Infinite, Timeout.Infinite);
                }
                else if (ConnectionStringBuilder.SyncInterval.HasValue)
                {
                    // Going online - restart the timer
                    _syncTimer.Change(ConnectionStringBuilder.SyncInterval.Value, ConnectionStringBuilder.SyncInterval.Value);
                }
            }
        }
    }

    /// <summary>
    /// Gets whether this connection is using HTTP transport.
    /// </summary>
    internal bool IsHttpConnection => _isHttpConnection;
    
    /// <summary>
    /// Gets the HTTP client for remote connections.
    /// </summary>
    internal LibSQLHttpClient? HttpClient => _httpClient;

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
    /// Returns schema information for the data source of this connection.
    /// </summary>
    /// <returns>A <see cref="DataTable"/> that contains schema information.</returns>
    public override DataTable GetSchema()
    {
        return GetSchema(null, null);
    }
    
    /// <summary>
    /// Returns schema information for the data source of this connection using the specified string for the schema name.
    /// </summary>
    /// <param name="collectionName">Specifies the name of the schema to return.</param>
    /// <returns>A <see cref="DataTable"/> that contains schema information.</returns>
    public override DataTable GetSchema(string collectionName)
    {
        return GetSchema(collectionName, null);
    }
    
    /// <summary>
    /// Returns schema information for the data source of this connection using the specified string for the schema name and the specified string array for the restriction values.
    /// </summary>
    /// <param name="collectionName">Specifies the name of the schema to return.</param>
    /// <param name="restrictionValues">Specifies a set of restriction values for the requested schema.</param>
    /// <returns>A <see cref="DataTable"/> that contains schema information.</returns>
    public override DataTable GetSchema(string? collectionName, string?[]? restrictionValues)
    {
        EnsureConnectionOpen();
        
        var schemaReader = new LibSQLSchemaReader(this);
        return schemaReader.GetSchema(collectionName, restrictionValues) ?? new DataTable();
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
                        // Use HTTP-based remote connections
                        // Auth token is optional - some servers don't require authentication
                        _httpClient = new LibSQLHttpClient(dataSource, builder.AuthToken);
                        _isHttpConnection = true;
                        
                        // Test the connection
                        try
                        {
                            var testTask = _httpClient.TestConnectionAsync();
                            if (!testTask.Wait(TimeSpan.FromSeconds(10)) || !testTask.Result)
                            {
                                throw new LibSQLConnectionException("Failed to connect to remote libSQL server", 0, dataSource);
                            }
                        }
                        catch (Exception ex)
                        {
                            _httpClient?.Dispose();
                            _httpClient = null;
                            _isHttpConnection = false;
                            throw new LibSQLConnectionException("Failed to connect to remote libSQL server", ex);
                        }
                        
                        // Skip native handle creation for HTTP connections
                        _connectionState = ConnectionState.Open;
                        OnStateChange(FromClosedToOpenEventArgs);
                        return;
                        
                        // Legacy native remote connection code (commented out)
                        // result = LibSQLNative.libsql_open_remote_with_webpki(
                        //     dataSource, builder.AuthToken, out dbHandle, out errorMsg);
                        // break;
                        
                    case LibSQLConnectionMode.EmbeddedReplica:
                        if (string.IsNullOrEmpty(builder.SyncUrl))
                        {
                            throw new InvalidOperationException("Sync URL is required for embedded replica connections.");
                        }
                        if (string.IsNullOrEmpty(builder.SyncAuthToken ?? builder.AuthToken) && !builder.Offline)
                        {
                            throw new InvalidOperationException("Auth token is required for embedded replica connections (unless in offline mode).");
                        }
                        
                        var authTokenToUse = builder.SyncAuthToken ?? builder.AuthToken ?? string.Empty;
                        var readYourWrites = builder.ReadYourWrites ? (byte)1 : (byte)0;
                        
                        if (builder.Offline)
                        {
                            // Use config-based API for offline mode
                            var config = new LibSQLConfig
                            {
                                DbPath = Marshal.StringToCoTaskMemUTF8(dataSource),
                                PrimaryUrl = Marshal.StringToCoTaskMemUTF8(builder.SyncUrl),
                                AuthToken = Marshal.StringToCoTaskMemUTF8(authTokenToUse),
                                ReadYourWrites = readYourWrites,
                                EncryptionKey = Marshal.StringToCoTaskMemUTF8(builder.EncryptionKey),
                                SyncInterval = builder.SyncInterval ?? 0,
                                WithWebpki = 1,
                                Offline = 1
                            };
                            
                            try
                            {
                                result = LibSQLNative.libsql_open_sync_with_config(in config, out dbHandle, out errorMsg);
                            }
                            finally
                            {
                                // Clean up unmanaged memory
                                if (config.DbPath != IntPtr.Zero)
                                    Marshal.FreeCoTaskMem(config.DbPath);
                                if (config.PrimaryUrl != IntPtr.Zero)
                                    Marshal.FreeCoTaskMem(config.PrimaryUrl);
                                if (config.AuthToken != IntPtr.Zero)
                                    Marshal.FreeCoTaskMem(config.AuthToken);
                                if (config.EncryptionKey != IntPtr.Zero)
                                    Marshal.FreeCoTaskMem(config.EncryptionKey);
                            }
                        }
                        else
                        {
                            // Use WebPKI by default for embedded replicas
                            result = LibSQLNative.libsql_open_sync_with_webpki(
                                dataSource, 
                                builder.SyncUrl, 
                                authTokenToUse, 
                                readYourWrites, 
                                builder.EncryptionKey, 
                                out dbHandle, 
                                out errorMsg);
                        }
                        break;
                        
                    case LibSQLConnectionMode.Local:
                    default:
                        if (!string.IsNullOrEmpty(builder.EncryptionKey))
                        {
                            // Use the config-based API for encrypted databases
                            var config = new LibSQLConfig
                            {
                                DbPath = Marshal.StringToCoTaskMemUTF8(dataSource),
                                PrimaryUrl = IntPtr.Zero,
                                AuthToken = IntPtr.Zero,
                                ReadYourWrites = 0,
                                EncryptionKey = Marshal.StringToCoTaskMemUTF8(builder.EncryptionKey),
                                SyncInterval = 0,
                                WithWebpki = 0,
                                Offline = 1
                            };
                            
                            try
                            {
                                result = LibSQLNative.libsql_open_sync_with_config(in config, out dbHandle, out errorMsg);
                            }
                            finally
                            {
                                // Clean up unmanaged memory
                                if (config.DbPath != IntPtr.Zero)
                                    Marshal.FreeCoTaskMem(config.DbPath);
                                if (config.EncryptionKey != IntPtr.Zero)
                                    Marshal.FreeCoTaskMem(config.EncryptionKey);
                            }
                        }
                        else if (dataSource == ":memory:" || dataSource.StartsWith(":memory:?", StringComparison.Ordinal))
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
                
                // Initialize statement cache if enabled
                if (_enableStatementCaching)
                {
                    _statementCache = new LibSQLStatementCache(MaxCachedStatements);
                }
                
                // Initialize function manager (lazy initialization - only when needed)
                // _functionManager = new LibSQLFunctionManager();
                
                // Enable extended result codes for better error reporting
                // Comment out for now to test if this is causing the crash
                // if (_databaseHandle != null && !_databaseHandle.IsInvalid)
                // {
                //     LibSQLNative.sqlite3_extended_result_codes(_databaseHandle.DangerousGetHandle(), 1);
                //     _extendedResultCodes = true;
                // }
                
                // Set up automatic sync for embedded replicas
                if (builder.Mode == LibSQLConnectionMode.EmbeddedReplica && 
                    builder.SyncInterval.HasValue && 
                    builder.SyncInterval.Value > 0 &&
                    !builder.Offline)
                {
                    SetupAutomaticSync(builder.SyncInterval.Value);
                }

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
                
                // Stop and dispose sync timer
                _syncTimer?.Dispose();
                _syncTimer = null;

                // Function manager disabled - libSQL doesn't support custom functions
                // if (_functionManager != null && _databaseHandle != null && !_databaseHandle.IsInvalid)
                // {
                //     _functionManager.Clear(_databaseHandle.DangerousGetHandle());
                //     _functionManager.Dispose();
                //     _functionManager = null;
                // }

                // Dispose statement cache
                _statementCache?.Dispose();
                _statementCache = null;

                // Dispose HTTP client for remote connections
                if (_isHttpConnection)
                {
                    _httpClient?.Dispose();
                    _httpClient = null;
                    _isHttpConnection = false;
                }

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
        if (_isHttpConnection && _httpClient != null)
        {
            // For HTTP connections, we need to wrap the HTTP command in a LibSQLCommand
            // For now, return a regular LibSQLCommand - we'll modify it to delegate to HTTP
            return new LibSQLCommand
            {
                Connection = this
            };
        }
        
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
            
            if (_isHttpConnection)
            {
                // For HTTP connections, use SQL commands
                using var command = CreateCommand();
                command.CommandText = beginStatement;
                command.ExecuteNonQuery();
            }
            else
            {
                // For native connections, use libSQL API
                var result = LibSQLNative.libsql_execute(_connectionHandle!, beginStatement, out var errorMsg);
                
                if (result != 0)
                {
                    var errorMessage = LibSQLHelper.GetErrorMessage(errorMsg);
                    LibSQLNative.libsql_free_error_msg(errorMsg);
                    throw LibSQLException.FromErrorCode(result, $"Failed to begin transaction: {errorMessage}", beginStatement);
                }
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
    
    // #region Custom Functions and Aggregates - DISABLED
    // libSQL doesn't expose the underlying SQLite APIs needed for custom functions
    // These features require sqlite3_create_function_v2 which is not available through libSQL's API
    /*
    public void RegisterFunction(LibSQLFunction function)
    {
        throw new NotSupportedException("Custom functions are not supported in libSQL. This feature requires direct SQLite API access.");
    }
    
    public void RegisterAggregate<TAggregate>() where TAggregate : LibSQLAggregate, new()
    {
        throw new NotSupportedException("Custom aggregates are not supported in libSQL. This feature requires direct SQLite API access.");
    }
    
    public void UnregisterFunction(string name)
    {
        throw new NotSupportedException("Custom functions are not supported in libSQL.");
    }
    
    public void UnregisterAggregate(string name)
    {
        throw new NotSupportedException("Custom aggregates are not supported in libSQL.");
    }
    
    public bool ExtendedResultCodes => false; // Not supported in libSQL
    */
    // #endregion
    
    // #region Backup/Restore - DISABLED
    // libSQL doesn't expose the SQLite backup APIs (sqlite3_backup_*)
    /*
    public void BackupDatabase(
        LibSQLConnection destinationConnection, 
        string destinationDatabaseName = "main",
        string sourceDatabaseName = "main",
        int pagesPerStep = -1,
        Action<int, int>? progress = null)
    {
        throw new NotSupportedException("Database backup is not supported in libSQL. This feature requires direct SQLite backup API access.");
    }
    */
    // #endregion
    
    // GetLastError removed - only needed for SQLite-specific APIs
    /*
    private string GetLastError(LibSQLDatabaseHandle? handle)
    {
        if (handle == null || handle.IsInvalid)
            return "Unknown error";
        
        var errorPtr = LibSQLNative.sqlite3_errmsg(handle.DangerousGetHandle());
        if (errorPtr == IntPtr.Zero)
            return "Unknown error";
        
        return Marshal.PtrToStringAnsi(errorPtr) ?? "Unknown error";
    }
    */
    
    #region Sync Operations
    
    /// <summary>
    /// Synchronizes the embedded replica with the remote primary database.
    /// </summary>
    /// <returns>Sync statistics including frames synced.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the connection is not an embedded replica.</exception>
    /// <exception cref="LibSQLException">Thrown when sync operation fails.</exception>
    public LibSQLSyncResult Sync()
    {
        EnsureConnectionOpen();
        
        if (ConnectionStringBuilder.Mode != LibSQLConnectionMode.EmbeddedReplica)
        {
            throw new InvalidOperationException("Sync is only available for embedded replica connections.");
        }
        
        if (_databaseHandle == null)
        {
            throw new InvalidOperationException("Database handle is not initialized.");
        }
        
        if (OfflineMode)
        {
            // In offline mode, return empty result without syncing
            return new LibSQLSyncResult
            {
                FrameNo = 0,
                FramesSynced = 0,
                Duration = TimeSpan.Zero
            };
        }
        
        // Raise SyncStarted event
        OnSyncStarted();
        
        var startTime = DateTime.UtcNow;
        
        try
        {
            // Use sync2 to get detailed sync statistics
            var result = LibSQLNative.libsql_sync2(_databaseHandle, out var replicated, out var errorMsg);
            
            if (result != 0)
            {
                var errorMessage = LibSQLHelper.GetErrorMessage(errorMsg);
                LibSQLNative.libsql_free_error_msg(errorMsg);
                var ex = LibSQLException.FromErrorCode(result, $"Failed to sync database: {errorMessage}");
                OnSyncFailed(ex);
                throw ex;
            }
            
            var syncResult = new LibSQLSyncResult
            {
                FrameNo = replicated.FrameNo,
                FramesSynced = replicated.FramesSynced,
                Duration = DateTime.UtcNow - startTime
            };
            
            // Raise SyncCompleted event
            OnSyncCompleted(syncResult);
            
            return syncResult;
        }
        catch (Exception ex) when (!(ex is LibSQLException))
        {
            OnSyncFailed(ex);
            throw;
        }
    }
    
    /// <summary>
    /// Asynchronously synchronizes the embedded replica with the remote primary database.
    /// </summary>
    /// <returns>A task that represents the asynchronous sync operation, containing sync statistics.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the connection is not an embedded replica.</exception>
    /// <exception cref="LibSQLException">Thrown when sync operation fails.</exception>
    public Task<LibSQLSyncResult> SyncAsync()
    {
        return Task.Run(() => Sync());
    }
    
    /// <summary>
    /// Asynchronously synchronizes the embedded replica with cancellation support.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous sync operation, containing sync statistics.</returns>
    public async Task<LibSQLSyncResult> SyncAsync(CancellationToken cancellationToken)
    {
        // Check cancellation before starting
        cancellationToken.ThrowIfCancellationRequested();
        
        // Run sync on thread pool with cancellation support
        using (cancellationToken.Register(() => { /* Could implement interrupt if libSQL supports it */ }))
        {
            return await Task.Run(() => Sync(), cancellationToken).ConfigureAwait(false);
        }
    }
    
    #endregion
    
    #region Automatic Sync
    
    /// <summary>
    /// Sets up automatic sync with the specified interval.
    /// </summary>
    /// <param name="intervalMs">The sync interval in milliseconds.</param>
    private void SetupAutomaticSync(int intervalMs)
    {
        _syncTimer = new Timer(AutomaticSyncCallback, null, intervalMs, intervalMs);
    }
    
    /// <summary>
    /// Callback for automatic sync timer.
    /// </summary>
    private void AutomaticSyncCallback(object? state)
    {
        // Skip if already syncing or connection is not open
        if (_isSyncing || _connectionState != ConnectionState.Open)
            return;
        
        _isSyncing = true;
        try
        {
            // Run sync asynchronously without blocking the timer
            Task.Run(async () =>
            {
                try
                {
                    await SyncAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    // Log error but don't crash the timer
                    OnSyncFailed(ex);
                }
                finally
                {
                    _isSyncing = false;
                }
            });
        }
        catch
        {
            _isSyncing = false;
            throw;
        }
    }
    
    #endregion
    
    #region Sync Events
    
    /// <summary>
    /// Occurs when synchronization starts.
    /// </summary>
    public event EventHandler? SyncStarted;
    
    /// <summary>
    /// Occurs when synchronization completes successfully.
    /// </summary>
    public event EventHandler<LibSQLSyncCompletedEventArgs>? SyncCompleted;
    
    /// <summary>
    /// Occurs when synchronization fails.
    /// </summary>
    public event EventHandler<LibSQLSyncFailedEventArgs>? SyncFailed;
    
    /// <summary>
    /// Raises the SyncStarted event.
    /// </summary>
    private void OnSyncStarted()
    {
        SyncStarted?.Invoke(this, EventArgs.Empty);
    }
    
    /// <summary>
    /// Raises the SyncCompleted event.
    /// </summary>
    private void OnSyncCompleted(LibSQLSyncResult result)
    {
        SyncCompleted?.Invoke(this, new LibSQLSyncCompletedEventArgs(result));
    }
    
    /// <summary>
    /// Raises the SyncFailed event.
    /// </summary>
    private void OnSyncFailed(Exception exception)
    {
        SyncFailed?.Invoke(this, new LibSQLSyncFailedEventArgs(exception));
    }
    
    #endregion
    
    #region Event Handling
    
    /// <summary>
    /// Raises the Progress event.
    /// </summary>
    internal void OnProgress(int current, int total, string? message = null)
    {
        Progress?.Invoke(this, new LibSQLProgressEventArgs(current, total, message));
    }
    
    /// <summary>
    /// Raises the CommandExecuting event.
    /// </summary>
    internal bool OnCommandExecuting(string commandText)
    {
        if (CommandExecuting == null)
            return true;
        
        var args = new LibSQLCommandEventArgs(commandText);
        CommandExecuting.Invoke(this, args);
        return !args.Cancel;
    }
    
    /// <summary>
    /// Raises the CommandExecuted event.
    /// </summary>
    internal void OnCommandExecuted(string commandText, TimeSpan duration)
    {
        CommandExecuted?.Invoke(this, new LibSQLCommandEventArgs(commandText, duration));
    }
    
    #endregion
    
    #region Internal Methods
    
    /// <summary>
    /// Gets the HTTP client for remote connections.
    /// </summary>
    /// <returns>The HTTP client or null if this is not an HTTP connection.</returns>
    internal Http.LibSQLHttpClient? GetHttpClient()
    {
        return _httpClient;
    }
    
    #endregion
}