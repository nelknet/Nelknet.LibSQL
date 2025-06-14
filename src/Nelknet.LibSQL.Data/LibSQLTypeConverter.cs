#nullable disable warnings

using System;
using System.Data;
using System.Globalization;
using System.Text;
using Nelknet.LibSQL.Bindings;

namespace Nelknet.LibSQL.Data;

/// <summary>
/// Provides conversion utilities between .NET types and libSQL values.
/// </summary>
internal static class LibSQLTypeConverter
{
    private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss.fff";
    private const string DateTimeOffsetFormat = "yyyy-MM-dd HH:mm:ss.fffzzz";
    private const string DateOnlyFormat = "yyyy-MM-dd";
    private const string TimeOnlyFormat = "HH:mm:ss.fff";
    private const string TimeSpanFormat = @"d\.hh\:mm\:ss\.fff";

    /// <summary>
    /// Converts a .NET value to its libSQL representation for parameter binding.
    /// </summary>
    /// <param name="value">The .NET value to convert.</param>
    /// <param name="targetType">The target libSQL type.</param>
    /// <returns>The converted value suitable for libSQL.</returns>
    public static object ConvertToLibSQL(object value, LibSQLDbType targetType)
    {
        if (value == null || value == DBNull.Value)
            return DBNull.Value;

        return targetType switch
        {
            LibSQLDbType.Integer => ConvertToInteger(value),
            LibSQLDbType.Real => ConvertToReal(value),
            LibSQLDbType.Text => ConvertToText(value),
            LibSQLDbType.Blob => ConvertToBlob(value),
            LibSQLDbType.Null => DBNull.Value,
            _ => throw new ArgumentException($"Unsupported libSQL type: {targetType}")
        };
    }

    /// <summary>
    /// Converts a libSQL value to the specified .NET type.
    /// </summary>
    /// <param name="value">The libSQL value.</param>
    /// <param name="targetType">The target .NET type.</param>
    /// <returns>The converted .NET value.</returns>
    public static object ConvertFromLibSQL(object value, Type targetType)
    {
        if (value == null || value == DBNull.Value)
        {
            if (targetType.IsValueType && Nullable.GetUnderlyingType(targetType) == null)
                throw new InvalidOperationException($"Cannot convert NULL to non-nullable type {targetType.Name}");
            return null;
        }

        // Handle nullable types
        var underlyingType = Nullable.GetUnderlyingType(targetType);
        if (underlyingType != null)
            targetType = underlyingType;

        return targetType switch
        {
            _ when targetType == typeof(bool) => ConvertToBoolean(value),
            _ when targetType == typeof(byte) => ConvertToByte(value),
            _ when targetType == typeof(sbyte) => ConvertToSByte(value),
            _ when targetType == typeof(short) => ConvertToInt16(value),
            _ when targetType == typeof(ushort) => ConvertToUInt16(value),
            _ when targetType == typeof(int) => ConvertToInt32(value),
            _ when targetType == typeof(uint) => ConvertToUInt32(value),
            _ when targetType == typeof(long) => ConvertToInt64(value),
            _ when targetType == typeof(ulong) => ConvertToUInt64(value),
            _ when targetType == typeof(float) => ConvertToSingle(value),
            _ when targetType == typeof(double) => ConvertToDouble(value),
            _ when targetType == typeof(decimal) => ConvertToDecimal(value),
            _ when targetType == typeof(string) => ConvertToString(value),
            _ when targetType == typeof(char) => ConvertToChar(value),
            _ when targetType == typeof(Guid) => ConvertToGuid(value),
            _ when targetType == typeof(DateTime) => ConvertToDateTime(value),
            _ when targetType == typeof(DateTimeOffset) => ConvertToDateTimeOffset(value),
            _ when targetType == typeof(TimeSpan) => ConvertToTimeSpan(value),
#if NET6_0_OR_GREATER
            _ when targetType == typeof(DateOnly) => ConvertToDateOnly(value),
            _ when targetType == typeof(TimeOnly) => ConvertToTimeOnly(value),
#endif
            _ when targetType == typeof(byte[]) => ConvertToByteArray(value),
            _ => Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture)
        };
    }

    #region Convert To LibSQL Types

    private static long ConvertToInteger(object value)
    {
        return value switch
        {
            bool b => b ? 1L : 0L,
            byte b => b,
            sbyte sb => sb,
            short s => s,
            ushort us => us,
            int i => i,
            uint ui => ui,
            long l => l,
            ulong ul => checked((long)ul),
            float f => checked((long)f),
            double d => checked((long)d),
            decimal dec => checked((long)dec),
            string str => long.Parse(str, CultureInfo.InvariantCulture),
            _ => Convert.ToInt64(value, CultureInfo.InvariantCulture)
        };
    }

    private static double ConvertToReal(object value)
    {
        return value switch
        {
            float f => f,
            double d => d,
            decimal dec => (double)dec,
            byte b => b,
            sbyte sb => sb,
            short s => s,
            ushort us => us,
            int i => i,
            uint ui => ui,
            long l => l,
            ulong ul => ul,
            string str => double.Parse(str, CultureInfo.InvariantCulture),
            _ => Convert.ToDouble(value, CultureInfo.InvariantCulture)
        };
    }

    private static string ConvertToText(object value)
    {
        return value switch
        {
            string str => str,
            char c => c.ToString(),
            Guid g => g.ToString(),
            DateTime dt => dt.ToString(DateTimeFormat, CultureInfo.InvariantCulture),
            DateTimeOffset dto => dto.ToString(DateTimeOffsetFormat, CultureInfo.InvariantCulture),
            TimeSpan ts => ts.ToString(TimeSpanFormat, CultureInfo.InvariantCulture),
#if NET6_0_OR_GREATER
            DateOnly d => d.ToString(DateOnlyFormat, CultureInfo.InvariantCulture),
            TimeOnly t => t.ToString(TimeOnlyFormat, CultureInfo.InvariantCulture),
#endif
            _ => value.ToString()
        };
    }

    private static byte[] ConvertToBlob(object value)
    {
        return value switch
        {
            byte[] bytes => bytes,
            string str => Encoding.UTF8.GetBytes(str),
            _ => throw new InvalidOperationException($"Cannot convert {value.GetType().Name} to BLOB")
        };
    }

    #endregion

    #region Convert From LibSQL Types

    private static bool ConvertToBoolean(object value)
    {
        return value switch
        {
            bool b => b,
            long l => l != 0,
            double d => d != 0.0,
            string str => bool.Parse(str),
            _ => Convert.ToBoolean(value, CultureInfo.InvariantCulture)
        };
    }

    private static byte ConvertToByte(object value)
    {
        return value switch
        {
            long l => checked((byte)l),
            double d => checked((byte)d),
            string str => byte.Parse(str, CultureInfo.InvariantCulture),
            _ => Convert.ToByte(value, CultureInfo.InvariantCulture)
        };
    }

    private static sbyte ConvertToSByte(object value)
    {
        return value switch
        {
            long l => checked((sbyte)l),
            double d => checked((sbyte)d),
            string str => sbyte.Parse(str, CultureInfo.InvariantCulture),
            _ => Convert.ToSByte(value, CultureInfo.InvariantCulture)
        };
    }

    private static short ConvertToInt16(object value)
    {
        return value switch
        {
            long l => checked((short)l),
            double d => checked((short)d),
            string str => short.Parse(str, CultureInfo.InvariantCulture),
            _ => Convert.ToInt16(value, CultureInfo.InvariantCulture)
        };
    }

    private static ushort ConvertToUInt16(object value)
    {
        return value switch
        {
            long l => checked((ushort)l),
            double d => checked((ushort)d),
            string str => ushort.Parse(str, CultureInfo.InvariantCulture),
            _ => Convert.ToUInt16(value, CultureInfo.InvariantCulture)
        };
    }

    private static int ConvertToInt32(object value)
    {
        return value switch
        {
            long l => checked((int)l),
            double d => checked((int)d),
            string str => int.Parse(str, CultureInfo.InvariantCulture),
            _ => Convert.ToInt32(value, CultureInfo.InvariantCulture)
        };
    }

    private static uint ConvertToUInt32(object value)
    {
        return value switch
        {
            long l => checked((uint)l),
            double d => checked((uint)d),
            string str => uint.Parse(str, CultureInfo.InvariantCulture),
            _ => Convert.ToUInt32(value, CultureInfo.InvariantCulture)
        };
    }

    private static long ConvertToInt64(object value)
    {
        return value switch
        {
            long l => l,
            double d => checked((long)d),
            string str => long.Parse(str, CultureInfo.InvariantCulture),
            _ => Convert.ToInt64(value, CultureInfo.InvariantCulture)
        };
    }

    private static ulong ConvertToUInt64(object value)
    {
        return value switch
        {
            long l => checked((ulong)l),
            double d => checked((ulong)d),
            string str => ulong.Parse(str, CultureInfo.InvariantCulture),
            _ => Convert.ToUInt64(value, CultureInfo.InvariantCulture)
        };
    }

    private static float ConvertToSingle(object value)
    {
        return value switch
        {
            double d => checked((float)d),
            long l => l,
            string str => float.Parse(str, CultureInfo.InvariantCulture),
            _ => Convert.ToSingle(value, CultureInfo.InvariantCulture)
        };
    }

    private static double ConvertToDouble(object value)
    {
        return value switch
        {
            double d => d,
            long l => l,
            string str => double.Parse(str, CultureInfo.InvariantCulture),
            _ => Convert.ToDouble(value, CultureInfo.InvariantCulture)
        };
    }

    private static decimal ConvertToDecimal(object value)
    {
        return value switch
        {
            double d => (decimal)d,
            long l => l,
            string str => decimal.Parse(str, CultureInfo.InvariantCulture),
            _ => Convert.ToDecimal(value, CultureInfo.InvariantCulture)
        };
    }

    private static string ConvertToString(object value)
    {
        return value switch
        {
            string str => str,
            byte[] bytes => Encoding.UTF8.GetString(bytes),
            _ => value.ToString()
        };
    }

    private static char ConvertToChar(object value)
    {
        return value switch
        {
            string str when str.Length == 1 => str[0],
            long l when l >= 0 && l <= char.MaxValue => (char)l,
            _ => Convert.ToChar(value, CultureInfo.InvariantCulture)
        };
    }

    private static Guid ConvertToGuid(object value)
    {
        return value switch
        {
            string str => Guid.Parse(str),
            byte[] bytes when bytes.Length == 16 => new Guid(bytes),
            _ => (Guid)value
        };
    }

    private static DateTime ConvertToDateTime(object value)
    {
        return value switch
        {
            string str => DateTime.ParseExact(str, DateTimeFormat, CultureInfo.InvariantCulture),
            long ticks => new DateTime(ticks),
            _ => Convert.ToDateTime(value, CultureInfo.InvariantCulture)
        };
    }

    private static DateTimeOffset ConvertToDateTimeOffset(object value)
    {
        return value switch
        {
            string str => DateTimeOffset.ParseExact(str, DateTimeOffsetFormat, CultureInfo.InvariantCulture),
            long ticks => new DateTimeOffset(ticks, TimeSpan.Zero),
            _ => (DateTimeOffset)value
        };
    }

    private static TimeSpan ConvertToTimeSpan(object value)
    {
        return value switch
        {
            string str => TimeSpan.ParseExact(str, TimeSpanFormat, CultureInfo.InvariantCulture),
            long ticks => new TimeSpan(ticks),
            _ => (TimeSpan)value
        };
    }

#if NET6_0_OR_GREATER
    private static DateOnly ConvertToDateOnly(object value)
    {
        return value switch
        {
            string str => DateOnly.ParseExact(str, DateOnlyFormat, CultureInfo.InvariantCulture),
            _ => (DateOnly)value
        };
    }

    private static TimeOnly ConvertToTimeOnly(object value)
    {
        return value switch
        {
            string str => TimeOnly.ParseExact(str, TimeOnlyFormat, CultureInfo.InvariantCulture),
            _ => (TimeOnly)value
        };
    }
#endif

    private static byte[] ConvertToByteArray(object value)
    {
        return value switch
        {
            byte[] bytes => bytes,
            string str => Encoding.UTF8.GetBytes(str),
            _ => throw new InvalidOperationException($"Cannot convert {value.GetType().Name} to byte array")
        };
    }

    #endregion

    /// <summary>
    /// Gets the libSQL column type name for a LibSQLDbType.
    /// </summary>
    /// <param name="type">The libSQL database type.</param>
    /// <returns>The column type name.</returns>
    public static string GetColumnTypeName(LibSQLDbType type)
    {
        return type switch
        {
            LibSQLDbType.Integer => "INTEGER",
            LibSQLDbType.Real => "REAL",
            LibSQLDbType.Text => "TEXT",
            LibSQLDbType.Blob => "BLOB",
            LibSQLDbType.Null => "NULL",
            _ => "TEXT"
        };
    }

    /// <summary>
    /// Determines if a value represents NULL.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <returns>True if the value represents NULL.</returns>
    public static bool IsNull(object value)
    {
        return value == null || value == DBNull.Value;
    }
}