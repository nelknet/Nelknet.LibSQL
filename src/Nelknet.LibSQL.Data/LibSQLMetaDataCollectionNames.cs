namespace Nelknet.LibSQL.Data;

/// <summary>
/// Defines the names of metadata collections for libSQL schema discovery.
/// </summary>
internal static class LibSQLMetaDataCollectionNames
{
    /// <summary>
    /// The collection of tables.
    /// </summary>
    public const string Tables = "Tables";
    
    /// <summary>
    /// The collection of columns.
    /// </summary>
    public const string Columns = "Columns";
    
    /// <summary>
    /// The collection of views.
    /// </summary>
    public const string Views = "Views";
    
    /// <summary>
    /// The collection of indexes.
    /// </summary>
    public const string Indexes = "Indexes";
    
    /// <summary>
    /// The collection of triggers.
    /// </summary>
    public const string Triggers = "Triggers";
}