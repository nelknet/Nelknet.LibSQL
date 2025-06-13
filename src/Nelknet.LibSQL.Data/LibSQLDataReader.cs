using System;
using System.Collections;
using System.Data;
using System.Data.Common;

namespace Nelknet.LibSQL.Data;

/// <summary>
/// Provides a way of reading a forward-only stream of rows from a libSQL database.
/// </summary>
public sealed class LibSQLDataReader : DbDataReader
{
    /// <summary>
    /// Gets the depth of nesting for the current row.
    /// </summary>
    public override int Depth => 0;

    /// <summary>
    /// Gets the number of columns in the current row.
    /// </summary>
    public override int FieldCount => throw new NotImplementedException("LibSQLDataReader will be implemented in Phase 7.");

    /// <summary>
    /// Gets a value indicating whether the data reader contains one or more rows.
    /// </summary>
    public override bool HasRows => throw new NotImplementedException("LibSQLDataReader will be implemented in Phase 7.");

    /// <summary>
    /// Gets a value indicating whether the data reader is closed.
    /// </summary>
    public override bool IsClosed => throw new NotImplementedException("LibSQLDataReader will be implemented in Phase 7.");

    /// <summary>
    /// Gets the number of rows changed, inserted, or deleted by execution of the SQL statement.
    /// </summary>
    public override int RecordsAffected => throw new NotImplementedException("LibSQLDataReader will be implemented in Phase 7.");

    /// <summary>
    /// Gets the value of the specified column as an instance of Object.
    /// </summary>
    /// <param name="ordinal">The zero-based column ordinal.</param>
    /// <returns>The value of the specified column.</returns>
    public override object this[int ordinal] => throw new NotImplementedException("LibSQLDataReader will be implemented in Phase 7.");

    /// <summary>
    /// Gets the value of the specified column as an instance of Object.
    /// </summary>
    /// <param name="name">The name of the column.</param>
    /// <returns>The value of the specified column.</returns>
    public override object this[string name] => throw new NotImplementedException("LibSQLDataReader will be implemented in Phase 7.");

    /// <summary>
    /// Closes the data reader.
    /// </summary>
    public override void Close()
    {
        throw new NotImplementedException("LibSQLDataReader will be implemented in Phase 7.");
    }

    /// <summary>
    /// Gets the value of the specified column as a Boolean.
    /// </summary>
    /// <param name="ordinal">The zero-based column ordinal.</param>
    /// <returns>The value of the specified column.</returns>
    public override bool GetBoolean(int ordinal)
    {
        throw new NotImplementedException("LibSQLDataReader will be implemented in Phase 7.");
    }

    /// <summary>
    /// Gets the value of the specified column as a byte.
    /// </summary>
    /// <param name="ordinal">The zero-based column ordinal.</param>
    /// <returns>The value of the specified column.</returns>
    public override byte GetByte(int ordinal)
    {
        throw new NotImplementedException("LibSQLDataReader will be implemented in Phase 7.");
    }

    /// <summary>
    /// Reads a stream of bytes from the specified column.
    /// </summary>
    /// <param name="ordinal">The zero-based column ordinal.</param>
    /// <param name="dataOffset">The index within the field from which to start the read operation.</param>
    /// <param name="buffer">The buffer into which to copy the data.</param>
    /// <param name="bufferOffset">The index within the buffer to start the copy operation.</param>
    /// <param name="length">The maximum number of bytes to read.</param>
    /// <returns>The actual number of bytes read.</returns>
    public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length)
    {
        throw new NotImplementedException("LibSQLDataReader will be implemented in Phase 7.");
    }

    /// <summary>
    /// Gets the value of the specified column as a single character.
    /// </summary>
    /// <param name="ordinal">The zero-based column ordinal.</param>
    /// <returns>The value of the specified column.</returns>
    public override char GetChar(int ordinal)
    {
        throw new NotImplementedException("LibSQLDataReader will be implemented in Phase 7.");
    }

    /// <summary>
    /// Reads a stream of characters from the specified column.
    /// </summary>
    /// <param name="ordinal">The zero-based column ordinal.</param>
    /// <param name="dataOffset">The index within the field from which to start the read operation.</param>
    /// <param name="buffer">The buffer into which to copy the data.</param>
    /// <param name="bufferOffset">The index within the buffer to start the copy operation.</param>
    /// <param name="length">The maximum number of characters to read.</param>
    /// <returns>The actual number of characters read.</returns>
    public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length)
    {
        throw new NotImplementedException("LibSQLDataReader will be implemented in Phase 7.");
    }

    /// <summary>
    /// Gets the name of the data type of the specified column.
    /// </summary>
    /// <param name="ordinal">The zero-based column ordinal.</param>
    /// <returns>The name of the data type.</returns>
    public override string GetDataTypeName(int ordinal)
    {
        throw new NotImplementedException("LibSQLDataReader will be implemented in Phase 7.");
    }

    /// <summary>
    /// Gets the value of the specified column as a DateTime.
    /// </summary>
    /// <param name="ordinal">The zero-based column ordinal.</param>
    /// <returns>The value of the specified column.</returns>
    public override DateTime GetDateTime(int ordinal)
    {
        throw new NotImplementedException("LibSQLDataReader will be implemented in Phase 7.");
    }

    /// <summary>
    /// Gets the value of the specified column as a Decimal.
    /// </summary>
    /// <param name="ordinal">The zero-based column ordinal.</param>
    /// <returns>The value of the specified column.</returns>
    public override decimal GetDecimal(int ordinal)
    {
        throw new NotImplementedException("LibSQLDataReader will be implemented in Phase 7.");
    }

    /// <summary>
    /// Gets the value of the specified column as a double-precision floating point number.
    /// </summary>
    /// <param name="ordinal">The zero-based column ordinal.</param>
    /// <returns>The value of the specified column.</returns>
    public override double GetDouble(int ordinal)
    {
        throw new NotImplementedException("LibSQLDataReader will be implemented in Phase 7.");
    }

    /// <summary>
    /// Returns an IEnumerator that can be used to iterate through the rows in the data reader.
    /// </summary>
    /// <returns>An IEnumerator that can be used to iterate through the rows in the data reader.</returns>
    public override IEnumerator GetEnumerator()
    {
        throw new NotImplementedException("LibSQLDataReader will be implemented in Phase 7.");
    }

    /// <summary>
    /// Gets the data type of the specified column.
    /// </summary>
    /// <param name="ordinal">The zero-based column ordinal.</param>
    /// <returns>The data type of the specified column.</returns>
    public override Type GetFieldType(int ordinal)
    {
        throw new NotImplementedException("LibSQLDataReader will be implemented in Phase 7.");
    }

    /// <summary>
    /// Gets the value of the specified column as a single-precision floating point number.
    /// </summary>
    /// <param name="ordinal">The zero-based column ordinal.</param>
    /// <returns>The value of the specified column.</returns>
    public override float GetFloat(int ordinal)
    {
        throw new NotImplementedException("LibSQLDataReader will be implemented in Phase 7.");
    }

    /// <summary>
    /// Gets the value of the specified column as a globally-unique identifier (GUID).
    /// </summary>
    /// <param name="ordinal">The zero-based column ordinal.</param>
    /// <returns>The value of the specified column.</returns>
    public override Guid GetGuid(int ordinal)
    {
        throw new NotImplementedException("LibSQLDataReader will be implemented in Phase 7.");
    }

    /// <summary>
    /// Gets the value of the specified column as a 16-bit signed integer.
    /// </summary>
    /// <param name="ordinal">The zero-based column ordinal.</param>
    /// <returns>The value of the specified column.</returns>
    public override short GetInt16(int ordinal)
    {
        throw new NotImplementedException("LibSQLDataReader will be implemented in Phase 7.");
    }

    /// <summary>
    /// Gets the value of the specified column as a 32-bit signed integer.
    /// </summary>
    /// <param name="ordinal">The zero-based column ordinal.</param>
    /// <returns>The value of the specified column.</returns>
    public override int GetInt32(int ordinal)
    {
        throw new NotImplementedException("LibSQLDataReader will be implemented in Phase 7.");
    }

    /// <summary>
    /// Gets the value of the specified column as a 64-bit signed integer.
    /// </summary>
    /// <param name="ordinal">The zero-based column ordinal.</param>
    /// <returns>The value of the specified column.</returns>
    public override long GetInt64(int ordinal)
    {
        throw new NotImplementedException("LibSQLDataReader will be implemented in Phase 7.");
    }

    /// <summary>
    /// Gets the name of the column, given the zero-based column ordinal.
    /// </summary>
    /// <param name="ordinal">The zero-based column ordinal.</param>
    /// <returns>The name of the specified column.</returns>
    public override string GetName(int ordinal)
    {
        throw new NotImplementedException("LibSQLDataReader will be implemented in Phase 7.");
    }

    /// <summary>
    /// Gets the column ordinal given the name of the column.
    /// </summary>
    /// <param name="name">The name of the column.</param>
    /// <returns>The zero-based column ordinal.</returns>
    public override int GetOrdinal(string name)
    {
        throw new NotImplementedException("LibSQLDataReader will be implemented in Phase 7.");
    }

    /// <summary>
    /// Gets the value of the specified column as an instance of string.
    /// </summary>
    /// <param name="ordinal">The zero-based column ordinal.</param>
    /// <returns>The value of the specified column.</returns>
    public override string GetString(int ordinal)
    {
        throw new NotImplementedException("LibSQLDataReader will be implemented in Phase 7.");
    }

    /// <summary>
    /// Gets the value of the specified column as an instance of Object.
    /// </summary>
    /// <param name="ordinal">The zero-based column ordinal.</param>
    /// <returns>The value of the specified column.</returns>
    public override object GetValue(int ordinal)
    {
        throw new NotImplementedException("LibSQLDataReader will be implemented in Phase 7.");
    }

    /// <summary>
    /// Populates an array of objects with the column values of the current row.
    /// </summary>
    /// <param name="values">An array of Object into which to copy the attribute columns.</param>
    /// <returns>The number of instances of Object in the array.</returns>
    public override int GetValues(object[] values)
    {
        throw new NotImplementedException("LibSQLDataReader will be implemented in Phase 7.");
    }

    /// <summary>
    /// Gets a value that indicates whether the column contains nonexistent or missing values.
    /// </summary>
    /// <param name="ordinal">The zero-based column ordinal.</param>
    /// <returns>true if the specified column is equivalent to DBNull; otherwise false.</returns>
    public override bool IsDBNull(int ordinal)
    {
        throw new NotImplementedException("LibSQLDataReader will be implemented in Phase 7.");
    }

    /// <summary>
    /// Advances the reader to the next result when reading the results of a batch of statements.
    /// </summary>
    /// <returns>true if there are more result sets; otherwise false.</returns>
    public override bool NextResult()
    {
        throw new NotImplementedException("LibSQLDataReader will be implemented in Phase 7.");
    }

    /// <summary>
    /// Advances the reader to the next record in a result set.
    /// </summary>
    /// <returns>true if there are more rows; otherwise false.</returns>
    public override bool Read()
    {
        throw new NotImplementedException("LibSQLDataReader will be implemented in Phase 7.");
    }
}