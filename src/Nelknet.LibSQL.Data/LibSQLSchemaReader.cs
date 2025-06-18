using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace Nelknet.LibSQL.Data;

/// <summary>
/// Provides schema information for libSQL databases.
/// </summary>
internal class LibSQLSchemaReader
{
    private readonly LibSQLConnection _connection;
    
    public LibSQLSchemaReader(LibSQLConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connection = connection;
    }
    
    /// <summary>
    /// Gets the metadata collection names.
    /// </summary>
    public static DataTable GetMetaDataCollections()
    {
        var table = new DataTable("MetaDataCollections");
#pragma warning disable IDISP004 // Don't ignore created IDisposable - DataColumns are owned by DataTable
        table.Columns.Add("CollectionName", typeof(string));
        table.Columns.Add("NumberOfRestrictions", typeof(int));
        table.Columns.Add("NumberOfIdentifierParts", typeof(int));
#pragma warning restore IDISP004
        
        table.Rows.Add(LibSQLMetaDataCollectionNames.Tables, 2, 2);
        table.Rows.Add(LibSQLMetaDataCollectionNames.Columns, 4, 4);
        table.Rows.Add(LibSQLMetaDataCollectionNames.Views, 2, 2);
        table.Rows.Add(LibSQLMetaDataCollectionNames.Indexes, 3, 3);
        table.Rows.Add(LibSQLMetaDataCollectionNames.Triggers, 2, 2);
        
        return table;
    }
    
    /// <summary>
    /// Gets the schema for the specified collection.
    /// </summary>
    public DataTable? GetSchema(string? collectionName, string?[]? restrictionValues)
    {
        if (string.IsNullOrEmpty(collectionName))
            return GetMetaDataCollections();
        
        return collectionName switch
        {
            "MetaDataCollections" => GetMetaDataCollections(),
            LibSQLMetaDataCollectionNames.Tables => GetTables(restrictionValues),
            LibSQLMetaDataCollectionNames.Columns => GetColumns(restrictionValues),
            LibSQLMetaDataCollectionNames.Views => GetViews(restrictionValues),
            LibSQLMetaDataCollectionNames.Indexes => GetIndexes(restrictionValues),
            LibSQLMetaDataCollectionNames.Triggers => GetTriggers(restrictionValues),
            _ => throw new ArgumentException($"Unknown collection name: {collectionName}", nameof(collectionName))
        };
    }
    
    private DataTable GetTables(string?[]? restrictionValues)
    {
        var table = new DataTable(LibSQLMetaDataCollectionNames.Tables);
#pragma warning disable IDISP004 // Don't ignore created IDisposable - DataColumns are owned by DataTable
        table.Columns.Add("TABLE_CATALOG", typeof(string));
        table.Columns.Add("TABLE_SCHEMA", typeof(string));
        table.Columns.Add("TABLE_NAME", typeof(string));
        table.Columns.Add("TABLE_TYPE", typeof(string));
#pragma warning restore IDISP004
        
        var catalog = restrictionValues?.Length > 0 ? restrictionValues[0] : null;
        var tableName = restrictionValues?.Length > 1 ? restrictionValues[1] : null;
        
        var query = "SELECT name, type FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%' AND name NOT LIKE 'libsql_%'";
        if (!string.IsNullOrEmpty(tableName))
            query += $" AND name = '{tableName.Replace("'", "''")}'";
        query += " ORDER BY name";
        
        using var command = _connection.CreateCommand();
        command.CommandText = query;
        using var reader = command.ExecuteReader();
        
        while (reader.Read())
        {
            table.Rows.Add(
                catalog,
                "main",
                reader.GetString(0),
                "TABLE"
            );
        }
        
        return table;
    }
    
    private DataTable GetColumns(string?[]? restrictionValues)
    {
        var table = new DataTable(LibSQLMetaDataCollectionNames.Columns);
#pragma warning disable IDISP004 // Don't ignore created IDisposable - DataColumns are owned by DataTable
        table.Columns.Add("TABLE_CATALOG", typeof(string));
        table.Columns.Add("TABLE_SCHEMA", typeof(string));
        table.Columns.Add("TABLE_NAME", typeof(string));
        table.Columns.Add("COLUMN_NAME", typeof(string));
        table.Columns.Add("ORDINAL_POSITION", typeof(int));
        table.Columns.Add("COLUMN_DEFAULT", typeof(string));
        table.Columns.Add("IS_NULLABLE", typeof(string));
        table.Columns.Add("DATA_TYPE", typeof(string));
        table.Columns.Add("CHARACTER_MAXIMUM_LENGTH", typeof(int));
        table.Columns.Add("NUMERIC_PRECISION", typeof(int));
        table.Columns.Add("NUMERIC_SCALE", typeof(int));
        table.Columns.Add("PRIMARY_KEY", typeof(bool));
        table.Columns.Add("AUTOINCREMENT", typeof(bool));
        table.Columns.Add("UNIQUE", typeof(bool));
#pragma warning restore IDISP004
        
        var catalog = restrictionValues?.Length > 0 ? restrictionValues[0] : null;
        var schema = restrictionValues?.Length > 1 ? restrictionValues[1] : null;
        var tableName = restrictionValues?.Length > 2 ? restrictionValues[2] : null;
        var columnName = restrictionValues?.Length > 3 ? restrictionValues[3] : null;
        
        // Get all tables first (excluding system tables)
        var tablesQuery = "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%' AND name NOT LIKE 'libsql_%'";
        if (!string.IsNullOrEmpty(tableName))
            tablesQuery += $" AND name = '{tableName.Replace("'", "''")}'";
        tablesQuery += " ORDER BY name";
        
        var tables = new List<string>();
        using (var tablesCommand = _connection.CreateCommand())
        {
            tablesCommand.CommandText = tablesQuery;
            using var tablesReader = tablesCommand.ExecuteReader();
            while (tablesReader.Read())
            {
                tables.Add(tablesReader.GetString(0));
            }
        }
        
        // Get column info for each table
        foreach (var tbl in tables)
        {
            using var command = _connection.CreateCommand();
            command.CommandText = $"PRAGMA table_info('{tbl.Replace("'", "''")}')";
            using var reader = command.ExecuteReader();
            
            while (reader.Read())
            {
                var colName = reader.GetString(1);
                if (!string.IsNullOrEmpty(columnName) && colName != columnName)
                    continue;
                
                var isPrimaryKey = reader.GetInt32(5) == 1;
                var isNotNull = reader.GetInt32(3) != 0;
                
                table.Rows.Add(
                    catalog,
                    schema ?? "main",
                    tbl,
                    colName,
                    reader.GetInt32(0), // cid
                    reader.IsDBNull(4) ? DBNull.Value : reader.GetValue(4), // dflt_value
                    (isNotNull || isPrimaryKey) ? "NO" : "YES", // Primary keys are always NOT NULL
                    reader.GetString(2), // type
                    DBNull.Value, // CHARACTER_MAXIMUM_LENGTH
                    DBNull.Value, // NUMERIC_PRECISION
                    DBNull.Value, // NUMERIC_SCALE
                    isPrimaryKey, // pk
                    false, // AUTOINCREMENT - would need to parse SQL
                    false  // UNIQUE - would need to check indexes
                );
            }
        }
        
        return table;
    }
    
    private DataTable GetViews(string?[]? restrictionValues)
    {
        var table = new DataTable(LibSQLMetaDataCollectionNames.Views);
#pragma warning disable IDISP004 // Don't ignore created IDisposable - DataColumns are owned by DataTable
        table.Columns.Add("TABLE_CATALOG", typeof(string));
        table.Columns.Add("TABLE_SCHEMA", typeof(string));
        table.Columns.Add("TABLE_NAME", typeof(string));
        table.Columns.Add("VIEW_DEFINITION", typeof(string));
        table.Columns.Add("CHECK_OPTION", typeof(string));
        table.Columns.Add("IS_UPDATABLE", typeof(string));
#pragma warning restore IDISP004
        
        var catalog = restrictionValues?.Length > 0 ? restrictionValues[0] : null;
        var viewName = restrictionValues?.Length > 1 ? restrictionValues[1] : null;
        
        var query = "SELECT name, sql FROM sqlite_master WHERE type = 'view'";
        if (!string.IsNullOrEmpty(viewName))
            query += $" AND name = '{viewName.Replace("'", "''")}'";
        query += " ORDER BY name";
        
        using var command = _connection.CreateCommand();
        command.CommandText = query;
        using var reader = command.ExecuteReader();
        
        while (reader.Read())
        {
            table.Rows.Add(
                catalog,
                "main",
                reader.GetString(0),
                reader.IsDBNull(1) ? null : reader.GetString(1),
                "NONE",
                "NO"
            );
        }
        
        return table;
    }
    
    private DataTable GetIndexes(string?[]? restrictionValues)
    {
        var table = new DataTable(LibSQLMetaDataCollectionNames.Indexes);
        table.Columns.Add("TABLE_CATALOG", typeof(string));
        table.Columns.Add("TABLE_SCHEMA", typeof(string));
        table.Columns.Add("TABLE_NAME", typeof(string));
        table.Columns.Add("INDEX_NAME", typeof(string));
        table.Columns.Add("PRIMARY_KEY", typeof(bool));
        table.Columns.Add("UNIQUE", typeof(bool));
        table.Columns.Add("CLUSTERED", typeof(bool));
        table.Columns.Add("TYPE", typeof(short));
        table.Columns.Add("FILL_FACTOR", typeof(short));
        table.Columns.Add("INITIAL_SIZE", typeof(int));
        table.Columns.Add("NULLS", typeof(int));
        table.Columns.Add("SORT_BOOKMARKS", typeof(bool));
        table.Columns.Add("AUTO_UPDATE", typeof(bool));
        table.Columns.Add("NULL_COLLATION", typeof(int));
        table.Columns.Add("ORDINAL_POSITION", typeof(int));
        table.Columns.Add("COLUMN_NAME", typeof(string));
        table.Columns.Add("COLUMN_GUID", typeof(Guid));
        table.Columns.Add("COLUMN_PROPID", typeof(int));
        table.Columns.Add("COLLATION", typeof(short));
        table.Columns.Add("CARDINALITY", typeof(long));
        table.Columns.Add("PAGES", typeof(int));
        table.Columns.Add("FILTER_CONDITION", typeof(string));
        table.Columns.Add("INTEGRATED", typeof(bool));
        table.Columns.Add("INDEX_DEFINITION", typeof(string));
        
        var catalog = restrictionValues?.Length > 0 ? restrictionValues[0] : null;
        var tableName = restrictionValues?.Length > 1 ? restrictionValues[1] : null;
        var indexName = restrictionValues?.Length > 2 ? restrictionValues[2] : null;
        
        var query = "SELECT name, tbl_name, sql FROM sqlite_master WHERE type = 'index'";
        if (!string.IsNullOrEmpty(tableName))
            query += $" AND tbl_name = '{tableName.Replace("'", "''")}'";
        if (!string.IsNullOrEmpty(indexName))
            query += $" AND name = '{indexName.Replace("'", "''")}'";
        query += " ORDER BY tbl_name, name";
        
        using var command = _connection.CreateCommand();
        command.CommandText = query;
        using var reader = command.ExecuteReader();
        
        while (reader.Read())
        {
            var idxName = reader.GetString(0);
            var tblName = reader.GetString(1);
            var sql = reader.IsDBNull(2) ? null : reader.GetValue(2)?.ToString();
            
            // Get index info
            using var infoCommand = _connection.CreateCommand();
            infoCommand.CommandText = $"PRAGMA index_info('{idxName.Replace("'", "''")}')";
            using var infoReader = infoCommand.ExecuteReader();
            
            while (infoReader.Read())
            {
                table.Rows.Add(
                    catalog,
                    "main",
                    tblName,
                    idxName,
                    false, // PRIMARY_KEY - would need to check
                    sql?.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) ?? false,
                    false, // CLUSTERED
                    3, // TYPE (3 = BTREE)
                    DBNull.Value, // FILL_FACTOR
                    DBNull.Value, // INITIAL_SIZE
                    DBNull.Value, // NULLS
                    false, // SORT_BOOKMARKS
                    true, // AUTO_UPDATE
                    4, // NULL_COLLATION
                    infoReader.GetInt32(0), // seqno
                    infoReader.IsDBNull(2) ? DBNull.Value : infoReader.GetValue(2), // name
                    DBNull.Value, // COLUMN_GUID
                    DBNull.Value, // COLUMN_PROPID
                    1, // COLLATION (1 = ASC)
                    DBNull.Value, // CARDINALITY
                    DBNull.Value, // PAGES
                    DBNull.Value, // FILTER_CONDITION
                    true, // INTEGRATED
                    sql // INDEX_DEFINITION
                );
            }
        }
        
        return table;
    }
    
    private DataTable GetTriggers(string?[]? restrictionValues)
    {
        var table = new DataTable(LibSQLMetaDataCollectionNames.Triggers);
        table.Columns.Add("TABLE_CATALOG", typeof(string));
        table.Columns.Add("TABLE_SCHEMA", typeof(string));
        table.Columns.Add("TABLE_NAME", typeof(string));
        table.Columns.Add("TRIGGER_NAME", typeof(string));
        table.Columns.Add("TRIGGER_TYPE", typeof(string));
        table.Columns.Add("TRIGGERING_EVENT", typeof(string));
        table.Columns.Add("IS_UPDATE_COLUMNS", typeof(bool));
        table.Columns.Add("TRIGGER_DEFINITION", typeof(string));
        
        var catalog = restrictionValues?.Length > 0 ? restrictionValues[0] : null;
        var tableName = restrictionValues?.Length > 1 ? restrictionValues[1] : null;
        
        var query = "SELECT name, tbl_name, sql FROM sqlite_master WHERE type = 'trigger'";
        if (!string.IsNullOrEmpty(tableName))
            query += $" AND tbl_name = '{tableName.Replace("'", "''")}'";
        query += " ORDER BY tbl_name, name";
        
        using var command = _connection.CreateCommand();
        command.CommandText = query;
        using var reader = command.ExecuteReader();
        
        while (reader.Read())
        {
            var sql = reader.IsDBNull(2) ? null : reader.GetString(2);
            var triggerType = "AFTER"; // Default
            var triggeringEvent = "INSERT"; // Default
            
            if (sql != null)
            {
                var upperSql = sql.ToUpperInvariant();
                if (upperSql.Contains("BEFORE", StringComparison.Ordinal))
                    triggerType = "BEFORE";
                else if (upperSql.Contains("INSTEAD OF", StringComparison.Ordinal))
                    triggerType = "INSTEAD OF";
                
                if (upperSql.Contains("UPDATE", StringComparison.Ordinal))
                    triggeringEvent = "UPDATE";
                else if (upperSql.Contains("DELETE", StringComparison.Ordinal))
                    triggeringEvent = "DELETE";
            }
            
            table.Rows.Add(
                catalog,
                "main",
                reader.GetString(1),
                reader.GetString(0),
                triggerType,
                triggeringEvent,
                false, // IS_UPDATE_COLUMNS
                sql
            );
        }
        
        return table;
    }
}