#nullable disable warnings

using System;
using System.Net.Http;
using System.Net.Http.Headers;
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
    private static readonly HttpClient SharedHttpClient = CreateSharedHttpClient();
    private readonly HttpClient _httpClient;
    private readonly AuthenticationHeaderValue? _authorization;
    private readonly SemaphoreSlim _pipelineLock = new(1, 1);
    private Uri _streamBaseUri;
    private string? _baton;
    private bool _disposed;

    public LibSQLHttpClient(string url, string authToken)
        : this(url, authToken, SharedHttpClient)
    {
    }

    public LibSQLHttpClient(string url, string authToken, HttpClient httpClient)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL cannot be null or empty", nameof(url));
        ArgumentNullException.ThrowIfNull(httpClient);

        _streamBaseUri = ParseStreamBaseUri(NormalizeUrl(url), currentBaseUri: null);
        _httpClient = httpClient;

        if (!string.IsNullOrWhiteSpace(authToken))
        {
            _authorization = new AuthenticationHeaderValue("Bearer", authToken);
        }
    }

    /// <summary>
    /// Executes a batch of Hrana requests.
    /// </summary>
    public async Task<HranaBatchResponse> ExecuteBatchAsync(HranaBatchRequest batch, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(batch);

        await _pipelineLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            batch.Baton = _baton;

            var json = JsonSerializer.Serialize(batch, HranaJsonSerializerContext.Default.HranaBatchRequest);
            var pipelineUri = new Uri(_streamBaseUri, "v2/pipeline");
            using var request = new HttpRequestMessage(HttpMethod.Post, pipelineUri)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };
            request.Headers.Authorization = _authorization;

            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            
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
            var result = JsonSerializer.Deserialize(responseJson, HranaJsonSerializerContext.Default.HranaBatchResponse);
            
            if (result == null)
                throw new LibSQLException("Failed to deserialize response from server");

            if (result.Results == null)
                throw new LibSQLException("Server returned an invalid Hrana response");

            _baton = result.Baton;

            if (!string.IsNullOrWhiteSpace(result.BaseUrl))
            {
                _streamBaseUri = ParseStreamBaseUri(result.BaseUrl, _streamBaseUri);
            }

            // Check for errors in the batch results
            foreach (var batchResult in result.Results)
            {
                if (batchResult.Type == HranaTypes.Error)
                {
                    throw CreateHranaException(batchResult.Error);
                }

                if (batchResult.Response?.Type == HranaTypes.Error)
                {
                    throw CreateHranaException(batchResult.Response.Error);
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
        finally
        {
            _pipelineLock.Release();
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

        return url;
    }

    private static HttpClient CreateSharedHttpClient()
    {
        var handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(15),
            UseCookies = false,
        };
        var httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(30),
        };
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Nelknet.LibSQL/1.0");
        return httpClient;
    }

    private static Uri ParseStreamBaseUri(string baseUrl, Uri? currentBaseUri)
    {
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri)
            || (baseUri.Scheme != Uri.UriSchemeHttp && baseUri.Scheme != Uri.UriSchemeHttps)
            || !string.IsNullOrEmpty(baseUri.UserInfo)
            || (currentBaseUri?.Scheme == Uri.UriSchemeHttps && baseUri.Scheme != Uri.UriSchemeHttps))
        {
            throw new LibSQLException("Server returned an invalid Hrana base URL");
        }

        var builder = new UriBuilder(baseUri)
        {
            Fragment = string.Empty,
            Query = string.Empty,
            Path = baseUri.AbsolutePath.EndsWith('/')
                ? baseUri.AbsolutePath
                : baseUri.AbsolutePath + "/",
        };
        return builder.Uri;
    }

    private static LibSQLException CreateHranaException(HranaError? error)
    {
        var message = string.IsNullOrWhiteSpace(error?.Message)
            ? "Unknown server error"
            : error.Message;
        return new LibSQLException($"SQL Error: {message}");
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _baton = null;
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
