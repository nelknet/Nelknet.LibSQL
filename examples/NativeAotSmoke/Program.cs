using System.Data;
using Nelknet.LibSQL.Data;

await RunLocalAsync();

var remoteUrl = Environment.GetEnvironmentVariable("LIBSQL_TEST_URL");
if (!string.IsNullOrWhiteSpace(remoteUrl))
{
    var authToken = Environment.GetEnvironmentVariable("LIBSQL_TEST_TOKEN") ?? string.Empty;
    await RunRemoteAsync(remoteUrl, authToken);
}

Console.WriteLine("NativeAOT smoke completed.");

static async Task RunLocalAsync()
{
    var databasePath = Path.Combine(AppContext.BaseDirectory, "nativeaot-smoke.db");
    if (File.Exists(databasePath))
        File.Delete(databasePath);

    using var connection = new LibSQLConnection($"Data Source={databasePath}");
    await connection.OpenAsync();

    using (var command = connection.CreateCommand())
    {
        command.CommandText = "CREATE TABLE smoke (id INTEGER PRIMARY KEY, name TEXT NOT NULL)";
        await command.ExecuteNonQueryAsync();

        command.CommandText = "INSERT INTO smoke (name) VALUES (@name)";
        command.Parameters.Add(new LibSQLParameter("@name", "local"));
        var inserted = await command.ExecuteNonQueryAsync();
        Require(inserted == 1, $"Expected 1 inserted row, got {inserted}.");
    }

    using (var command = connection.CreateCommand())
    {
        command.CommandText = "SELECT id, name FROM smoke WHERE name = @name";
        command.Parameters.Add(new LibSQLParameter("@name", "local"));

        using var reader = await command.ExecuteReaderAsync();
        Require(await reader.ReadAsync(), "Expected one local row.");
        Require(reader.GetFieldType(0) == typeof(long), "Expected local id to be Int64.");
        Require(reader.GetString(1) == "local", "Expected local name value.");

        var schema = reader.GetSchemaTable();
        Require(schema != null, "Expected local schema table.");
        Require(schema!.Columns.Contains("DataType"), "Expected DataType schema column.");
    }
}

static async Task RunRemoteAsync(string url, string authToken)
{
    var connectionString = string.IsNullOrEmpty(authToken)
        ? $"Data Source={url}"
        : $"Data Source={url};Auth Token={authToken}";

    using var connection = new LibSQLConnection(connectionString);
    await connection.OpenAsync();

    using var command = connection.CreateCommand();
    command.CommandText = "SELECT @answer AS answer, @name AS name, @payload AS payload";
    command.Parameters.Add(new LibSQLParameter("@name", "remote"));
    command.Parameters.Add(new LibSQLParameter("@payload", DbType.Binary) { Value = new byte[] { 1, 2, 3 } });
    command.Parameters.Add(new LibSQLParameter("@answer", 42));

    using var reader = await command.ExecuteReaderAsync();
    Require(await reader.ReadAsync(), "Expected one remote row.");
    Require(reader.GetInt64(0) == 42, "Expected remote integer value.");
    Require(reader.GetString(1) == "remote", "Expected remote string value.");

    var payload = (byte[])reader.GetValue(2);
    Require(payload.SequenceEqual(new byte[] { 1, 2, 3 }), "Expected remote blob value.");
}

static void Require(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException(message);
}
