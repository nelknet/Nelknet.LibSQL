#nullable disable warnings

using Nelknet.LibSQL.Bindings;
using Nelknet.LibSQL.Data.Http;
using System;
using System.Collections;
using System.Data;
using System.Data.Common;
using System.Runtime.InteropServices;

namespace Nelknet.LibSQL.Data;

/// <summary>
/// Provides a way of reading a forward-only stream of rows from a libSQL database.
/// </summary>
public sealed class LibSQLDataReader : DbDataReader
{
    private readonly LibSQLRowsHandle? _rowsHandle;
    private readonly LibSQLHttpDataReader? _httpDataReader;
    private readonly CommandBehavior _behavior;
    private LibSQLRowHandle? _currentRow;
    private bool _disposed;
    private bool _closed;
    private int _fieldCount = -1;
    private string[]? _columnNames;
    private bool _hasInitializedMetadata;
    private bool _isHttpReader;
    /// <summary>
    /// Initializes a new instance of the <see cref="LibSQLDataReader"/> class.
    /// This constructor creates a closed reader for testing purposes.
    /// </summary>
    public LibSQLDataReader()
    {
        _behavior = CommandBehavior.Default;
        _closed = true;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LibSQLDataReader"/> class.
    /// </summary>
    /// <param name="rowsHandle">The handle to the libSQL rows result set.</param>
    /// <param name="behavior">The command behavior that controls the reader.</param>
    internal LibSQLDataReader(LibSQLRowsHandle rowsHandle, CommandBehavior behavior = CommandBehavior.Default)
    {
        _rowsHandle = rowsHandle ?? throw new ArgumentNullException(nameof(rowsHandle));
        _behavior = behavior;
        _closed = false;
        _isHttpReader = false;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LibSQLDataReader"/> class that wraps an HTTP data reader.
    /// </summary>
    /// <param name="httpDataReader">The HTTP data reader to wrap.</param>
    /// <param name="behavior">The command behavior that controls the reader.</param>
    internal LibSQLDataReader(LibSQLHttpDataReader httpDataReader, CommandBehavior behavior = CommandBehavior.Default)
    {
        _httpDataReader = httpDataReader ?? throw new ArgumentNullException(nameof(httpDataReader));
        _behavior = behavior;
        _closed = false;
        _isHttpReader = true;
    }

    /// <summary>
    /// Gets the depth of nesting for the current row.
    /// </summary>
    public override int Depth => 0;

    /// <summary>
    /// Gets the number of columns in the current row.
    /// </summary>
    public override int FieldCount
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            
            if (_isHttpReader && _httpDataReader != null)
                return _httpDataReader.FieldCount;
                
            if (_closed || _rowsHandle == null)
                return 0;

            EnsureMetadataInitialized();
            return _fieldCount;
        }
    }

    /// <summary>
    /// Gets a value indicating whether the data reader contains one or more rows.
    /// </summary>
    public override bool HasRows
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
                
            if (_isHttpReader && _httpDataReader != null)
                return _httpDataReader.HasRows;
                
            if (_closed || _rowsHandle == null)
                return false;

            // For now, assume there are rows if we have a valid handle
            // A more sophisticated implementation could peek ahead
            return true;
        }
    }

    /// <summary>
    /// Gets a value indicating whether the data reader is closed.
    /// </summary>
    public override bool IsClosed => _closed || _disposed;

    /// <summary>
    /// Gets the number of rows changed, inserted, or deleted by execution of the SQL statement.
    /// </summary>
    public override int RecordsAffected => -1; // Not applicable for SELECT statements

    /// <summary>
    /// Gets the value of the specified column as an instance of Object.
    /// </summary>
    /// <param name="ordinal">The zero-based column ordinal.</param>
    /// <returns>The value of the specified column.</returns>
    public override object this[int ordinal] => GetValue(ordinal);

    /// <summary>
    /// Gets the value of the specified column as an instance of Object.
    /// </summary>
    /// <param name="name">The name of the column.</param>
    /// <returns>The value of the specified column.</returns>
    public override object this[string name] => GetValue(GetOrdinal(name));

    /// <summary>
    /// Closes the data reader.
    /// </summary>
    public override void Close()
    {
        if (!_closed)
        {
            _closed = true;
            
            if (_isHttpReader && _httpDataReader != null)
            {
                _httpDataReader.Close();
                return;
            }
            
            // Clean up current row if we have one
            _currentRow?.Dispose();
            _currentRow = null;
            
            // Clean up rows handle
            _rowsHandle?.Dispose();
        }
    }

    /// <summary>
    /// Gets the value of the specified column as a Boolean.
    /// </summary>
    /// <param name="ordinal">The zero-based column ordinal.</param>
    /// <returns>The value of the specified column.</returns>
    public override bool GetBoolean(int ordinal)
    {
        var value = GetValue(ordinal);
        if (value == DBNull.Value)
            throw new InvalidCastException("Cannot convert NULL to boolean.");
        
        return (bool)LibSQLTypeConverter.ConvertFromLibSQL(value, typeof(bool));
    }

    /// <summary>
    /// Gets the value of the specified column as a byte.
    /// </summary>
    /// <param name="ordinal">The zero-based column ordinal.</param>
    /// <returns>The value of the specified column.</returns>
    public override byte GetByte(int ordinal)
    {
        var value = GetValue(ordinal);
        if (value == DBNull.Value)
            throw new InvalidCastException("Cannot convert NULL to byte.");
        
        return (byte)LibSQLTypeConverter.ConvertFromLibSQL(value, typeof(byte));
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
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_closed || _rowsHandle == null || _currentRow == null)
            throw new InvalidOperationException("No current row available. Call Read() first.");
        
        ValidateOrdinal(ordinal);
        
        var data = GetBlobBytes(ordinal);
        if (data == null)
            return 0;
            
        if (buffer == null)
            return data.Length;
            
        long actualLength = Math.Min(length, data.Length - dataOffset);
        if (actualLength <= 0)
            return 0;
            
        Array.Copy(data, dataOffset, buffer, bufferOffset, actualLength);
        return actualLength;
    }

    /// <summary>
    /// Gets the value of the specified column as a single character.
    /// </summary>
    /// <param name="ordinal">The zero-based column ordinal.</param>
    /// <returns>The value of the specified column.</returns>
    public override char GetChar(int ordinal)
    {
        var value = GetValue(ordinal);
        if (value == DBNull.Value)
            throw new InvalidCastException("Cannot convert NULL to char.");
        
        return (char)LibSQLTypeConverter.ConvertFromLibSQL(value, typeof(char));
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
        var value = GetValue(ordinal);
        if (value == DBNull.Value)
            return 0;

        string str = (string)LibSQLTypeConverter.ConvertFromLibSQL(value, typeof(string));
        
        if (buffer == null)
        {
            // Just return the length
            return str.Length;
        }

        var charsToRead = Math.Min(str.Length - dataOffset, length);
        charsToRead = Math.Min(charsToRead, buffer.Length - bufferOffset);
        
        if (charsToRead > 0)
        {
            str.CopyTo((int)dataOffset, buffer, bufferOffset, (int)charsToRead);
        }
        
        return charsToRead;
    }

    /// <summary>
    /// Gets the name of the data type of the specified column.
    /// </summary>
    /// <param name="ordinal">The zero-based column ordinal.</param>
    /// <returns>The name of the data type.</returns>
    public override string GetDataTypeName(int ordinal)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_closed || _rowsHandle == null)
            throw new InvalidOperationException("Reader is closed.");

        ValidateOrdinal(ordinal);

        // Get the column type from the current row if available
        if (_currentRow != null)
        {
            int result = LibSQLNative.libsql_column_type(_rowsHandle, _currentRow, ordinal, out int columnType, out IntPtr errorMsg);
            if (result == 0)
            {
                return columnType switch
                {
                    0 => "NULL",
                    1 => "INTEGER",
                    2 => "REAL",
                    3 => "TEXT",
                    4 => "BLOB",
                    _ => "UNKNOWN"
                };
            }
            else
            {
                LibSQLNative.libsql_free_error_msg(errorMsg);
            }
        }

        return "UNKNOWN";
    }

    /// <summary>
    /// Gets the value of the specified column as a DateTime.
    /// </summary>
    /// <param name="ordinal">The zero-based column ordinal.</param>
    /// <returns>The value of the specified column.</returns>
    public override DateTime GetDateTime(int ordinal)
    {
        var value = GetValue(ordinal);
        if (value == DBNull.Value)
            throw new InvalidCastException("Cannot convert NULL to DateTime.");
        
        return (DateTime)LibSQLTypeConverter.ConvertFromLibSQL(value, typeof(DateTime));
    }

    /// <summary>
    /// Gets the value of the specified column as a Decimal.
    /// </summary>
    /// <param name="ordinal">The zero-based column ordinal.</param>
    /// <returns>The value of the specified column.</returns>
    public override decimal GetDecimal(int ordinal)
    {
        var value = GetValue(ordinal);
        if (value == DBNull.Value)
            throw new InvalidCastException("Cannot convert NULL to decimal.");
        
        return (decimal)LibSQLTypeConverter.ConvertFromLibSQL(value, typeof(decimal));
    }

    /// <summary>
    /// Gets the value of the specified column as a double-precision floating point number.
    /// </summary>
    /// <param name="ordinal">The zero-based column ordinal.</param>
    /// <returns>The value of the specified column.</returns>
    public override double GetDouble(int ordinal)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
            
        if (_isHttpReader && _httpDataReader != null)
            return _httpDataReader.GetDouble(ordinal);
            
        if (_closed || _rowsHandle == null || _currentRow == null)
            throw new InvalidOperationException("No current row available. Call Read() first.");

        ValidateOrdinal(ordinal);

        int result = LibSQLNative.libsql_get_float(_currentRow, ordinal, out double value, out IntPtr errorMsg);
        if (result != 0)
        {
            var errorMessage = LibSQLHelper.GetErrorMessage(errorMsg);
            LibSQLNative.libsql_free_error_msg(errorMsg);
            throw new InvalidOperationException($"Failed to get double value: {errorMessage}");
        }

        return value;
    }

    /// <summary>
    /// Returns an IEnumerator that can be used to iterate through the rows in the data reader.
    /// </summary>
    /// <returns>An IEnumerator that can be used to iterate through the rows in the data reader.</returns>
    public override IEnumerator GetEnumerator()
    {
        return new DbEnumerator(this);
    }

    /// <summary>
    /// Gets the data type of the specified column.
    /// </summary>
    /// <param name="ordinal">The zero-based column ordinal.</param>
    /// <returns>The data type of the specified column.</returns>
    public override Type GetFieldType(int ordinal)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_closed || _rowsHandle == null)
            throw new InvalidOperationException("Reader is closed.");

        ValidateOrdinal(ordinal);

        // Get the column type from the current row if available, otherwise default to object
        if (_currentRow != null)
        {
            int result = LibSQLNative.libsql_column_type(_rowsHandle, _currentRow, ordinal, out int columnType, out IntPtr errorMsg);
            if (result == 0)
            {
                return columnType switch
                {
                    0 => typeof(object), // NULL
                    1 => typeof(long),   // INTEGER
                    2 => typeof(double), // REAL
                    3 => typeof(string), // TEXT
                    4 => typeof(byte[]), // BLOB
                    _ => typeof(object)
                };
            }
            else
            {
                LibSQLNative.libsql_free_error_msg(errorMsg);
            }
        }

        return typeof(object);
    }

    /// <summary>
    /// Gets the value of the specified column as a single-precision floating point number.
    /// </summary>
    /// <param name="ordinal">The zero-based column ordinal.</param>
    /// <returns>The value of the specified column.</returns>
    public override float GetFloat(int ordinal)
    {
        var value = GetValue(ordinal);
        if (value == DBNull.Value)
            throw new InvalidCastException("Cannot convert NULL to float.");
        
        return (float)LibSQLTypeConverter.ConvertFromLibSQL(value, typeof(float));
    }

    /// <summary>
    /// Gets the value of the specified column as a globally-unique identifier (GUID).
    /// </summary>
    /// <param name="ordinal">The zero-based column ordinal.</param>
    /// <returns>The value of the specified column.</returns>
    public override Guid GetGuid(int ordinal)
    {
        var value = GetValue(ordinal);
        if (value == DBNull.Value)
            throw new InvalidCastException("Cannot convert NULL to Guid.");
        
        return (Guid)LibSQLTypeConverter.ConvertFromLibSQL(value, typeof(Guid));
    }

    /// <summary>
    /// Gets the value of the specified column as a 16-bit signed integer.
    /// </summary>
    /// <param name="ordinal">The zero-based column ordinal.</param>
    /// <returns>The value of the specified column.</returns>
    public override short GetInt16(int ordinal)
    {
        var value = GetValue(ordinal);
        if (value == DBNull.Value)
            throw new InvalidCastException("Cannot convert NULL to short.");
        
        return (short)LibSQLTypeConverter.ConvertFromLibSQL(value, typeof(short));
    }

    /// <summary>
    /// Gets the value of the specified column as a 32-bit signed integer.
    /// </summary>
    /// <param name="ordinal">The zero-based column ordinal.</param>
    /// <returns>The value of the specified column.</returns>
    public override int GetInt32(int ordinal)
    {
        var value = GetValue(ordinal);
        if (value == DBNull.Value)
            throw new InvalidCastException("Cannot convert NULL to int.");
        
        return (int)LibSQLTypeConverter.ConvertFromLibSQL(value, typeof(int));
    }

    /// <summary>
    /// Gets the value of the specified column as a 64-bit signed integer.
    /// </summary>
    /// <param name="ordinal">The zero-based column ordinal.</param>
    /// <returns>The value of the specified column.</returns>
    public override long GetInt64(int ordinal)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
            
        if (_isHttpReader && _httpDataReader != null)
            return _httpDataReader.GetInt64(ordinal);
            
        if (_closed || _rowsHandle == null || _currentRow == null)
            throw new InvalidOperationException("No current row available. Call Read() first.");

        ValidateOrdinal(ordinal);

        int result = LibSQLNative.libsql_get_int(_currentRow, ordinal, out long value, out IntPtr errorMsg);
        if (result != 0)
        {
            var errorMessage = LibSQLHelper.GetErrorMessage(errorMsg);
            LibSQLNative.libsql_free_error_msg(errorMsg);
            throw new InvalidOperationException($"Failed to get integer value: {errorMessage}");
        }

        return value;
    }

    /// <summary>
    /// Gets the name of the column, given the zero-based column ordinal.
    /// </summary>
    /// <param name="ordinal">The zero-based column ordinal.</param>
    /// <returns>The name of the specified column.</returns>
    public override string GetName(int ordinal)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
            
        if (_isHttpReader && _httpDataReader != null)
            return _httpDataReader.GetName(ordinal);
            
        if (_closed || _rowsHandle == null)
            throw new InvalidOperationException("Reader is closed.");

        EnsureMetadataInitialized();
        ValidateOrdinal(ordinal);

        return _columnNames![ordinal];
    }

    /// <summary>
    /// Gets the column ordinal given the name of the column.
    /// </summary>
    /// <param name="name">The name of the column.</param>
    /// <returns>The zero-based column ordinal.</returns>
    public override int GetOrdinal(string name)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
            
        if (_isHttpReader && _httpDataReader != null)
            return _httpDataReader.GetOrdinal(name);
            
        if (_closed || _rowsHandle == null)
            throw new InvalidOperationException("Reader is closed.");
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Column name cannot be null or empty.", nameof(name));

        EnsureMetadataInitialized();

        for (int i = 0; i < _columnNames!.Length; i++)
        {
            if (string.Equals(_columnNames[i], name, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        throw new IndexOutOfRangeException($"Column '{name}' not found.");
    }

    /// <summary>
    /// Gets the value of the specified column as an instance of string.
    /// </summary>
    /// <param name="ordinal">The zero-based column ordinal.</param>
    /// <returns>The value of the specified column.</returns>
    public override string GetString(int ordinal)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
            
        if (_isHttpReader && _httpDataReader != null)
            return _httpDataReader.GetString(ordinal);
            
        if (_closed || _rowsHandle == null || _currentRow == null)
            throw new InvalidOperationException("No current row available. Call Read() first.");

        ValidateOrdinal(ordinal);

        IntPtr strPtr;
        int result = LibSQLNative.libsql_get_string(_currentRow, ordinal, out strPtr, out IntPtr errorMsg);
        if (result != 0)
        {
            var errorMessage = LibSQLHelper.GetErrorMessage(errorMsg);
            LibSQLNative.libsql_free_error_msg(errorMsg);
            throw new InvalidOperationException($"Failed to get string value: {errorMessage}");
        }

        try
        {
            return Marshal.PtrToStringUTF8(strPtr) ?? string.Empty;
        }
        finally
        {
            if (strPtr != IntPtr.Zero)
                LibSQLNative.libsql_free_string(strPtr);
        }
    }

    /// <summary>
    /// Gets the value of the specified column as an instance of Object.
    /// </summary>
    /// <param name="ordinal">The zero-based column ordinal.</param>
    /// <returns>The value of the specified column.</returns>
    public override object GetValue(int ordinal)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
            
        if (_isHttpReader && _httpDataReader != null)
            return _httpDataReader.GetValue(ordinal);
            
        if (_closed || _rowsHandle == null || _currentRow == null)
            throw new InvalidOperationException("No current row available. Call Read() first.");

        ValidateOrdinal(ordinal);

        // Get the column type first
        int result = LibSQLNative.libsql_column_type(_rowsHandle, _currentRow, ordinal, out int columnType, out IntPtr errorMsg);
        if (result != 0)
        {
            var errorMessage = LibSQLHelper.GetErrorMessage(errorMsg);
            LibSQLNative.libsql_free_error_msg(errorMsg);
            throw new InvalidOperationException($"Failed to get column type: {errorMessage}");
        }

        // Return value based on type
        // libSQL/SQLite type constants: INT=1, FLOAT=2, TEXT=3, BLOB=4, NULL=5
        return columnType switch
        {
            1 => GetInt64(ordinal), // INT - use long as the widest integer type
            2 => GetDouble(ordinal), // FLOAT
            3 => GetString(ordinal), // TEXT
            4 => GetBlobBytes(ordinal), // BLOB
            5 => DBNull.Value, // NULL
            _ => throw new NotSupportedException($"Unknown column type: {columnType}")
        };
    }

    /// <summary>
    /// Populates an array of objects with the column values of the current row.
    /// </summary>
    /// <param name="values">An array of Object into which to copy the attribute columns.</param>
    /// <returns>The number of instances of Object in the array.</returns>
    public override int GetValues(object[] values)
    {
        ArgumentNullException.ThrowIfNull(values);
        
        int count = Math.Min(values.Length, FieldCount);
        for (int i = 0; i < count; i++)
        {
            values[i] = GetValue(i);
        }
        
        return count;
    }

    /// <summary>
    /// Gets a value that indicates whether the column contains nonexistent or missing values.
    /// </summary>
    /// <param name="ordinal">The zero-based column ordinal.</param>
    /// <returns>true if the specified column is equivalent to DBNull; otherwise false.</returns>
    public override bool IsDBNull(int ordinal)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
            
        if (_isHttpReader && _httpDataReader != null)
            return _httpDataReader.IsDBNull(ordinal);
            
        if (_closed || _rowsHandle == null || _currentRow == null)
            throw new InvalidOperationException("No current row available. Call Read() first.");

        ValidateOrdinal(ordinal);

        // Get the column type
        int result = LibSQLNative.libsql_column_type(_rowsHandle, _currentRow, ordinal, out int columnType, out IntPtr errorMsg);
        if (result != 0)
        {
            var errorMessage = LibSQLHelper.GetErrorMessage(errorMsg);
            LibSQLNative.libsql_free_error_msg(errorMsg);
            throw new InvalidOperationException($"Failed to get column type: {errorMessage}");
        }

        return columnType == 0; // NULL type
    }

    /// <summary>
    /// Advances the reader to the next result when reading the results of a batch of statements.
    /// </summary>
    /// <returns>true if there are more result sets; otherwise false.</returns>
    public override bool NextResult()
    {
        // libSQL doesn't support multiple result sets in a single query execution
        // Always return false to indicate no more result sets
        return false;
    }

    /// <summary>
    /// Returns a DataTable that describes the column metadata of the LibSQLDataReader.
    /// </summary>
    /// <returns>A DataTable that describes the column metadata.</returns>
    public override DataTable? GetSchemaTable()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_closed || _rowsHandle == null)
            return null;

        EnsureMetadataInitialized();

        var schemaTable = new DataTable("SchemaTable");
        
        // Define the schema table columns
        schemaTable.Columns.Add("ColumnName", typeof(string));
        schemaTable.Columns.Add("ColumnOrdinal", typeof(int));
        schemaTable.Columns.Add("ColumnSize", typeof(int));
        schemaTable.Columns.Add("NumericPrecision", typeof(short));
        schemaTable.Columns.Add("NumericScale", typeof(short));
        schemaTable.Columns.Add("DataType", typeof(Type));
        schemaTable.Columns.Add("ProviderType", typeof(string));
        schemaTable.Columns.Add("IsLong", typeof(bool));
        schemaTable.Columns.Add("AllowDBNull", typeof(bool));
        schemaTable.Columns.Add("IsReadOnly", typeof(bool));
        schemaTable.Columns.Add("IsRowVersion", typeof(bool));
        schemaTable.Columns.Add("IsUnique", typeof(bool));
        schemaTable.Columns.Add("IsKey", typeof(bool));
        schemaTable.Columns.Add("IsAutoIncrement", typeof(bool));
        schemaTable.Columns.Add("BaseSchemaName", typeof(string));
        schemaTable.Columns.Add("BaseCatalogName", typeof(string));
        schemaTable.Columns.Add("BaseTableName", typeof(string));
        schemaTable.Columns.Add("BaseColumnName", typeof(string));

        // Populate schema information for each column
        for (int i = 0; i < _fieldCount; i++)
        {
            var row = schemaTable.NewRow();
            
            row["ColumnName"] = _columnNames![i];
            row["ColumnOrdinal"] = i;
            row["ColumnSize"] = -1; // Unknown for libSQL
            row["NumericPrecision"] = DBNull.Value;
            row["NumericScale"] = DBNull.Value;
            
            // Try to determine the data type from the current row if available
            Type dataType = typeof(object);
            string providerType = "UNKNOWN";
            
            if (_currentRow != null)
            {
                int result = LibSQLNative.libsql_column_type(_rowsHandle, _currentRow, i, out int columnType, out IntPtr errorMsg);
                if (result == 0)
                {
                    (dataType, providerType) = columnType switch
                    {
                        0 => (typeof(object), "NULL"),
                        1 => (typeof(long), "INTEGER"),
                        2 => (typeof(double), "REAL"),
                        3 => (typeof(string), "TEXT"),
                        4 => (typeof(byte[]), "BLOB"),
                        _ => (typeof(object), "UNKNOWN")
                    };
                }
                else
                {
                    LibSQLNative.libsql_free_error_msg(errorMsg);
                }
            }
            
            row["DataType"] = dataType;
            row["ProviderType"] = providerType;
            row["IsLong"] = providerType == "BLOB";
            row["AllowDBNull"] = true; // SQLite/libSQL allows NULL in most columns
            row["IsReadOnly"] = true; // Data readers are read-only
            row["IsRowVersion"] = false;
            row["IsUnique"] = false; // We can't determine this from libSQL API
            row["IsKey"] = false; // We can't determine this from libSQL API
            row["IsAutoIncrement"] = false; // We can't determine this from libSQL API
            row["BaseSchemaName"] = DBNull.Value;
            row["BaseCatalogName"] = DBNull.Value;
            row["BaseTableName"] = DBNull.Value;
            row["BaseColumnName"] = _columnNames![i];
            
            schemaTable.Rows.Add(row);
        }

        return schemaTable;
    }

    /// <summary>
    /// Gets the value of the specified column as the requested type.
    /// </summary>
    /// <typeparam name="T">The type of the value to be returned.</typeparam>
    /// <param name="ordinal">The zero-based column ordinal.</param>
    /// <returns>The value of the specified column.</returns>
    public override T GetFieldValue<T>(int ordinal)
    {
        var value = GetValue(ordinal);
        
        if (value == DBNull.Value)
        {
            if (typeof(T).IsValueType && Nullable.GetUnderlyingType(typeof(T)) == null)
                throw new InvalidCastException($"Column contains null value and cannot be converted to non-nullable type {typeof(T)}.");
            
            return default!;
        }

        // Use type converter for consistent conversions
        try
        {
            return (T)LibSQLTypeConverter.ConvertFromLibSQL(value, typeof(T));
        }
        catch (Exception ex)
        {
            throw new InvalidCastException($"Cannot convert value of type {value.GetType()} to {typeof(T)}.", ex);
        }
    }

    /// <summary>
    /// Advances the reader to the next record in a result set.
    /// </summary>
    /// <returns>true if there are more rows; otherwise false.</returns>
    public override bool Read()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
            
        if (_isHttpReader && _httpDataReader != null)
            return _httpDataReader.Read();
            
        if (_closed || _rowsHandle == null)
            return false;

        // Clean up previous row
        _currentRow?.Dispose();
        _currentRow = null;

        // Get next row from libSQL
        int result = LibSQLNative.libsql_next_row(_rowsHandle, out IntPtr rowPtr, out IntPtr errorMsg);
        
        if (result != 0)
        {
            if (rowPtr == IntPtr.Zero)
            {
                // No more rows
                return false;
            }
            
            // Error occurred
            var errorMessage = LibSQLHelper.GetErrorMessage(errorMsg);
            LibSQLNative.libsql_free_error_msg(errorMsg);
            throw new InvalidOperationException($"Failed to read next row: {errorMessage}");
        }

        if (rowPtr == IntPtr.Zero)
        {
            // No more rows
            return false;
        }

        _currentRow = new LibSQLRowHandle(rowPtr);
        return true;
    }

    /// <summary>
    /// Ensures that the column metadata has been initialized.
    /// </summary>
    private void EnsureMetadataInitialized()
    {
        if (_hasInitializedMetadata || _rowsHandle == null)
            return;

        _fieldCount = LibSQLNative.libsql_column_count(_rowsHandle);
        _columnNames = new string[_fieldCount];

        for (int i = 0; i < _fieldCount; i++)
        {
            int result = LibSQLNative.libsql_column_name(_rowsHandle, i, out IntPtr namePtr, out IntPtr errorMsg);
            if (result != 0)
            {
                var errorMessage = LibSQLHelper.GetErrorMessage(errorMsg);
                LibSQLNative.libsql_free_error_msg(errorMsg);
                throw new InvalidOperationException($"Failed to get column name: {errorMessage}");
            }

            try
            {
                _columnNames[i] = Marshal.PtrToStringUTF8(namePtr) ?? $"Column{i}";
            }
            finally
            {
                if (namePtr != IntPtr.Zero)
                    LibSQLNative.libsql_free_string(namePtr);
            }
        }

        _hasInitializedMetadata = true;
    }

    /// <summary>
    /// Validates that the specified column ordinal is within bounds.
    /// </summary>
    /// <param name="ordinal">The column ordinal to validate.</param>
    private void ValidateOrdinal(int ordinal)
    {
        if (ordinal < 0 || ordinal >= FieldCount)
            throw new IndexOutOfRangeException($"Column ordinal {ordinal} is out of range. Valid range is 0 to {FieldCount - 1}.");
    }

    /// <summary>
    /// Gets the value of the specified column as a byte array.
    /// </summary>
    /// <param name="ordinal">The zero-based column ordinal.</param>
    /// <returns>The value of the specified column as a byte array.</returns>
    private byte[]? GetBlobBytes(int ordinal)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_closed || _rowsHandle == null || _currentRow == null)
            throw new InvalidOperationException("No current row available. Call Read() first.");

        ValidateOrdinal(ordinal);

        int result = LibSQLNative.libsql_get_blob(_currentRow, ordinal, out LibSQLBlob blob, out IntPtr errorMsg);
        if (result != 0)
        {
            var errorMessage = LibSQLHelper.GetErrorMessage(errorMsg);
            LibSQLNative.libsql_free_error_msg(errorMsg);
            throw new InvalidOperationException($"Failed to get blob value: {errorMessage}");
        }

        try
        {
            if (blob.Ptr == IntPtr.Zero || blob.Len == 0)
                return null;

            var data = new byte[blob.Len];
            Marshal.Copy(blob.Ptr, data, 0, blob.Len);
            return data;
        }
        finally
        {
            LibSQLNative.libsql_free_blob(blob);
        }
    }

    /// <summary>
    /// Releases the unmanaged resources used by the LibSQLDataReader.
    /// </summary>
    /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected override void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                Close();
                if (_isHttpReader && _httpDataReader != null)
                {
                    _httpDataReader.Dispose();
                }
            }
            _disposed = true;
        }
        base.Dispose(disposing);
    }
}