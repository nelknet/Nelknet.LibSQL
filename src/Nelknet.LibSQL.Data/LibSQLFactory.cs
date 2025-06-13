using System;
using System.Data.Common;

namespace Nelknet.LibSQL.Data;

/// <summary>
/// Represents a set of methods for creating instances of the libSQL data provider's implementation of the data source classes.
/// </summary>
public sealed class LibSQLFactory : DbProviderFactory
{
    /// <summary>
    /// The provider invariant name for libSQL.
    /// </summary>
    public const string ProviderInvariantName = "Nelknet.LibSQL.Data";

    /// <summary>
    /// Gets the singleton instance of the <see cref="LibSQLFactory"/>.
    /// </summary>
    public static readonly LibSQLFactory Instance = new();

    /// <summary>
    /// Private constructor to enforce singleton pattern.
    /// </summary>
    private LibSQLFactory()
    {
    }

    /// <summary>
    /// Gets a value indicating whether the factory can create a <see cref="DbCommandBuilder"/>.
    /// </summary>
    public override bool CanCreateCommandBuilder => false; // Not implemented yet

    /// <summary>
    /// Gets a value indicating whether the factory can create a <see cref="DbDataAdapter"/>.
    /// </summary>
    public override bool CanCreateDataAdapter => false; // Not implemented yet

    /// <summary>
    /// Gets a value indicating whether the factory can create a <see cref="DbDataSourceEnumerator"/>.
    /// </summary>
    public override bool CanCreateDataSourceEnumerator => false; // Not implemented

    /// <summary>
    /// Returns a new instance of the provider's class that implements the <see cref="DbCommand"/> class.
    /// </summary>
    /// <returns>A new instance of <see cref="LibSQLCommand"/>.</returns>
    public override DbCommand CreateCommand()
    {
        return new LibSQLCommand();
    }

    /// <summary>
    /// Returns a new instance of the provider's class that implements the <see cref="DbCommandBuilder"/> class.
    /// </summary>
    /// <returns>This method is not supported.</returns>
    /// <exception cref="NotSupportedException">Always thrown.</exception>
    public override DbCommandBuilder CreateCommandBuilder()
    {
        throw new NotSupportedException("CommandBuilder is not yet implemented for libSQL.");
    }

    /// <summary>
    /// Returns a new instance of the provider's class that implements the <see cref="DbConnection"/> class.
    /// </summary>
    /// <returns>A new instance of <see cref="LibSQLConnection"/>.</returns>
    public override DbConnection CreateConnection()
    {
        return new LibSQLConnection();
    }

    /// <summary>
    /// Returns a new instance of the provider's class that implements the <see cref="DbConnectionStringBuilder"/> class.
    /// </summary>
    /// <returns>A new instance of <see cref="LibSQLConnectionStringBuilder"/>.</returns>
    public override DbConnectionStringBuilder CreateConnectionStringBuilder()
    {
        return new LibSQLConnectionStringBuilder();
    }

    /// <summary>
    /// Returns a new instance of the provider's class that implements the <see cref="DbDataAdapter"/> class.
    /// </summary>
    /// <returns>This method is not supported.</returns>
    /// <exception cref="NotSupportedException">Always thrown.</exception>
    public override DbDataAdapter CreateDataAdapter()
    {
        throw new NotSupportedException("DataAdapter is not yet implemented for libSQL.");
    }

    /// <summary>
    /// Returns a new instance of the provider's class that implements the <see cref="DbParameter"/> class.
    /// </summary>
    /// <returns>A new instance of <see cref="LibSQLParameter"/>.</returns>
    public override DbParameter CreateParameter()
    {
        return new LibSQLParameter();
    }

    /// <summary>
    /// Registers the libSQL provider factory with <see cref="DbProviderFactories"/>.
    /// </summary>
    public static void RegisterFactory()
    {
        DbProviderFactories.RegisterFactory(ProviderInvariantName, Instance);
    }

    /// <summary>
    /// Unregisters the libSQL provider factory from <see cref="DbProviderFactories"/>.
    /// </summary>
    public static void UnregisterFactory()
    {
        DbProviderFactories.UnregisterFactory(ProviderInvariantName);
    }
}