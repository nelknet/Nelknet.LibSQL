#nullable disable warnings

using System;
using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Nelknet.LibSQL.Bindings;
using Nelknet.LibSQL.Native;

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
            }
        }
    }

    /// <summary>
    /// Gets or sets the wait time (in seconds) before terminating the attempt to execute a command and generating an error.
    /// </summary>
    public override int CommandTimeout
    {
        get => _commandTimeout;
        set => _commandTimeout = value >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(value));
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

        var connectionHandle = Connection!.ConnectionHandle!;
        IntPtr errorMsg;
        int result;

        if (_isPrepared && _preparedStatement != null)
        {
            // Bind parameters to prepared statement
            BindParameters(_preparedStatement);
            
            // Execute prepared statement
            result = LibSQLNative.libsql_execute_stmt(_preparedStatement, out errorMsg);
        }
        else
        {
            // Execute directly
            result = LibSQLNative.libsql_execute(connectionHandle, CommandText, out errorMsg);
        }

        if (result != 0)
        {
            var errorMessage = LibSQLHelper.GetErrorMessage(errorMsg);
            LibSQLNative.libsql_free_error_msg(errorMsg);
            throw new InvalidOperationException($"Failed to execute command: {errorMessage}");
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

        var connectionHandle = Connection!.ConnectionHandle!;
        IntPtr rowsHandle;
        IntPtr errorMsg;
        int result;

        if (_isPrepared && _preparedStatement != null)
        {
            // Bind parameters to prepared statement
            BindParameters(_preparedStatement);
            
            // Query using prepared statement
            result = LibSQLNative.libsql_query_stmt(_preparedStatement, out rowsHandle, out errorMsg);
        }
        else
        {
            // Query directly
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
                
                // Get the value from the first column (index 0)
                IntPtr valuePtr;
                result = LibSQLNative.libsql_get_string(row, 0, out valuePtr, out errorMsg);
                
                if (result != 0)
                {
                    var errorMessage = LibSQLHelper.GetErrorMessage(errorMsg);
                    LibSQLNative.libsql_free_error_msg(errorMsg);
                    throw new InvalidOperationException($"Failed to get scalar value: {errorMessage}");
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

        var connectionHandle = Connection!.ConnectionHandle!;
        IntPtr rowsHandle;
        IntPtr errorMsg;
        int result;

        if (_isPrepared && _preparedStatement != null)
        {
            // Bind parameters to prepared statement
            BindParameters(_preparedStatement);
            
            // Query using prepared statement
            result = LibSQLNative.libsql_query_stmt(_preparedStatement, out rowsHandle, out errorMsg);
        }
        else
        {
            // Query directly
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

    #region Async Methods

    /// <summary>
    /// Asynchronously executes the command and returns the number of rows affected.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation. The result contains the number of rows affected.</returns>
    public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken = default)
    {
        // libSQL doesn't currently support true async operations at the native level
        // For now, we'll run the synchronous version on a task with timeout support
        var timeoutToken = CommandTimeout > 0 ? 
            new CancellationTokenSource(TimeSpan.FromSeconds(CommandTimeout)).Token :
            CancellationToken.None;
        
        var combinedToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutToken).Token;
        
        return Task.Run(() =>
        {
            combinedToken.ThrowIfCancellationRequested();
            return ExecuteNonQuery();
        }, combinedToken);
    }

    /// <summary>
    /// Asynchronously executes the command and returns the first column of the first row in the result set.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation. The result contains the first column of the first row in the result set.</returns>
    public override Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken = default)
    {
        // libSQL doesn't currently support true async operations at the native level
        // For now, we'll run the synchronous version on a task with timeout support
        var timeoutToken = CommandTimeout > 0 ? 
            new CancellationTokenSource(TimeSpan.FromSeconds(CommandTimeout)).Token :
            CancellationToken.None;
        
        var combinedToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutToken).Token;
        
        return Task.Run(() =>
        {
            combinedToken.ThrowIfCancellationRequested();
            return ExecuteScalar();
        }, combinedToken);
    }

    /// <summary>
    /// Asynchronously executes the command and returns a <see cref="DbDataReader"/>.
    /// </summary>
    /// <param name="behavior">One of the <see cref="CommandBehavior"/> values.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation. The result contains a <see cref="DbDataReader"/> object.</returns>
    protected override Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
    {
        // libSQL doesn't currently support true async operations at the native level
        // For now, we'll run the synchronous version on a task with timeout support
        var timeoutToken = CommandTimeout > 0 ? 
            new CancellationTokenSource(TimeSpan.FromSeconds(CommandTimeout)).Token :
            CancellationToken.None;
        
        var combinedToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutToken).Token;
        
        return Task.Run<DbDataReader>(() =>
        {
            combinedToken.ThrowIfCancellationRequested();
            return ExecuteReader(behavior);
        }, combinedToken);
    }

    #endregion

    /// <summary>
    /// Releases the unmanaged resources used by the <see cref="LibSQLCommand"/> and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ReleasePreparedStatement();
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
}