#nullable disable warnings

namespace Nelknet.LibSQL.Data;

/// <summary>
/// Represents the libSQL data types that correspond to SQLite data types.
/// </summary>
public enum LibSQLDbType
{
    /// <summary>
    /// Integer type (64-bit signed integer)
    /// </summary>
    Integer,

    /// <summary>
    /// Real/Floating point type (64-bit IEEE floating point number)
    /// </summary>
    Real,

    /// <summary>
    /// Text type (UTF-8 encoded string)
    /// </summary>
    Text,

    /// <summary>
    /// BLOB type (Binary Large Object - byte array)
    /// </summary>
    Blob,

    /// <summary>
    /// NULL type
    /// </summary>
    Null
}