#nullable disable warnings

using System;
using System.Data;
using System.Data.Common;

namespace Nelknet.LibSQL.Data;

/// <summary>
/// Represents a parameter to a <see cref="LibSQLCommand"/>.
/// </summary>
public sealed class LibSQLParameter : DbParameter
{
    private string _parameterName = string.Empty;
    private object? _value;
    private DbType _dbType = DbType.String;
    private ParameterDirection _direction = ParameterDirection.Input;

    /// <summary>
    /// Initializes a new instance of the <see cref="LibSQLParameter"/> class.
    /// </summary>
    public LibSQLParameter()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LibSQLParameter"/> class with the specified parameter name.
    /// </summary>
    /// <param name="parameterName">The name of the parameter.</param>
    public LibSQLParameter(string parameterName) : this()
    {
        ParameterName = parameterName;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LibSQLParameter"/> class with the specified parameter name and value.
    /// </summary>
    /// <param name="parameterName">The name of the parameter.</param>
    /// <param name="value">The value of the parameter.</param>
    public LibSQLParameter(string parameterName, object? value) : this(parameterName)
    {
        Value = value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LibSQLParameter"/> class with the specified parameter name and data type.
    /// </summary>
    /// <param name="parameterName">The name of the parameter.</param>
    /// <param name="dbType">The data type of the parameter.</param>
    public LibSQLParameter(string parameterName, DbType dbType) : this(parameterName)
    {
        DbType = dbType;
    }

    /// <summary>
    /// Gets or sets the <see cref="DbType"/> of the parameter.
    /// </summary>
    public override DbType DbType
    {
        get => _dbType;
        set => _dbType = value;
    }

    /// <summary>
    /// Gets or sets the direction of the parameter.
    /// </summary>
    public override ParameterDirection Direction
    {
        get => _direction;
        set
        {
            if (value != ParameterDirection.Input)
            {
                throw new NotSupportedException("libSQL only supports input parameters.");
            }
            _direction = value;
        }
    }

    /// <summary>
    /// Gets or sets whether the parameter accepts null values.
    /// </summary>
    public override bool IsNullable { get; set; } = true;

    /// <summary>
    /// Gets or sets the name of the parameter.
    /// </summary>
    public override string ParameterName
    {
        get => _parameterName;
        set => _parameterName = value ?? string.Empty;
    }

    /// <summary>
    /// Gets or sets the maximum size, in bytes, of the data within the column.
    /// </summary>
    public override int Size { get; set; }

    /// <summary>
    /// Gets or sets the name of the source column mapped to the DataSet and used for loading or returning the Value.
    /// </summary>
    public override string SourceColumn { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether source column values that are null are converted to a null value or their default value.
    /// </summary>
    public override bool SourceColumnNullMapping { get; set; }

    /// <summary>
    /// Gets or sets the DataRowVersion to use when you load Value.
    /// </summary>
    public override DataRowVersion SourceVersion { get; set; } = DataRowVersion.Current;

    /// <summary>
    /// Gets or sets the value of the parameter.
    /// </summary>
    public override object? Value
    {
        get => _value;
        set
        {
            _value = value;
            // Auto-infer DbType based on the value type
            if (value != null && value != DBNull.Value)
            {
                _dbType = InferDbTypeFromValue(value);
            }
        }
    }

    /// <summary>
    /// Resets the DbType property to its original settings.
    /// </summary>
    public override void ResetDbType()
    {
        _dbType = DbType.String;
    }

    /// <summary>
    /// Gets or sets the maximum number of digits used to represent the Value property.
    /// </summary>
    public override byte Precision { get; set; }

    /// <summary>
    /// Gets or sets the number of decimal places to which Value is resolved.
    /// </summary>
    public override byte Scale { get; set; }

    /// <summary>
    /// Validates the parameter's properties.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrEmpty(ParameterName))
        {
            throw new InvalidOperationException("Parameter name cannot be null or empty.");
        }

        if (!ParameterName.StartsWith("@") && !ParameterName.StartsWith(":") && !ParameterName.StartsWith("?"))
        {
            throw new ArgumentException("Parameter name must start with '@', ':', or '?'.", nameof(ParameterName));
        }
    }

    /// <summary>
    /// Infers the DbType from the value type.
    /// </summary>
    /// <param name="value">The value to infer the type from.</param>
    /// <returns>The inferred DbType.</returns>
    private static DbType InferDbTypeFromValue(object value)
    {
        return value switch
        {
            bool => DbType.Boolean,
            byte => DbType.Byte,
            sbyte => DbType.SByte,
            short => DbType.Int16,
            ushort => DbType.UInt16,
            int => DbType.Int32,
            uint => DbType.UInt32,
            long => DbType.Int64,
            ulong => DbType.UInt64,
            float => DbType.Single,
            double => DbType.Double,
            decimal => DbType.Decimal,
            DateTime => DbType.DateTime,
            DateTimeOffset => DbType.DateTimeOffset,
            TimeSpan => DbType.Time,
            Guid => DbType.Guid,
            byte[] => DbType.Binary,
            char => DbType.StringFixedLength,
            string => DbType.String,
#if NET6_0_OR_GREATER
            DateOnly => DbType.Date,
            TimeOnly => DbType.Time,
#endif
            _ => DbType.Object
        };
    }
}