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
        
        // Handle sequence response (multiple results)
        if (result.Type == HranaTypes.Sequence)
        {
            // For sequence, we don't get individual affected row counts
            // Return -1 to indicate the operation succeeded but row count is unknown
            return -1;
        }
        
        // Handle single execute response
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
        
        // Handle sequence response
        if (result.Type == HranaTypes.Sequence)
        {
            // Sequence responses don't return data readers
            // Return empty reader for compatibility
            return new LibSQLHttpDataReader(new HranaQueryResult
            {
                Cols = new List<HranaColumn>(),
                Rows = new List<List<HranaValue>>(),
                AffectedRowCount = 0
            });
        }
        
        // Handle single execute response
        if (result.Response?.Result == null)
        {
            throw new LibSQLException("Invalid response from server");
        }

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
        
        // Check if we have multiple statements (naive check for semicolons outside of strings)
        var sql = CommandText?.Trim() ?? string.Empty;
        var hasMultipleStatements = CountStatements(sql) > 1;
        
        if (hasMultipleStatements && (_parameters == null || _parameters.Count == 0))
        {
            // Use sequence request for multi-statement execution without parameters
            // This matches standard ADO.NET behavior where all statements execute
            batch.Requests.Add(new HranaRequest
            {
                Type = HranaTypes.Sequence,
                Sql = sql
            });
        }
        else
        {
            // Use execute request for single statement or statements with parameters
            var statement = new HranaStatement
            {
                Sql = ProcessSql(sql),
                Args = CreateArgs()
            };

            batch.Requests.Add(new HranaRequest
            {
                Type = HranaTypes.Execute,
                Statement = statement
            });
        }

        return batch;
    }
    
    private int CountStatements(string sql)
    {
        // Simple count of statements by splitting on semicolons
        // This is a naive implementation that doesn't handle semicolons in strings
        if (string.IsNullOrWhiteSpace(sql))
            return 0;
            
        var statements = sql.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return statements.Where(s => !string.IsNullOrWhiteSpace(s)).Count();
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

    /// <summary>
    /// Executes a batch of SQL statements as a transaction.
    /// All statements will be executed in a single transaction that is automatically committed if all succeed,
    /// or rolled back if any fail.
    /// </summary>
    /// <param name="statements">The SQL statements to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of affected rows, or -1 if not available.</returns>
    public async Task<int> ExecuteTransactionalBatchAsync(string[] statements, CancellationToken cancellationToken = default)
    {
        if (statements == null || statements.Length == 0)
            throw new ArgumentException("Statements array cannot be null or empty", nameof(statements));

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (CommandTimeout > 0)
        {
            cts.CancelAfter(TimeSpan.FromSeconds(CommandTimeout));
        }

        // Create a batch with conditional steps
        var batch = new HranaBatchRequest();
        var hranaBatch = new HranaBatch();

        // Add BEGIN statement as first step
        hranaBatch.Steps.Add(new HranaBatchStep
        {
            Statement = new HranaStatement { Sql = "BEGIN", Args = null },
            Condition = null
        });

        // Add each statement with condition to check previous step succeeded
        for (int i = 0; i < statements.Length; i++)
        {
            hranaBatch.Steps.Add(new HranaBatchStep
            {
                Statement = new HranaStatement { Sql = statements[i], Args = null },
                Condition = new HranaBatchCondition { Type = "ok", Step = i } // Depends on previous step
            });
        }

        // Add COMMIT with condition that all previous steps succeeded
        hranaBatch.Steps.Add(new HranaBatchStep
        {
            Statement = new HranaStatement { Sql = "COMMIT", Args = null },
            Condition = new HranaBatchCondition { Type = "ok", Step = statements.Length }
        });

        // Add ROLLBACK if any step failed (not condition for the COMMIT step)
        hranaBatch.Steps.Add(new HranaBatchStep
        {
            Statement = new HranaStatement { Sql = "ROLLBACK", Args = null },
            Condition = new HranaBatchCondition 
            { 
                Type = "not", 
                InnerCondition = new HranaBatchCondition { Type = "ok", Step = statements.Length + 1 }
            }
        });

        batch.Requests.Add(new HranaRequest
        {
            Type = HranaTypes.Batch,
            Batch = hranaBatch
        });

        var response = await _httpClient.ExecuteBatchAsync(batch, cts.Token);

        if (response.Results.Count == 0)
            return -1;

        var result = response.Results[0];
        if (result.Response?.BatchResult != null)
        {
            // Check if all steps succeeded
            var batchResult = result.Response.BatchResult;
            if (batchResult.StepErrors != null && batchResult.StepErrors.Any(e => e != null))
            {
                // Find the first error
                var firstError = batchResult.StepErrors.FirstOrDefault(e => e != null);
                throw new LibSQLException($"Batch execution failed: {firstError?.Message ?? "Unknown error"}");
            }

            // Sum up affected rows from all steps (excluding BEGIN/COMMIT/ROLLBACK)
            var totalAffected = 0;
            if (batchResult.StepResults != null)
            {
                // Skip BEGIN (0) and COMMIT/ROLLBACK (last 2)
                for (int i = 1; i < batchResult.StepResults.Count - 2 && i <= statements.Length; i++)
                {
                    if (batchResult.StepResults[i] != null)
                    {
                        totalAffected += (int)batchResult.StepResults[i].AffectedRowCount;
                    }
                }
            }

            return totalAffected;
        }

        return -1;
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