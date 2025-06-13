#nullable disable warnings

using System;
using System.Data;
using System.Data.Common;

namespace Nelknet.LibSQL.Data;

/// <summary>
/// Represents a SQL command to execute against a libSQL database.
/// </summary>
public sealed class LibSQLCommand : DbCommand
{
    private LibSQLConnection? _connection;
    private string? _commandText = string.Empty;
    private int _commandTimeout = 30;

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
        set => _commandText = value;
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
    public override CommandType CommandType { get; set; } = CommandType.Text;

    /// <summary>
    /// Gets or sets the <see cref="LibSQLConnection"/> used by this command.
    /// </summary>
    public new LibSQLConnection? Connection
    {
        get => _connection;
        set => _connection = value;
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
        // TODO: Implement ExecuteNonQuery using native calls
        throw new NotImplementedException("ExecuteNonQuery will be implemented in Phase 6.");
    }

    /// <summary>
    /// Executes the command and returns the first column of the first row in the result set.
    /// </summary>
    /// <returns>The first column of the first row in the result set.</returns>
    public override object? ExecuteScalar()
    {
        EnsureConnectionOpen();
        // TODO: Implement ExecuteScalar using native calls
        throw new NotImplementedException("ExecuteScalar will be implemented in Phase 6.");
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
        // TODO: Implement ExecuteReader using native calls
        throw new NotImplementedException("ExecuteReader will be implemented in Phase 7.");
    }

    /// <summary>
    /// Creates a prepared (or compiled) version of the command on the data source.
    /// </summary>
    public override void Prepare()
    {
        EnsureConnectionOpen();
        // TODO: Implement Prepare using native calls
        throw new NotImplementedException("Prepare will be implemented in Phase 6.");
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
}