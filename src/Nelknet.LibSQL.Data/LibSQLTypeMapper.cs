#nullable disable warnings

using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Globalization;

namespace Nelknet.LibSQL.Data;

/// <summary>
/// Provides type mapping between .NET types and libSQL types.
/// </summary>
internal static class LibSQLTypeMapper
{
    /// <summary>
    /// Maps .NET types to libSQL database types.
    /// </summary>
    private static readonly Dictionary<Type, LibSQLDbType> NetTypeToLibSQLType = new()
    {
        // Integer types
        { typeof(bool), LibSQLDbType.Integer },
        { typeof(byte), LibSQLDbType.Integer },
        { typeof(sbyte), LibSQLDbType.Integer },
        { typeof(short), LibSQLDbType.Integer },
        { typeof(ushort), LibSQLDbType.Integer },
        { typeof(int), LibSQLDbType.Integer },
        { typeof(uint), LibSQLDbType.Integer },
        { typeof(long), LibSQLDbType.Integer },
        { typeof(ulong), LibSQLDbType.Integer },

        // Floating point types
        { typeof(float), LibSQLDbType.Real },
        { typeof(double), LibSQLDbType.Real },
        { typeof(decimal), LibSQLDbType.Real },

        // Text types
        { typeof(string), LibSQLDbType.Text },
        { typeof(char), LibSQLDbType.Text },
        { typeof(Guid), LibSQLDbType.Text },

        // Date/Time types (stored as text in ISO format)
        { typeof(DateTime), LibSQLDbType.Text },
        { typeof(DateTimeOffset), LibSQLDbType.Text },
        { typeof(TimeSpan), LibSQLDbType.Text },
#if NET6_0_OR_GREATER
        { typeof(DateOnly), LibSQLDbType.Text },
        { typeof(TimeOnly), LibSQLDbType.Text },
#endif

        // Binary types
        { typeof(byte[]), LibSQLDbType.Blob },

        // Nullable variants
        { typeof(bool?), LibSQLDbType.Integer },
        { typeof(byte?), LibSQLDbType.Integer },
        { typeof(sbyte?), LibSQLDbType.Integer },
        { typeof(short?), LibSQLDbType.Integer },
        { typeof(ushort?), LibSQLDbType.Integer },
        { typeof(int?), LibSQLDbType.Integer },
        { typeof(uint?), LibSQLDbType.Integer },
        { typeof(long?), LibSQLDbType.Integer },
        { typeof(ulong?), LibSQLDbType.Integer },
        { typeof(float?), LibSQLDbType.Real },
        { typeof(double?), LibSQLDbType.Real },
        { typeof(decimal?), LibSQLDbType.Real },
        { typeof(char?), LibSQLDbType.Text },
        { typeof(Guid?), LibSQLDbType.Text },
        { typeof(DateTime?), LibSQLDbType.Text },
        { typeof(DateTimeOffset?), LibSQLDbType.Text },
        { typeof(TimeSpan?), LibSQLDbType.Text },
#if NET6_0_OR_GREATER
        { typeof(DateOnly?), LibSQLDbType.Text },
        { typeof(TimeOnly?), LibSQLDbType.Text },
#endif
    };

    /// <summary>
    /// Maps DbType enumeration to libSQL database types.
    /// </summary>
    private static readonly Dictionary<DbType, LibSQLDbType> DbTypeToLibSQLType = new()
    {
        // Integer types
        { DbType.Boolean, LibSQLDbType.Integer },
        { DbType.Byte, LibSQLDbType.Integer },
        { DbType.SByte, LibSQLDbType.Integer },
        { DbType.Int16, LibSQLDbType.Integer },
        { DbType.UInt16, LibSQLDbType.Integer },
        { DbType.Int32, LibSQLDbType.Integer },
        { DbType.UInt32, LibSQLDbType.Integer },
        { DbType.Int64, LibSQLDbType.Integer },
        { DbType.UInt64, LibSQLDbType.Integer },

        // Floating point types
        { DbType.Single, LibSQLDbType.Real },
        { DbType.Double, LibSQLDbType.Real },
        { DbType.Decimal, LibSQLDbType.Real },
        { DbType.Currency, LibSQLDbType.Real },
        { DbType.VarNumeric, LibSQLDbType.Real },

        // Text types
        { DbType.String, LibSQLDbType.Text },
        { DbType.StringFixedLength, LibSQLDbType.Text },
        { DbType.AnsiString, LibSQLDbType.Text },
        { DbType.AnsiStringFixedLength, LibSQLDbType.Text },
        { DbType.Guid, LibSQLDbType.Text },
        { DbType.Xml, LibSQLDbType.Text },

        // Date/Time types
        { DbType.DateTime, LibSQLDbType.Text },
        { DbType.DateTime2, LibSQLDbType.Text },
        { DbType.DateTimeOffset, LibSQLDbType.Text },
        { DbType.Date, LibSQLDbType.Text },
        { DbType.Time, LibSQLDbType.Text },

        // Binary types
        { DbType.Binary, LibSQLDbType.Blob },
        { DbType.Object, LibSQLDbType.Blob }
    };

    /// <summary>
    /// Gets the libSQL database type for a .NET type.
    /// </summary>
    /// <param name="type">The .NET type.</param>
    /// <returns>The corresponding libSQL database type.</returns>
    public static LibSQLDbType GetLibSQLType(Type type)
    {
        if (type == null)
            return LibSQLDbType.Null;

        // Handle nullable types
        var underlyingType = Nullable.GetUnderlyingType(type);
        if (underlyingType != null)
            type = underlyingType;

        if (NetTypeToLibSQLType.TryGetValue(type, out var libSQLType))
            return libSQLType;

        // Default to text for unknown types (they'll be converted to string)
        return LibSQLDbType.Text;
    }

    /// <summary>
    /// Gets the libSQL database type for a DbType.
    /// </summary>
    /// <param name="dbType">The DbType.</param>
    /// <returns>The corresponding libSQL database type.</returns>
    public static LibSQLDbType GetLibSQLType(DbType dbType)
    {
        if (DbTypeToLibSQLType.TryGetValue(dbType, out var libSQLType))
            return libSQLType;

        // Default to text for unknown DbTypes
        return LibSQLDbType.Text;
    }

    /// <summary>
    /// Gets the appropriate DbType for a .NET type.
    /// </summary>
    /// <param name="type">The .NET type.</param>
    /// <returns>The corresponding DbType.</returns>
    public static DbType GetDbType(Type type)
    {
        if (type == null)
            return DbType.Object;

        // Handle nullable types
        var underlyingType = Nullable.GetUnderlyingType(type);
        if (underlyingType != null)
            type = underlyingType;

        return type switch
        {
            _ when type == typeof(bool) => DbType.Boolean,
            _ when type == typeof(byte) => DbType.Byte,
            _ when type == typeof(sbyte) => DbType.SByte,
            _ when type == typeof(short) => DbType.Int16,
            _ when type == typeof(ushort) => DbType.UInt16,
            _ when type == typeof(int) => DbType.Int32,
            _ when type == typeof(uint) => DbType.UInt32,
            _ when type == typeof(long) => DbType.Int64,
            _ when type == typeof(ulong) => DbType.UInt64,
            _ when type == typeof(float) => DbType.Single,
            _ when type == typeof(double) => DbType.Double,
            _ when type == typeof(decimal) => DbType.Decimal,
            _ when type == typeof(string) => DbType.String,
            _ when type == typeof(char) => DbType.StringFixedLength,
            _ when type == typeof(Guid) => DbType.Guid,
            _ when type == typeof(DateTime) => DbType.DateTime,
            _ when type == typeof(DateTimeOffset) => DbType.DateTimeOffset,
            _ when type == typeof(TimeSpan) => DbType.Time,
#if NET6_0_OR_GREATER
            _ when type == typeof(DateOnly) => DbType.Date,
            _ when type == typeof(TimeOnly) => DbType.Time,
#endif
            _ when type == typeof(byte[]) => DbType.Binary,
            _ => DbType.Object
        };
    }
}