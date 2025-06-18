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
using Nelknet.LibSQL.Data.Http;

namespace Nelknet.LibSQL.Data;

/// <summary>
/// Represents a SQL command to execute against a libSQL database.
/// </summary>
public sealed class LibSQLCommand : DbCommand
{
    private LibSQLConnection? _connection;
    private string? _commandText = string.Empty;
    private int _commandTimeout = 30;
    private LibSQLStatementHandle? _preparedStatement;
    private bool _isPrepared;
    private bool _explainMode;
    private ExplainVerbosity _explainVerbosity = ExplainVerbosity.Normal;
    private bool _enableStatementCaching = true;
    private LibSQLHttpCommand? _httpCommand;

    /// <summary>
    /// Initializes a new instance of the <see cref="LibSQLCommand"/> class.
    /// </summary>
    public LibSQLCommand()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LibSQLCommand"/> class with the specified command text.
    /// </summary>
    /// <param name="commandText">The text of the command.</param>
    public LibSQLCommand(string commandText) : this()
    {
        CommandText = commandText;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LibSQLCommand"/> class with the specified command text and connection.
    /// </summary>
    /// <param name="commandText">The text of the command.</param>
    /// <param name="connection">The connection to the database.</param>
    public LibSQLCommand(string commandText, LibSQLConnection connection) : this(commandText)
    {
        Connection = connection;
    }

    /// <summary>
    /// Gets or sets the SQL statement or stored procedure to execute.
    /// </summary>
    public override string CommandText
    {
        get => _commandText ?? string.Empty;
        set
        {
            if (_commandText != value)
            {
                _commandText = value;
                // Invalidate prepared statement when command text changes
                ReleasePreparedStatement();
                
                // Sync with HTTP command
                if (_httpCommand != null)
                {
                    _httpCommand.CommandText = value ?? string.Empty;
                }
            }
        }
    }

    /// <summary>
    /// Gets or sets the wait time (in seconds) before terminating the attempt to execute a command and generating an error.
    /// </summary>
    public override int CommandTimeout
    {
        get => _commandTimeout;
        set
        {
            _commandTimeout = value >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(value));
            
            // Sync with HTTP command
            if (_httpCommand != null)
            {
                _httpCommand.CommandTimeout = _commandTimeout;
            }
        }
    }

    /// <summary>
    /// Gets or sets how the CommandText property is interpreted.
    /// </summary>
    public override CommandType CommandType
    {
        get => CommandType.Text;
        set
        {
            if (value != CommandType.Text)
            {
                throw new NotSupportedException("libSQL only supports CommandType.Text.");
            }
        }
    }

    /// <summary>
    /// Gets or sets the <see cref="LibSQLConnection"/> used by this command.
    /// </summary>
    public new LibSQLConnection? Connection
    {
        get => _connection;
        set
        {
            if (_connection != value)
            {
                _connection = value;
                // Invalidate prepared statement when connection changes
                ReleasePreparedStatement();
                
                // Initialize HTTP command if needed
                if (_connection?.IsHttpConnection == true && _connection.HttpClient != null)
                {
                    _httpCommand?.Dispose();
                    _httpCommand = new LibSQLHttpCommand(_connection.HttpClient);
                }
                else
                {
                    _httpCommand?.Dispose();
                    _httpCommand = null;
                }
            }
        }
    }

    /// <summary>
    /// Gets or sets the <see cref="DbConnection"/> used by this command.
    /// </summary>
    protected override DbConnection? DbConnection
    {
        get => Connection;
        set => Connection = (LibSQLConnection?)value;
    }

    /// <summary>
    /// Gets the collection of <see cref="DbParameter"/> objects.
    /// </summary>
    protected override DbParameterCollection DbParameterCollection => Parameters;

    /// <summary>
    /// Gets the collection of <see cref="LibSQLParameter"/> objects.
    /// </summary>
    public new LibSQLParameterCollection Parameters { get; } = new();

    /// <summary>
    /// Gets or sets the transaction within which the command executes.
    /// </summary>
    protected override DbTransaction? DbTransaction { get; set; }

    /// <summary>
    /// Gets or sets whether the command object should be visible in a customized interface control.
    /// </summary>
    public override bool DesignTimeVisible { get; set; }
    
    /// <summary>
    /// Gets or sets whether statement caching is enabled for this command.
    /// </summary>
    /// <remarks>
    /// When true (default), prepared statements may be cached and reused for better performance.
    /// Set to false for commands that are executed in loops with different parameters.
    /// </remarks>
    public bool EnableStatementCaching
    {
        get => _enableStatementCaching;
        set => _enableStatementCaching = value;
    }

    /// <summary>
    /// Gets or sets how command results are applied to the DataRow when used by the Update method of a DbDataAdapter.
    /// </summary>
    public override UpdateRowSource UpdatedRowSource { get; set; } = UpdateRowSource.None;

    /// <summary>
    /// Cancels the execution of the command.
    /// </summary>
    public override void Cancel()
    {
        // libSQL doesn't support cancellation at the moment
        // This is a no-op for now
    }

    /// <summary>
    /// Executes the command and returns the number of rows affected.
    /// </summary>
    /// <returns>The number of rows affected.</returns>
    public override int ExecuteNonQuery()
    {
        EnsureConnectionOpen();
        
        if (string.IsNullOrWhiteSpace(CommandText))
        {
            throw new InvalidOperationException("CommandText property has not been properly initialized.");
        }

        // Delegate to HTTP command for remote connections
        if (_httpCommand != null)
        {
            SyncParametersToHttp();
            return _httpCommand.ExecuteNonQuery();
        }

        var connectionHandle = Connection!.ConnectionHandle!;
        IntPtr errorMsg;
        int result;

        if (_isPrepared && _preparedStatement != null)
        {
            // Reset the prepared statement first
            var resetResult = LibSQLNative.libsql_reset_stmt(_preparedStatement, out var resetErrorMsg);
            if (resetResult != 0)
            {
                var resetError = LibSQLHelper.GetErrorMessage(resetErrorMsg);
                LibSQLNative.libsql_free_error_msg(resetErrorMsg);
                throw new InvalidOperationException($"Failed to reset prepared statement: {resetError}");
            }
            
            // Bind parameters to prepared statement
            BindParameters(_preparedStatement);
            
            // Execute prepared statement
            result = LibSQLNative.libsql_execute_stmt(_preparedStatement, out errorMsg);
        }
        else if (Parameters.Count > 0)
        {
            // We have parameters but no prepared statement - use helper method
            var statement = GetOrPrepareStatement(connectionHandle, out var usingCachedStatement);
            
            try
            {
                // Bind parameters
                BindParameters(statement);
                
                // Execute the prepared statement
                result = LibSQLNative.libsql_execute_stmt(statement, out errorMsg);
            }
            finally
            {
                // Only dispose if not cached
                if (!usingCachedStatement)
                {
                    statement?.Dispose();
                }
            }
        }
        else
        {
            // Execute directly - no parameters
            result = LibSQLNative.libsql_execute(connectionHandle, CommandText, out errorMsg);
        }

        if (result != 0)
        {
            var errorMessage = LibSQLHelper.GetErrorMessage(errorMsg);
            LibSQLNative.libsql_free_error_msg(errorMsg);
            
            // Use the error handler to create the appropriate exception type
            // Note: We can't use the connectionHandle for error checking since it's not a SafeHandle
            LibSQLErrorHandler.CheckResult(result, null, CommandText, errorMessage);
        }

        // Get the number of changes made
        return (int)LibSQLNative.libsql_changes(connectionHandle);
    }

    /// <summary>
    /// Executes the command and returns the first column of the first row in the result set.
    /// </summary>
    /// <returns>The first column of the first row in the result set.</returns>
    public override object? ExecuteScalar()
    {
        EnsureConnectionOpen();
        
        if (string.IsNullOrWhiteSpace(CommandText))
        {
            throw new InvalidOperationException("CommandText property has not been properly initialized.");
        }

        // Delegate to HTTP command for remote connections
        if (_httpCommand != null)
        {
            SyncParametersToHttp();
            return _httpCommand.ExecuteScalar();
        }

        var connectionHandle = Connection!.ConnectionHandle!;
        IntPtr rowsHandle;
        IntPtr errorMsg;
        int result;

        if (_isPrepared && _preparedStatement != null)
        {
            // Reset the prepared statement first
            var resetResult = LibSQLNative.libsql_reset_stmt(_preparedStatement, out var resetErrorMsg);
            if (resetResult != 0)
            {
                var resetError = LibSQLHelper.GetErrorMessage(resetErrorMsg);
                LibSQLNative.libsql_free_error_msg(resetErrorMsg);
                throw new InvalidOperationException($"Failed to reset prepared statement: {resetError}");
            }
            
            // Bind parameters to prepared statement
            BindParameters(_preparedStatement);
            
            // Query using prepared statement
            result = LibSQLNative.libsql_query_stmt(_preparedStatement, out rowsHandle, out errorMsg);
        }
        else if (Parameters.Count > 0)
        {
            // We have parameters but no prepared statement - use helper method
            var statement = GetOrPrepareStatement(connectionHandle, out var usingCachedStatement);
            
            try
            {
                // Bind parameters
                BindParameters(statement);
                
                // Query using the prepared statement
                result = LibSQLNative.libsql_query_stmt(statement, out rowsHandle, out errorMsg);
            }
            finally
            {
                // Only dispose if not cached
                if (!usingCachedStatement)
                {
                    statement?.Dispose();
                }
            }
        }
        else
        {
            // Query directly - no parameters
            result = LibSQLNative.libsql_query(connectionHandle, CommandText, out rowsHandle, out errorMsg);
        }

        if (result != 0)
        {
            var errorMessage = LibSQLHelper.GetErrorMessage(errorMsg);
            LibSQLNative.libsql_free_error_msg(errorMsg);
            throw new InvalidOperationException($"Failed to execute query: {errorMessage}");
        }

        try
        {
            using var rows = new LibSQLRowsHandle(rowsHandle);
            
            // Get the first row
            IntPtr rowHandle;
            result = LibSQLNative.libsql_next_row(rows, out rowHandle, out errorMsg);
            
            if (result != 0)
            {
                var errorMessage = LibSQLHelper.GetErrorMessage(errorMsg);
                LibSQLNative.libsql_free_error_msg(errorMsg);
                throw new InvalidOperationException($"Failed to get first row: {errorMessage}");
            }

            if (rowHandle == IntPtr.Zero)
            {
                // No rows returned
                return null;
            }

            try
            {
                using var row = new LibSQLRowHandle(rowHandle);
                
                // Get the column type first
                result = LibSQLNative.libsql_column_type(rows, row, 0, out int columnType, out errorMsg);
                if (result != 0)
                {
                    var errorMessage = LibSQLHelper.GetErrorMessage(errorMsg);
                    LibSQLNative.libsql_free_error_msg(errorMsg);
                    throw new InvalidOperationException($"Failed to get column type: {errorMessage}");
                }

                // Get value based on column type
                switch (columnType)
                {
                    case 1: // INTEGER
                        result = LibSQLNative.libsql_get_int(row, 0, out long intValue, out errorMsg);
                        if (result != 0)
                        {
                            var errorMessage = LibSQLHelper.GetErrorMessage(errorMsg);
                            LibSQLNative.libsql_free_error_msg(errorMsg);
                            throw new InvalidOperationException($"Failed to get integer value: {errorMessage}");
                        }
                        return intValue;

                    case 2: // FLOAT
                        result = LibSQLNative.libsql_get_float(row, 0, out double floatValue, out errorMsg);
                        if (result != 0)
                        {
                            var errorMessage = LibSQLHelper.GetErrorMessage(errorMsg);
                            LibSQLNative.libsql_free_error_msg(errorMsg);
                            throw new InvalidOperationException($"Failed to get float value: {errorMessage}");
                        }
                        return floatValue;

                    case 3: // TEXT
                        IntPtr valuePtr;
                        result = LibSQLNative.libsql_get_string(row, 0, out valuePtr, out errorMsg);
                        if (result != 0)
                        {
                            var errorMessage = LibSQLHelper.GetErrorMessage(errorMsg);
                            LibSQLNative.libsql_free_error_msg(errorMsg);
                            throw new InvalidOperationException($"Failed to get string value: {errorMessage}");
                        }
                        if (valuePtr == IntPtr.Zero)
                        {
                            return null;
                        }
                        try
                        {
                            return System.Runtime.InteropServices.Marshal.PtrToStringUTF8(valuePtr);
                        }
                        finally
                        {
                            LibSQLNative.libsql_free_string(valuePtr);
                        }

                    case 4: // BLOB
                        result = LibSQLNative.libsql_get_blob(row, 0, out LibSQLBlob blob, out errorMsg);
                        if (result != 0)
                        {
                            var errorMessage = LibSQLHelper.GetErrorMessage(errorMsg);
                            LibSQLNative.libsql_free_error_msg(errorMsg);
                            throw new InvalidOperationException($"Failed to get blob value: {errorMessage}");
                        }
                        var blobData = blob.ToByteArray();
                        LibSQLNative.libsql_free_blob(blob);
                        return blobData;

                    case 5: // NULL
                        return null;

                    default:
                        throw new NotSupportedException($"Unknown column type: {columnType}");
                }
            }
            finally
            {
                LibSQLNative.libsql_free_row(rowHandle);
            }
        }
        finally
        {
            LibSQLNative.libsql_free_rows(rowsHandle);
        }
    }

    /// <summary>
    /// Executes the command and returns a <see cref="DbDataReader"/>.
    /// </summary>
    /// <param name="behavior">One of the <see cref="CommandBehavior"/> values.</param>
    /// <returns>A <see cref="DbDataReader"/> object.</returns>
    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
    {
        return ExecuteReader(behavior);
    }

    /// <summary>
    /// Executes the command and returns a <see cref="LibSQLDataReader"/>.
    /// </summary>
    /// <param name="behavior">One of the <see cref="CommandBehavior"/> values.</param>
    /// <returns>A <see cref="LibSQLDataReader"/> object.</returns>
    public new LibSQLDataReader ExecuteReader(CommandBehavior behavior = CommandBehavior.Default)
    {
        EnsureConnectionOpen();
        
        if (string.IsNullOrWhiteSpace(CommandText))
        {
            throw new InvalidOperationException("CommandText property has not been properly initialized.");
        }

        // Delegate to HTTP command for remote connections
        if (_httpCommand != null)
        {
            SyncParametersToHttp();
            var httpReader = (LibSQLHttpDataReader)_httpCommand.ExecuteReader();
            // Wrap the HTTP reader in a LibSQLDataReader
            return new LibSQLDataReader(httpReader);
        }

        var connectionHandle = Connection!.ConnectionHandle!;
        IntPtr rowsHandle;
        IntPtr errorMsg;
        int result;

        if (_isPrepared && _preparedStatement != null)
        {
            // Reset the prepared statement first
            var resetResult = LibSQLNative.libsql_reset_stmt(_preparedStatement, out var resetErrorMsg);
            if (resetResult != 0)
            {
                var resetError = LibSQLHelper.GetErrorMessage(resetErrorMsg);
                LibSQLNative.libsql_free_error_msg(resetErrorMsg);
                throw new InvalidOperationException($"Failed to reset prepared statement: {resetError}");
            }
            
            // Bind parameters to prepared statement
            BindParameters(_preparedStatement);
            
            // Query using prepared statement
            result = LibSQLNative.libsql_query_stmt(_preparedStatement, out rowsHandle, out errorMsg);
        }
        else if (Parameters.Count > 0)
        {
            // We have parameters but no prepared statement - use helper method
            var statement = GetOrPrepareStatement(connectionHandle, out var usingCachedStatement);
            
            try
            {
                // Bind parameters
                BindParameters(statement);
                
                // Query using the prepared statement
                result = LibSQLNative.libsql_query_stmt(statement, out rowsHandle, out errorMsg);
            }
            finally
            {
                // Only dispose if not cached
                if (!usingCachedStatement)
                {
                    statement?.Dispose();
                }
            }
        }
        else
        {
            // Query directly - no parameters
            result = LibSQLNative.libsql_query(connectionHandle, CommandText, out rowsHandle, out errorMsg);
        }

        if (result != 0)
        {
            var errorMessage = LibSQLHelper.GetErrorMessage(errorMsg);
            LibSQLNative.libsql_free_error_msg(errorMsg);
            throw new InvalidOperationException($"Failed to execute query: {errorMessage}");
        }

        // Create LibSQLDataReader with the rows handle
        var rowsHandleWrapper = new LibSQLRowsHandle(rowsHandle);
        return new LibSQLDataReader(rowsHandleWrapper, behavior);
    }

    /// <summary>
    /// Creates a prepared (or compiled) version of the command on the data source.
    /// </summary>
    public override void Prepare()
    {
        EnsureConnectionOpen();
        
        if (string.IsNullOrWhiteSpace(CommandText))
        {
            throw new InvalidOperationException("CommandText property has not been properly initialized.");
        }

        // Release any existing prepared statement
        ReleasePreparedStatement();

        var connectionHandle = Connection!.ConnectionHandle!;
        IntPtr stmtHandle;
        IntPtr errorMsg;
        
        var result = LibSQLNative.libsql_prepare(connectionHandle, CommandText, out stmtHandle, out errorMsg);
        
        if (result != 0)
        {
            var errorMessage = LibSQLHelper.GetErrorMessage(errorMsg);
            LibSQLNative.libsql_free_error_msg(errorMsg);
            throw new InvalidOperationException($"Failed to prepare statement: {errorMessage}");
        }

        _preparedStatement = new LibSQLStatementHandle(stmtHandle);
        _isPrepared = true;
    }

    /// <summary>
    /// Creates a new instance of a <see cref="DbParameter"/> object.
    /// </summary>
    /// <returns>A <see cref="DbParameter"/> object.</returns>
    protected override DbParameter CreateDbParameter()
    {
        return CreateParameter();
    }

    /// <summary>
    /// Creates a new instance of a <see cref="LibSQLParameter"/> object.
    /// </summary>
    /// <returns>A <see cref="LibSQLParameter"/> object.</returns>
    public new LibSQLParameter CreateParameter()
    {
        return new LibSQLParameter();
    }

    /// <summary>
    /// Releases the unmanaged resources used by the <see cref="LibSQLCommand"/> and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ReleasePreparedStatement();
            _httpCommand?.Dispose();
            _httpCommand = null;
        }
        base.Dispose(disposing);
    }

    /// <summary>
    /// Ensures the connection is open and available.
    /// </summary>
    private void EnsureConnectionOpen()
    {
        if (Connection is null)
        {
            throw new InvalidOperationException("Connection property has not been initialized.");
        }

        if (Connection.State != ConnectionState.Open)
        {
            throw new InvalidOperationException("Connection must be open to execute commands.");
        }
    }

    /// <summary>
    /// Releases any prepared statement resources.
    /// </summary>
    private void ReleasePreparedStatement()
    {
        if (_preparedStatement != null)
        {
            _preparedStatement.Dispose();
            _preparedStatement = null;
            _isPrepared = false;
        }
    }

    /// <summary>
    /// Binds parameters to the prepared statement.
    /// </summary>
    /// <param name="statement">The prepared statement handle.</param>
    private void BindParameters(LibSQLStatementHandle statement)
    {
        // Validate all parameters before binding
        Parameters.ValidateParameters();
        
        for (int i = 0; i < Parameters.Count; i++)
        {
            var parameter = Parameters[i];
            IntPtr errorMsg;
            int result;

            // Get the converted value for libSQL
            var libSQLValue = parameter.GetLibSQLValue();

            if (LibSQLTypeConverter.IsNull(libSQLValue))
            {
                result = LibSQLNative.libsql_bind_null(statement, i + 1, out errorMsg);
            }
            else
            {
                switch (parameter.LibSQLType)
                {
                    case LibSQLDbType.Integer:
                        var longValue = (long)libSQLValue;
                        result = LibSQLNative.libsql_bind_int(statement, i + 1, longValue, out errorMsg);
                        break;

                    case LibSQLDbType.Real:
                        var doubleValue = (double)libSQLValue;
                        result = LibSQLNative.libsql_bind_float(statement, i + 1, doubleValue, out errorMsg);
                        break;

                    case LibSQLDbType.Text:
                        var stringValue = (string)libSQLValue;
                        result = LibSQLNative.libsql_bind_string(statement, i + 1, stringValue, out errorMsg);
                        break;

                    case LibSQLDbType.Blob:
                        var blobValue = (byte[])libSQLValue;
                        var pinnedBlob = GCHandle.Alloc(blobValue, GCHandleType.Pinned);
                        try
                        {
                            result = LibSQLNative.libsql_bind_blob(statement, i + 1, pinnedBlob.AddrOfPinnedObject(), blobValue.Length, out errorMsg);
                        }
                        finally
                        {
                            pinnedBlob.Free();
                        }
                        break;

                    default:
                        throw new NotSupportedException($"Unsupported libSQL type: {parameter.LibSQLType}");
                }
            }

            if (result != 0)
            {
                var errorMessage = LibSQLHelper.GetErrorMessage(errorMsg);
                LibSQLNative.libsql_free_error_msg(errorMsg);
                throw new InvalidOperationException($"Failed to bind parameter {i + 1}: {errorMessage}");
            }
        }
    }
    
    /// <summary>
    /// Synchronizes parameters from this command to the HTTP command.
    /// </summary>
    private void SyncParametersToHttp()
    {
        if (_httpCommand == null) return;
        
        // Clear existing parameters
        _httpCommand.Parameters.Clear();
        
        // Copy all parameters
        foreach (LibSQLParameter param in Parameters)
        {
            var httpParam = new LibSQLParameter(param.ParameterName, param.DbType)
            {
                Value = param.Value,
                Direction = param.Direction,
                Size = param.Size,
                Precision = param.Precision,
                Scale = param.Scale
            };
            _httpCommand.Parameters.Add(httpParam);
        }
    }
    
    /// <summary>
    /// Gets or prepares a statement, using the cache if enabled.
    /// </summary>
    /// <param name="connectionHandle">The connection handle.</param>
    /// <param name="usingCachedStatement">Whether the returned statement is from the cache.</param>
    /// <returns>The prepared statement handle.</returns>
    private LibSQLStatementHandle GetOrPrepareStatement(LibSQLConnectionHandle connectionHandle, out bool usingCachedStatement)
    {
        LibSQLStatementHandle? statement = null;
        usingCachedStatement = false;
        
        // Don't cache statements with positional parameters (?) as they're often used
        // for bulk operations where the same statement is executed many times
        bool hasPositionalParameters = CommandText.Contains('?') && !CommandText.Contains('@');
        
        // Try to get from cache if enabled at both connection and command level, and not using positional parameters
        if (_enableStatementCaching && Connection!.EnableStatementCaching && Connection.StatementCache != null && !hasPositionalParameters)
        {
            usingCachedStatement = Connection.StatementCache.TryGetStatement(CommandText, out statement);
        }
        
        if (!usingCachedStatement)
        {
            // Prepare new statement
            IntPtr errorMsg;
            var result = LibSQLNative.libsql_prepare(connectionHandle, CommandText, out var stmtHandle, out errorMsg);
            if (result != 0)
            {
                var errorMessage = LibSQLHelper.GetErrorMessage(errorMsg);
                LibSQLNative.libsql_free_error_msg(errorMsg);
                throw new InvalidOperationException($"Failed to prepare statement: {errorMessage}");
            }
            
            statement = new LibSQLStatementHandle(stmtHandle);
            
            // Add to cache if enabled
            if (Connection.EnableStatementCaching && Connection.StatementCache != null)
            {
                Connection.StatementCache.AddStatement(CommandText, statement);
                usingCachedStatement = true; // Don't dispose since it's now cached
            }
        }
        else if (statement != null)
        {
            // Reset cached statement before reuse
            IntPtr resetErrorMsg;
            var resetResult = LibSQLNative.libsql_reset_stmt(statement, out resetErrorMsg);
            if (resetResult != 0)
            {
                var resetError = LibSQLHelper.GetErrorMessage(resetErrorMsg);
                LibSQLNative.libsql_free_error_msg(resetErrorMsg);
                throw new InvalidOperationException($"Failed to reset cached statement: {resetError}");
            }
        }
        
        return statement!;
    }
    
    #region Query Plan Support
    
    /// <summary>
    /// Gets or sets whether this command should return query plan information instead of executing.
    /// </summary>
    public bool ExplainMode
    {
        get => _explainMode;
        set => _explainMode = value;
    }
    
    /// <summary>
    /// Gets or sets the verbosity level for EXPLAIN commands.
    /// </summary>
    public ExplainVerbosity ExplainVerbosity
    {
        get => _explainVerbosity;
        set => _explainVerbosity = value;
    }
    
    /// <summary>
    /// Executes the command and returns the query plan.
    /// </summary>
    /// <returns>A DataTable containing the query plan information.</returns>
    public DataTable GetQueryPlan()
    {
        EnsureConnectionOpen();
        
        var originalText = _commandText;
        var originalExplainMode = _explainMode;
        
        try
        {
            // Prepend EXPLAIN or EXPLAIN QUERY PLAN to the command
            var explainPrefix = _explainVerbosity switch
            {
                ExplainVerbosity.QueryPlan => "EXPLAIN QUERY PLAN ",
                ExplainVerbosity.Detailed => "EXPLAIN ",
                _ => "EXPLAIN QUERY PLAN "
            };
            
            _commandText = explainPrefix + originalText;
            _explainMode = true;
            
            // Execute and read the results into a DataTable
            using var reader = ExecuteReader();
            var table = new DataTable("QueryPlan");
            
            // Add columns
            for (int i = 0; i < reader.FieldCount; i++)
            {
                table.Columns.Add(reader.GetName(i), typeof(string));
            }
            
            // Add rows
            while (reader.Read())
            {
                var row = table.NewRow();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    row[i] = reader.IsDBNull(i) ? DBNull.Value : reader.GetValue(i);
                }
                table.Rows.Add(row);
            }
            
            return table;
        }
        finally
        {
            _commandText = originalText;
            _explainMode = originalExplainMode;
        }
    }
    
    #endregion

    #region Batch Execution

    /// <summary>
    /// Executes multiple SQL statements as a single batch.
    /// For remote connections, this uses the Hrana protocol's sequence request.
    /// For local connections, this executes statements sequentially.
    /// </summary>
    /// <param name="statements">The SQL statements to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The total number of affected rows, or -1 if not available.</returns>
    public async Task<int> ExecuteBatchAsync(string[] statements, CancellationToken cancellationToken = default)
    {
        if (statements == null || statements.Length == 0)
            throw new ArgumentException("Statements array cannot be null or empty", nameof(statements));

        EnsureConnectionOpen();

        // For HTTP connections, use the optimized sequence execution
        if (_httpCommand != null)
        {
            // Create a custom HTTP command to execute the sequence
            var batch = new Http.HranaBatchRequest();
            var combinedSql = string.Join("; ", statements);
            
            batch.Requests.Add(new Http.HranaRequest
            {
                Type = Http.HranaTypes.Sequence,
                Sql = combinedSql
            });

            var httpClient = ((LibSQLConnection)Connection!).GetHttpClient();
            if (httpClient == null)
                throw new InvalidOperationException("HTTP client not available");
                
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            if (CommandTimeout > 0)
            {
                cts.CancelAfter(TimeSpan.FromSeconds(CommandTimeout));
            }

            var response = await httpClient.ExecuteBatchAsync(batch, cts.Token).ConfigureAwait(false);
            
            // Sequence requests don't return affected rows
            return -1;
        }

        // For local connections, execute statements sequentially
        var totalAffected = 0;
        foreach (var statement in statements)
        {
            _commandText = statement;
            var affected = await ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            if (affected > 0)
                totalAffected += affected;
        }

        return totalAffected;
    }

    /// <summary>
    /// Executes multiple SQL statements as a transactional batch.
    /// All statements are executed within a transaction that is automatically
    /// committed if all succeed, or rolled back if any fail.
    /// </summary>
    /// <param name="statements">The SQL statements to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The total number of affected rows, or -1 if not available.</returns>
    public async Task<int> ExecuteTransactionalBatchAsync(string[] statements, CancellationToken cancellationToken = default)
    {
        if (statements == null || statements.Length == 0)
            throw new ArgumentException("Statements array cannot be null or empty", nameof(statements));

        EnsureConnectionOpen();

        // For HTTP connections, use the optimized transactional batch
        if (_httpCommand != null)
        {
            return await _httpCommand.ExecuteTransactionalBatchAsync(statements, cancellationToken).ConfigureAwait(false);
        }

        // For local connections, use a transaction
        using var transaction = Connection!.BeginTransaction();
        try
        {
            Transaction = transaction;
            var totalAffected = 0;

            foreach (var statement in statements)
            {
                _commandText = statement;
                var affected = await ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                if (affected > 0)
                    totalAffected += affected;
            }

            transaction.Commit();
            return totalAffected;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
        finally
        {
            Transaction = null;
        }
    }

    #endregion
}