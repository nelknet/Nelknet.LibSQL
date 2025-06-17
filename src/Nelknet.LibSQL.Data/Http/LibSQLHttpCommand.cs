#nullable disable warnings

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nelknet.LibSQL.Data.Exceptions;

namespace Nelknet.LibSQL.Data.Http;

/// <summary>
/// HTTP-based command implementation for remote libSQL connections.
/// </summary>
internal sealed class LibSQLHttpCommand : DbCommand
{
    private readonly LibSQLHttpClient _httpClient;
    private readonly LibSQLParameterCollection _parameters;
    private string _commandText = string.Empty;
    private int _commandTimeout = 30;

    public LibSQLHttpCommand(LibSQLHttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _parameters = new LibSQLParameterCollection();
    }

    public override string CommandText
    {
        get => _commandText;
        set => _commandText = value ?? string.Empty;
    }

    public override int CommandTimeout
    {
        get => _commandTimeout;
        set => _commandTimeout = Math.Max(0, value);
    }

    public override CommandType CommandType { get; set; } = CommandType.Text;

    public override bool DesignTimeVisible { get; set; }

    public override UpdateRowSource UpdatedRowSource { get; set; }

    protected override DbConnection? DbConnection { get; set; }

    protected override DbParameterCollection DbParameterCollection => _parameters;

    protected override DbTransaction? DbTransaction { get; set; }

    public override void Cancel()
    {
        // HTTP requests can't be cancelled once sent, this is a no-op
    }

    public override int ExecuteNonQuery()
    {
        return ExecuteNonQueryAsync(CancellationToken.None).GetAwaiter().GetResult();
    }

    public override async Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
    {
        ValidateCommand();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (CommandTimeout > 0)
        {
            cts.CancelAfter(TimeSpan.FromSeconds(CommandTimeout));
        }

        var batch = CreateBatch();
        var response = await _httpClient.ExecuteBatchAsync(batch, cts.Token);

        if (response.Results.Count == 0)
            return 0;

        var result = response.Results[0];
        if (result.Response?.Result != null)
        {
            return (int)result.Response.Result.AffectedRowCount;
        }

        return 0;
    }

    public override object? ExecuteScalar()
    {
        return ExecuteScalarAsync(CancellationToken.None).GetAwaiter().GetResult();
    }

    public override async Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken)
    {
        using var reader = await ExecuteReaderAsync(cancellationToken);
        if (reader.Read() && reader.FieldCount > 0)
        {
            return reader.GetValue(0);
        }
        return null;
    }

    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
    {
        return ExecuteDbDataReaderAsync(behavior, CancellationToken.None).GetAwaiter().GetResult();
    }

    protected override async Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
    {
        ValidateCommand();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (CommandTimeout > 0)
        {
            cts.CancelAfter(TimeSpan.FromSeconds(CommandTimeout));
        }

        var batch = CreateBatch();
        var response = await _httpClient.ExecuteBatchAsync(batch, cts.Token);

        if (response.Results.Count == 0)
            throw new LibSQLException("No results returned from server");

        var result = response.Results[0];
        if (result.Response?.Result == null)
            throw new LibSQLException("Invalid response from server");

        return new LibSQLHttpDataReader(result.Response.Result);
    }

    public override void Prepare()
    {
        // HTTP connections don't support prepared statements
        // This is a no-op for compatibility
    }

    protected override DbParameter CreateDbParameter()
    {
        return new LibSQLParameter();
    }

    private void ValidateCommand()
    {
        if (string.IsNullOrWhiteSpace(CommandText))
            throw new InvalidOperationException("CommandText must be specified");

        if (CommandType != CommandType.Text)
            throw new NotSupportedException($"CommandType {CommandType} is not supported for HTTP connections");
    }

    private HranaBatchRequest CreateBatch()
    {
        var batch = new HranaBatchRequest();
        
        var statement = new HranaStatement
        {
            Sql = ProcessSql(CommandText),
            Args = CreateArgs()
        };

        batch.Requests.Add(new HranaRequest
        {
            Type = HranaTypes.Execute,
            Statement = statement
        });

        return batch;
    }

    private string ProcessSql(string sql)
    {
        // Convert named parameters to positional parameters
        if (_parameters.Count == 0)
            return sql;

        var processedSql = sql;
        var paramIndex = 1;
        
        foreach (LibSQLParameter param in _parameters)
        {
            if (!string.IsNullOrEmpty(param.ParameterName))
            {
                var paramName = param.ParameterName.StartsWith("@") ? param.ParameterName : "@" + param.ParameterName;
                processedSql = processedSql.Replace(paramName, $"?{paramIndex}");
                paramIndex++;
            }
        }

        return processedSql;
    }

    private List<HranaValue>? CreateArgs()
    {
        if (_parameters.Count == 0)
            return null;

        var args = new List<HranaValue>();
        
        foreach (LibSQLParameter param in _parameters)
        {
            args.Add(ConvertParameter(param));
        }

        return args;
    }

    private static HranaValue ConvertParameter(LibSQLParameter parameter)
    {
        var value = parameter.Value;
        
        if (value == null || value == DBNull.Value)
        {
            return new HranaValue { Type = HranaTypes.Null, Value = null };
        }

        return parameter.DbType switch
        {
            DbType.Boolean => new HranaValue 
            { 
                Type = HranaTypes.Integer, 
                Value = (Convert.ToBoolean(value) ? 1 : 0).ToString(CultureInfo.InvariantCulture) 
            },
            DbType.Byte or DbType.SByte or DbType.Int16 or DbType.UInt16 or 
            DbType.Int32 or DbType.UInt32 or DbType.Int64 or DbType.UInt64 => new HranaValue 
            { 
                Type = HranaTypes.Integer, 
                Value = Convert.ToInt64(value).ToString(CultureInfo.InvariantCulture) 
            },
            DbType.Single or DbType.Double or DbType.Decimal => new HranaValue 
            { 
                Type = HranaTypes.Float, 
                Value = Convert.ToDouble(value) 
            },
            DbType.Binary => new HranaValue 
            { 
                Type = HranaTypes.Blob, 
                Base64 = value is byte[] bytes ? Convert.ToBase64String(bytes) : Convert.ToBase64String((byte[])value),
                Value = null
            },
            _ => new HranaValue 
            { 
                Type = HranaTypes.Text, 
                Value = Convert.ToString(value) ?? string.Empty 
            }
        };
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // HttpClient is managed by the connection, don't dispose it here
        }
        base.Dispose(disposing);
    }
}