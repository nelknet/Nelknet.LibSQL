using System;
using System.Collections.Generic;

namespace Nelknet.LibSQL.Data;

/// <summary>
/// Represents a parsed libSQL connection string.
/// </summary>
public sealed class LibSQLConnectionString
{
    private const string DataSourceKey = "Data Source";
    private const string DataSourceKeyAlias = "DataSource";
    private const string AuthTokenKey = "Auth Token";
    private const string AuthTokenKeyAlias = "AuthToken";
    private const string WithWebPKIKey = "With WebPKI";
    private const string WithWebPKIKeyAlias = "WithWebPKI";

    /// <summary>
    /// Gets the data source (database path or URL).
    /// </summary>
    public string DataSource { get; }

    /// <summary>
    /// Gets the authentication token for remote connections.
    /// </summary>
    public string? AuthToken { get; }

    /// <summary>
    /// Gets whether to use WebPKI for remote connections.
    /// </summary>
    public bool WithWebPKI { get; }

    /// <summary>
    /// Gets whether this is a remote connection (starts with libsql:// or https://).
    /// </summary>
    public bool IsRemote { get; }

    /// <summary>
    /// Gets whether this is a file connection (not remote and not in-memory).
    /// </summary>
    public bool IsFile { get; }

    /// <summary>
    /// Gets whether this is an in-memory connection (:memory:).
    /// </summary>
    public bool IsInMemory { get; }

    private LibSQLConnectionString(string dataSource, string? authToken, bool withWebPKI)
    {
        DataSource = dataSource;
        AuthToken = authToken;
        WithWebPKI = withWebPKI;

        // Determine connection type
        IsRemote = dataSource.StartsWith("libsql://", StringComparison.OrdinalIgnoreCase) ||
                   dataSource.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
        IsInMemory = string.Equals(dataSource, ":memory:", StringComparison.OrdinalIgnoreCase);
        IsFile = !IsRemote && !IsInMemory;
    }

    /// <summary>
    /// Parses a connection string into a <see cref="LibSQLConnectionString"/>.
    /// </summary>
    /// <param name="connectionString">The connection string to parse.</param>
    /// <returns>A parsed <see cref="LibSQLConnectionString"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when the connection string is invalid.</exception>
    public static LibSQLConnectionString Parse(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Connection string cannot be null or empty.", nameof(connectionString));
        }

        var parameters = ParseConnectionString(connectionString);

        // Get data source
        if (!TryGetParameter(parameters, DataSourceKey, DataSourceKeyAlias, out var dataSource))
        {
            throw new ArgumentException("Connection string must contain a 'Data Source' parameter.");
        }

        if (string.IsNullOrWhiteSpace(dataSource))
        {
            throw new ArgumentException("Data Source cannot be empty.");
        }

        // Get optional parameters
        TryGetParameter(parameters, AuthTokenKey, AuthTokenKeyAlias, out var authToken);
        TryGetBooleanParameter(parameters, WithWebPKIKey, WithWebPKIKeyAlias, out var withWebPKI);

        return new LibSQLConnectionString(dataSource, authToken, withWebPKI);
    }

    /// <summary>
    /// Parses a connection string into key-value pairs.
    /// </summary>
    private static Dictionary<string, string> ParseConnectionString(string connectionString)
    {
        var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        
        // Simple connection string parser
        // Supports: Key=Value;Key2=Value2
        var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var part in parts)
        {
            var trimmedPart = part.Trim();
            if (string.IsNullOrEmpty(trimmedPart))
                continue;

            var equalIndex = trimmedPart.IndexOf('=');
            if (equalIndex <= 0 || equalIndex == trimmedPart.Length - 1)
                continue;

            var key = trimmedPart.Substring(0, equalIndex).Trim();
            var value = trimmedPart.Substring(equalIndex + 1).Trim();

            // Remove quotes if present
            if (value.Length >= 2 && 
                ((value.StartsWith("\"") && value.EndsWith("\"")) ||
                 (value.StartsWith("'") && value.EndsWith("'"))))
            {
                value = value.Substring(1, value.Length - 2);
            }

            parameters[key] = value;
        }

        return parameters;
    }

    /// <summary>
    /// Tries to get a parameter value by key or alias.
    /// </summary>
    private static bool TryGetParameter(Dictionary<string, string> parameters, string key, string? alias, out string? value)
    {
        if (parameters.TryGetValue(key, out value))
            return true;

        if (alias != null && parameters.TryGetValue(alias, out value))
            return true;

        value = null;
        return false;
    }

    /// <summary>
    /// Tries to get a boolean parameter value by key or alias.
    /// </summary>
    private static bool TryGetBooleanParameter(Dictionary<string, string> parameters, string key, string? alias, out bool value)
    {
        if (TryGetParameter(parameters, key, alias, out var stringValue))
        {
            if (bool.TryParse(stringValue, out value))
                return true;

            // Also accept 1/0, yes/no, y/n
            switch (stringValue?.ToLowerInvariant())
            {
                case "1":
                case "yes":
                case "y":
                    value = true;
                    return true;
                case "0":
                case "no":
                case "n":
                    value = false;
                    return true;
            }
        }

        value = false;
        return false;
    }
}