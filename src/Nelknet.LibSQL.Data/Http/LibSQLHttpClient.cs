#nullable disable warnings

using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Nelknet.LibSQL.Data.Exceptions;

namespace Nelknet.LibSQL.Data.Http;

/// <summary>
/// HTTP client for libSQL remote connections using the Hrana protocol.
/// </summary>
internal sealed class LibSQLHttpClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly string _authToken;
    private readonly string _baseUrl;
    private bool _disposed;

    public LibSQLHttpClient(string url, string authToken)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL cannot be null or empty", nameof(url));

        _authToken = authToken;
        _baseUrl = NormalizeUrl(url);
        
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_baseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };
        
        // Only add authorization header if token is provided
        if (!string.IsNullOrWhiteSpace(_authToken))
        {
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _authToken);
        }
        
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Nelknet.LibSQL/1.0");

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    /// <summary>
    /// Executes a batch of Hrana requests.
    /// </summary>
    public async Task<HranaBatchResponse> ExecuteBatchAsync(HranaBatchRequest batch, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        try
        {
            var json = JsonSerializer.Serialize(batch, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("v2/pipeline", content, cancellationToken).ConfigureAwait(false);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                throw new LibSQLHttpException(
                    $"HTTP {(int)response.StatusCode} {response.StatusCode}: {response.ReasonPhrase}",
                    (int)response.StatusCode,
                    errorContent,
                    json);
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var result = JsonSerializer.Deserialize<HranaBatchResponse>(responseJson, _jsonOptions);
            
            if (result == null)
                throw new LibSQLException("Failed to deserialize response from server");

            // Check for errors in the batch results
            foreach (var batchResult in result.Results)
            {
                if (batchResult.Response?.Type == HranaTypes.Error && batchResult.Response.Error != null)
                {
                    throw new LibSQLException($"SQL Error: {batchResult.Response.Error.Message}");
                }
            }

            return result;
        }
        catch (HttpRequestException ex)
        {
            throw new LibSQLConnectionException("Failed to connect to remote libSQL server", ex);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            throw new LibSQLException("Request timed out", ex);
        }
        catch (JsonException ex)
        {
            throw new LibSQLException("Failed to parse response from server", ex);
        }
    }

    /// <summary>
    /// Tests the connection to the remote server.
    /// </summary>
    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var batch = new HranaBatchRequest();
            batch.Requests.Add(new HranaRequest
            {
                Type = HranaTypes.Execute,
                Statement = new HranaStatement
                {
                    Sql = "SELECT 1",
                    Args = null
                }
            });

            var response = await ExecuteBatchAsync(batch, cancellationToken).ConfigureAwait(false);
            return response.Results.Count > 0 && response.Results[0].Type == HranaTypes.Ok;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Normalizes the URL for libSQL connections.
    /// </summary>
    private static string NormalizeUrl(string url)
    {
        // Convert libsql:// to https://
        if (url.StartsWith("libsql://", StringComparison.OrdinalIgnoreCase))
        {
            url = string.Concat("https://", url.AsSpan(9));
        }

        // Ensure it ends with a slash for proper BaseAddress
        if (!url.EndsWith('/'))
        {
            url += "/";
        }

        return url;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _httpClient?.Dispose();
            _disposed = true;
        }
    }
}

/// <summary>
/// HTTP-specific exception for libSQL connections.
/// </summary>
public sealed class LibSQLHttpException : LibSQLException
{
    /// <summary>
    /// Gets the HTTP status code associated with this exception.
    /// </summary>
    public int StatusCode { get; }
    
    /// <summary>
    /// Gets the response content from the server, if available.
    /// </summary>
    public string? ResponseContent { get; }
    
    /// <summary>
    /// Gets the request content that was sent to the server, if available.
    /// </summary>
    public string? RequestContent { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="LibSQLHttpException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="responseContent">The response content from the server.</param>
    /// <param name="requestContent">The request content sent to the server.</param>
    public LibSQLHttpException(string message, int statusCode, string? responseContent = null, string? requestContent = null)
        : base(message)
    {
        StatusCode = statusCode;
        ResponseContent = responseContent;
        RequestContent = requestContent;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LibSQLHttpException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="innerException">The inner exception.</param>
    /// <param name="responseContent">The response content from the server.</param>
    /// <param name="requestContent">The request content sent to the server.</param>
    public LibSQLHttpException(string message, int statusCode, Exception innerException, string? responseContent = null, string? requestContent = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        ResponseContent = responseContent;
        RequestContent = requestContent;
    }
}