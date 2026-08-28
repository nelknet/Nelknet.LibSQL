using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Nelknet.LibSQL.Data;
using Nelknet.LibSQL.Data.Exceptions;
using Nelknet.LibSQL.Data.Http;

namespace Nelknet.LibSQL.Tests;

public sealed class HranaHttpProtocolTests
{
    [Fact]
    public async Task ExecuteBatchAsync_TopLevelError_ThrowsServerMessage()
    {
        const string responseBody =
            "{\"baton\":null,\"base_url\":null,\"results\":[{\"type\":\"error\",\"error\":{\"message\":\"no such table: missing_table\",\"code\":\"SQLITE_ERROR\"}}]}";

        await using var server = new ScriptedHttpServer(responseBody);
        using var client = new LibSQLHttpClient(server.BaseUri.ToString(), string.Empty);

        var exception = await Assert.ThrowsAsync<LibSQLException>(
            () => client.ExecuteBatchAsync(CreateSelectBatch()));

        Assert.Contains("no such table: missing_table", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteBatchAsync_TopLevelErrorWithoutDetails_StillThrows()
    {
        const string responseBody =
            "{\"baton\":null,\"base_url\":null,\"results\":[{\"type\":\"error\"}]}";

        await using var server = new ScriptedHttpServer(responseBody);
        using var client = new LibSQLHttpClient(server.BaseUri.ToString(), string.Empty);

        var exception = await Assert.ThrowsAsync<LibSQLException>(
            () => client.ExecuteBatchAsync(CreateSelectBatch()));

        Assert.Contains("Unknown server error", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteBatchAsync_ResponseBaseUrl_RoutesNextRequestWithBaton()
    {
        await using var redirectedServer = new ScriptedHttpServer(SuccessResponse());
        var firstResponse = SuccessResponse(
            baton: "baton-1",
            baseUrl: redirectedServer.BaseUri.ToString());
        await using var originalServer = new ScriptedHttpServer(firstResponse, SuccessResponse());
        using var client = new LibSQLHttpClient(originalServer.BaseUri.ToString(), string.Empty);

        await client.ExecuteBatchAsync(CreateSelectBatch());
        await client.ExecuteBatchAsync(CreateSelectBatch());

        Assert.Single(originalServer.RequestBodies);
        var redirectedRequest = Assert.Single(redirectedServer.RequestBodies);

        using var document = JsonDocument.Parse(redirectedRequest);
        Assert.Equal("baton-1", document.RootElement.GetProperty("baton").GetString());
    }

    [Fact]
    public async Task ExecuteBatchAsync_ConcurrentRequests_SerializesStreamState()
    {
        await using var redirectedServer = new ScriptedHttpServer(SuccessResponse());
        var firstResponse = SuccessResponse(
            baton: "baton-1",
            baseUrl: redirectedServer.BaseUri.ToString());
        await using var originalServer = new ScriptedHttpServer(firstResponse, SuccessResponse());
        using var client = new LibSQLHttpClient(originalServer.BaseUri.ToString(), string.Empty);

        await Task.WhenAll(
            client.ExecuteBatchAsync(CreateSelectBatch()),
            client.ExecuteBatchAsync(CreateSelectBatch()));

        Assert.Single(originalServer.RequestBodies);
        var redirectedRequest = Assert.Single(redirectedServer.RequestBodies);

        using var document = JsonDocument.Parse(redirectedRequest);
        Assert.Equal("baton-1", document.RootElement.GetProperty("baton").GetString());
    }

    [Fact]
    public async Task ExecuteBatchAsync_NonHttpBaseUrl_RejectsRedirect()
    {
        await using var server = new ScriptedHttpServer(
            SuccessResponse(baton: "baton-1", baseUrl: "file:///tmp/libsql/"));
        using var client = new LibSQLHttpClient(server.BaseUri.ToString(), string.Empty);

        var exception = await Assert.ThrowsAsync<LibSQLException>(
            () => client.ExecuteBatchAsync(CreateSelectBatch()));

        Assert.Contains("invalid Hrana base URL", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RemoteConnections_SequentialLifetimes_ReuseTcpConnection()
    {
        await using var server = new ScriptedHttpServer(SuccessResponse(), SuccessResponse());
        var connectionString = $"Data Source={server.BaseUri}";

        using (var firstConnection = new LibSQLConnection(connectionString))
        {
            firstConnection.Open();
        }

        using (var secondConnection = new LibSQLConnection(connectionString))
        {
            secondConnection.Open();
        }

        Assert.Equal(1, server.AcceptedConnectionCount);
    }

    [Fact]
    public async Task RemoteConnection_InjectedHttpClient_RemainsUsableAfterClose()
    {
        using var handler = new RecordingHttpMessageHandler();
        using var httpClient = new HttpClient(handler);

        using (var connection = new LibSQLConnection("Data Source=https://example.test", httpClient))
        {
            connection.Open();
        }

        using var response = await httpClient.GetAsync("https://example.test/health");

        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal(2, handler.RequestCount);
    }

    [Fact]
    public void RemoteConnections_InjectedHttpClient_IsolatesAuthorization()
    {
        using var handler = new RecordingHttpMessageHandler();
        using var httpClient = new HttpClient(handler);

        using (var firstConnection = new LibSQLConnection(
            "Data Source=https://example.test;Auth Token=first-token",
            httpClient))
        {
            firstConnection.Open();
        }

        using (var secondConnection = new LibSQLConnection(
            "Data Source=https://example.test;Auth Token=second-token",
            httpClient))
        {
            secondConnection.Open();
        }

        Assert.Equal(
            ["Bearer first-token", "Bearer second-token"],
            handler.AuthorizationHeaders);
    }

    private static HranaBatchRequest CreateSelectBatch()
    {
        var batch = new HranaBatchRequest();
        batch.Requests.Add(new HranaRequest
        {
            Type = HranaTypes.Execute,
            Statement = new HranaStatement
            {
                Sql = "SELECT 1",
                Args = null,
            },
        });
        return batch;
    }

    private static string SuccessResponse(string? baton = null, string? baseUrl = null)
    {
        var batonJson = baton is null ? "null" : JsonSerializer.Serialize(baton);
        var baseUrlJson = baseUrl is null ? "null" : JsonSerializer.Serialize(baseUrl);
        return $"{{\"baton\":{batonJson},\"base_url\":{baseUrlJson},\"results\":[{{\"type\":\"ok\",\"response\":{{\"type\":\"execute\",\"result\":{{\"cols\":[],\"rows\":[],\"affected_row_count\":0}}}}}}]}}";
    }

    private sealed class ScriptedHttpServer : IAsyncDisposable
    {
        private readonly TcpListener _listener;
        private readonly CancellationTokenSource _stop = new();
        private readonly ConcurrentQueue<string> _responses;
        private readonly Task _serverTask;
        private int _acceptedConnectionCount;

        internal ScriptedHttpServer(params string[] responses)
        {
            _responses = new ConcurrentQueue<string>(responses);
            _listener = new TcpListener(IPAddress.Loopback, 0);
            _listener.Start();
            var port = ((IPEndPoint)_listener.LocalEndpoint).Port;
            BaseUri = new Uri($"http://127.0.0.1:{port}/");
            _serverTask = RunAsync();
        }

        internal Uri BaseUri { get; }

        internal ConcurrentQueue<string> RequestBodies { get; } = new();

        internal int AcceptedConnectionCount => Volatile.Read(ref _acceptedConnectionCount);

        public async ValueTask DisposeAsync()
        {
            _stop.Cancel();
            _listener.Stop();

            try
            {
                await _serverTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            catch (SocketException) when (_stop.IsCancellationRequested)
            {
            }

            _stop.Dispose();
        }

        private async Task RunAsync()
        {
            while (!_stop.IsCancellationRequested)
            {
                using var socket = await _listener.AcceptTcpClientAsync(_stop.Token).ConfigureAwait(false);
                Interlocked.Increment(ref _acceptedConnectionCount);
                await using var stream = socket.GetStream();
                while (!_stop.IsCancellationRequested)
                {
                    var requestBody = await ReadRequestBodyAsync(stream, _stop.Token).ConfigureAwait(false);
                    if (requestBody is null)
                    {
                        break;
                    }

                    RequestBodies.Enqueue(requestBody);

                    if (!_responses.TryDequeue(out var responseBody))
                    {
                        responseBody = SuccessResponse();
                    }

                    await WriteResponseAsync(stream, responseBody, _stop.Token).ConfigureAwait(false);
                }
            }
        }

        private static async Task<string?> ReadRequestBodyAsync(
            NetworkStream stream,
            CancellationToken cancellationToken)
        {
            var request = new StringBuilder();
            var buffer = new byte[4096];
            var headerEnd = -1;
            var contentLength = 0;

            while (true)
            {
                var bytesRead = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                if (bytesRead == 0)
                {
                    if (request.Length == 0)
                    {
                        return null;
                    }

                    break;
                }

                request.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));

                if (headerEnd < 0)
                {
                    headerEnd = request.ToString().IndexOf("\r\n\r\n", StringComparison.Ordinal);
                    if (headerEnd >= 0)
                    {
                        var headers = request.ToString(0, headerEnd);
                        var contentLengthHeader = headers
                            .Split("\r\n", StringSplitOptions.RemoveEmptyEntries)
                            .Single(line => line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase));
                        contentLength = int.Parse(
                            contentLengthHeader["Content-Length:".Length..].Trim(),
                            System.Globalization.CultureInfo.InvariantCulture);
                        headerEnd += 4;
                    }
                }

                if (headerEnd >= 0 && request.Length - headerEnd >= contentLength)
                {
                    return request.ToString(headerEnd, contentLength);
                }
            }

            throw new InvalidOperationException("The test server received an incomplete HTTP request.");
        }

        private static async Task WriteResponseAsync(
            NetworkStream stream,
            string responseBody,
            CancellationToken cancellationToken)
        {
            var bodyBytes = Encoding.UTF8.GetBytes(responseBody);
            var headerBytes = Encoding.ASCII.GetBytes(
                $"HTTP/1.1 200 OK\r\nContent-Type: application/json\r\nContent-Length: {bodyBytes.Length}\r\nConnection: keep-alive\r\n\r\n");
            await stream.WriteAsync(headerBytes, cancellationToken).ConfigureAwait(false);
            await stream.WriteAsync(bodyBytes, cancellationToken).ConfigureAwait(false);
        }
    }

    private sealed class RecordingHttpMessageHandler : HttpMessageHandler
    {
        private readonly List<string?> _authorizationHeaders = [];

        internal IReadOnlyList<string?> AuthorizationHeaders => _authorizationHeaders;

        internal int RequestCount => _authorizationHeaders.Count;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            _authorizationHeaders.Add(request.Headers.Authorization?.ToString());
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(SuccessResponse(), Encoding.UTF8, "application/json"),
            });
        }
    }
}
