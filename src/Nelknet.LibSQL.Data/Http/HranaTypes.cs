#nullable disable warnings

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Nelknet.LibSQL.Data.Http;

/// <summary>
/// Represents a Hrana protocol batch request.
/// </summary>
internal sealed class HranaBatchRequest
{
    [JsonPropertyName("requests")]
    public List<HranaRequest> Requests { get; set; } = new();
}

/// <summary>
/// Represents a single request in a Hrana batch.
/// </summary>
internal sealed class HranaRequest
{
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("stmt")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public HranaStatement? Statement { get; set; }

    [JsonPropertyName("batch")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public HranaBatch? Batch { get; set; }

    [JsonPropertyName("sql")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Sql { get; set; }
}

/// <summary>
/// Represents a SQL statement in a Hrana request.
/// </summary>
internal sealed class HranaStatement
{
    [JsonPropertyName("sql")]
    public string Sql { get; set; }

    [JsonPropertyName("args")]
    public List<HranaValue>? Args { get; set; }
}

/// <summary>
/// Represents a parameter value in Hrana protocol.
/// </summary>
internal sealed class HranaValue
{
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("value")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Value { get; set; }
    
    [JsonPropertyName("base64")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Base64 { get; set; }
}

/// <summary>
/// Represents a Hrana protocol batch response.
/// </summary>
internal sealed class HranaBatchResponse
{
    [JsonPropertyName("baton")]
    public string? Baton { get; set; }
    
    [JsonPropertyName("base_url")]
    public string? BaseUrl { get; set; }
    
    [JsonPropertyName("results")]
    public List<HranaResult> Results { get; set; } = new();
}

/// <summary>
/// Represents a single result in a Hrana batch response.
/// </summary>
internal sealed class HranaResult
{
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("response")]
    public HranaResponse? Response { get; set; }
}

/// <summary>
/// Represents the response portion of a Hrana result.
/// </summary>
internal sealed class HranaResponse
{
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("result")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public HranaQueryResult? Result { get; set; }

    [JsonPropertyName("batch_result")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public HranaBatchResult? BatchResult { get; set; }

    // Error fields
    [JsonPropertyName("error")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public HranaError? Error { get; set; }
}

/// <summary>
/// Represents query result data in Hrana protocol.
/// </summary>
internal sealed class HranaQueryResult
{
    [JsonPropertyName("cols")]
    public List<HranaColumn>? Cols { get; set; }

    [JsonPropertyName("rows")]
    public List<List<HranaValue>>? Rows { get; set; }

    [JsonPropertyName("affected_row_count")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public ulong AffectedRowCount { get; set; }

    [JsonPropertyName("last_insert_rowid")]
    public string? LastInsertRowid { get; set; }
    
    [JsonPropertyName("replication_index")]
    public string? ReplicationIndex { get; set; }
    
    [JsonPropertyName("rows_read")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public ulong RowsRead { get; set; }
    
    [JsonPropertyName("rows_written")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public ulong RowsWritten { get; set; }
    
    [JsonPropertyName("query_duration_ms")]
    public double QueryDurationMs { get; set; }
}

/// <summary>
/// Represents a column definition in Hrana protocol.
/// </summary>
internal sealed class HranaColumn
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("decltype")]
    public string? DeclType { get; set; }
}

/// <summary>
/// Represents an error in Hrana protocol.
/// </summary>
internal sealed class HranaError
{
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("code")]
    public string? Code { get; set; }
}

/// <summary>
/// Constants for Hrana protocol types.
/// </summary>
internal static class HranaTypes
{
    // Request types
    public const string Execute = "execute";
    public const string Close = "close";
    public const string Batch = "batch";
    public const string Sequence = "sequence";

    // Response types
    public const string Ok = "ok";
    public const string Error = "error";

    // Value types
    public const string Null = "null";
    public const string Integer = "integer";
    public const string Float = "float";
    public const string Text = "text";
    public const string Blob = "blob";
}

/// <summary>
/// Represents a batch of statements for conditional execution.
/// </summary>
internal sealed class HranaBatch
{
    [JsonPropertyName("steps")]
    public List<HranaBatchStep> Steps { get; set; } = new();
}

/// <summary>
/// Represents a single step in a batch.
/// </summary>
internal sealed class HranaBatchStep
{
    [JsonPropertyName("stmt")]
    public HranaStatement Statement { get; set; }

    [JsonPropertyName("condition")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public HranaBatchCondition? Condition { get; set; }
}

/// <summary>
/// Represents a condition for batch step execution.
/// </summary>
internal sealed class HranaBatchCondition
{
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("step")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Step { get; set; }

    [JsonPropertyName("cond")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public HranaBatchCondition? InnerCondition { get; set; }
}

/// <summary>
/// Represents a batch result from the server.
/// </summary>
internal sealed class HranaBatchResult
{
    [JsonPropertyName("step_results")]
    public List<HranaQueryResult?> StepResults { get; set; } = new();

    [JsonPropertyName("step_errors")]
    public List<HranaError?> StepErrors { get; set; } = new();
}