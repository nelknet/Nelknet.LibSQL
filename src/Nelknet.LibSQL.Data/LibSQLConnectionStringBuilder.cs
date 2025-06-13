#nullable disable warnings

using System;
using System.Collections.Generic;
using System.Data.Common;

namespace Nelknet.LibSQL.Data;

/// <summary>
/// Provides a simple way to create and manage the contents of connection strings used by the <see cref="LibSQLConnection"/> class.
/// </summary>
public sealed class LibSQLConnectionStringBuilder : DbConnectionStringBuilder
{
    private static readonly HashSet<string> ValidKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "Data Source",
        "DataSource",
        "Database",
        "DB",
        "Uri",
        "Url",
        "Auth Token",
        "AuthToken",
        "Token",
        "Encryption Key",
        "EncryptionKey",
        "Key",
        "Sync URL",
        "SyncURL",
        "SyncUrl",
        "Sync Auth Token",
        "SyncAuthToken",
        "SyncToken",
        "Read Your Writes",
        "ReadYourWrites",
        "Mode"
    };

    private const string InMemoryConnectionString = ":memory:";
    private const string SharedMemoryConnectionString = ":memory:?cache=shared";

    private string? _dataSource;
    private string? _authToken;
    private string? _encryptionKey;
    private string? _syncUrl;
    private string? _syncAuthToken;
    private bool _readYourWrites = true;
    private LibSQLConnectionMode _mode = LibSQLConnectionMode.Local;

    /// <summary>
    /// Initializes a new instance of the <see cref="LibSQLConnectionStringBuilder"/> class.
    /// </summary>
    public LibSQLConnectionStringBuilder()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LibSQLConnectionStringBuilder"/> class with the specified connection string.
    /// </summary>
    /// <param name="connectionString">The connection string to parse.</param>
    public LibSQLConnectionStringBuilder(string connectionString)
    {
        ConnectionString = connectionString;
        RefreshFromBase();
    }

    /// <summary>
    /// Gets or sets the data source (file path, URL, or special value like :memory:).
    /// </summary>
    public string? DataSource
    {
        get => _dataSource;
        set
        {
            _dataSource = value;
            base["Data Source"] = value;
            UpdateMode();
        }
    }

    /// <summary>
    /// Gets or sets the authentication token for remote connections.
    /// </summary>
    public string? AuthToken
    {
        get => _authToken;
        set
        {
            _authToken = value;
            base["Auth Token"] = value;
        }
    }

    /// <summary>
    /// Gets or sets the encryption key for encrypted databases.
    /// </summary>
    public string? EncryptionKey
    {
        get => _encryptionKey;
        set
        {
            _encryptionKey = value;
            base["Encryption Key"] = value;
        }
    }

    /// <summary>
    /// Gets or sets the sync URL for embedded replica connections.
    /// </summary>
    public string? SyncUrl
    {
        get => _syncUrl;
        set
        {
            _syncUrl = value;
            base["Sync URL"] = value;
            UpdateMode();
        }
    }

    /// <summary>
    /// Gets or sets the authentication token for sync operations.
    /// </summary>
    public string? SyncAuthToken
    {
        get => _syncAuthToken;
        set
        {
            _syncAuthToken = value;
            base["Sync Auth Token"] = value;
        }
    }

    /// <summary>
    /// Gets or sets whether to enable read-your-writes consistency for sync operations.
    /// </summary>
    public bool ReadYourWrites
    {
        get => _readYourWrites;
        set
        {
            _readYourWrites = value;
            base["Read Your Writes"] = value;
        }
    }

    /// <summary>
    /// Gets the connection mode based on the current configuration.
    /// </summary>
    public LibSQLConnectionMode Mode => _mode;

    /// <summary>
    /// Gets or sets the value associated with the specified key.
    /// </summary>
    /// <param name="keyword">The key of the item to get or set.</param>
    /// <returns>The value associated with the specified key.</returns>
    public override object this[string keyword]
    {
        get => base[keyword];
        set
        {
            if (!IsValidKeyword(keyword))
            {
                throw new ArgumentException($"Keyword not supported: '{keyword}'", nameof(keyword));
            }

            var normalizedKeyword = NormalizeKeyword(keyword);
            switch (normalizedKeyword)
            {
                case "Data Source":
                    DataSource = value?.ToString();
                    break;
                case "Auth Token":
                    AuthToken = value?.ToString();
                    break;
                case "Encryption Key":
                    EncryptionKey = value?.ToString();
                    break;
                case "Sync URL":
                    SyncUrl = value?.ToString();
                    break;
                case "Sync Auth Token":
                    SyncAuthToken = value?.ToString();
                    break;
                case "Read Your Writes":
                    ReadYourWrites = Convert.ToBoolean(value);
                    break;
                default:
                    base[keyword] = value;
                    break;
            }
        }
    }

    /// <summary>
    /// Creates an in-memory connection string.
    /// </summary>
    /// <returns>A connection string for an in-memory database.</returns>
    public static string CreateInMemoryConnectionString()
    {
        return $"Data Source={InMemoryConnectionString}";
    }

    /// <summary>
    /// Creates a shared in-memory connection string.
    /// </summary>
    /// <returns>A connection string for a shared in-memory database.</returns>
    public static string CreateSharedMemoryConnectionString()
    {
        return $"Data Source={SharedMemoryConnectionString}";
    }

    /// <summary>
    /// Determines whether the specified keyword is valid.
    /// </summary>
    /// <param name="keyword">The keyword to validate.</param>
    /// <returns>true if the keyword is valid; otherwise, false.</returns>
    private static bool IsValidKeyword(string keyword)
    {
        return ValidKeywords.Contains(keyword);
    }

    /// <summary>
    /// Normalizes the keyword to a standard form.
    /// </summary>
    /// <param name="keyword">The keyword to normalize.</param>
    /// <returns>The normalized keyword.</returns>
    private static string NormalizeKeyword(string keyword)
    {
        return keyword.ToLowerInvariant() switch
        {
            "datasource" or "database" or "db" or "uri" or "url" => "Data Source",
            "authtoken" or "token" => "Auth Token",
            "encryptionkey" or "key" => "Encryption Key",
            "syncurl" => "Sync URL",
            "syncauthtoken" or "synctoken" => "Sync Auth Token",
            "readyourwrites" => "Read Your Writes",
            _ => keyword
        };
    }

    /// <summary>
    /// Updates the connection mode based on the current configuration.
    /// </summary>
    private void UpdateMode()
    {
        if (string.IsNullOrWhiteSpace(_dataSource))
        {
            _mode = LibSQLConnectionMode.Local;
        }
        else if (_dataSource.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                 _dataSource.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                 _dataSource.StartsWith("libsql://", StringComparison.OrdinalIgnoreCase))
        {
            _mode = !string.IsNullOrWhiteSpace(_syncUrl) 
                ? LibSQLConnectionMode.EmbeddedReplica 
                : LibSQLConnectionMode.Remote;
        }
        else
        {
            _mode = !string.IsNullOrWhiteSpace(_syncUrl) 
                ? LibSQLConnectionMode.EmbeddedReplica 
                : LibSQLConnectionMode.Local;
        }
    }

    /// <summary>
    /// Clears the contents of the <see cref="LibSQLConnectionStringBuilder"/> instance.
    /// </summary>
    public override void Clear()
    {
        base.Clear();
        _dataSource = null;
        _authToken = null;
        _encryptionKey = null;
        _syncUrl = null;
        _syncAuthToken = null;
        _readYourWrites = true;
        _mode = LibSQLConnectionMode.Local;
    }

    /// <summary>
    /// Determines whether the <see cref="LibSQLConnectionStringBuilder"/> contains a specific key.
    /// </summary>
    /// <param name="keyword">The key to locate in the <see cref="LibSQLConnectionStringBuilder"/>.</param>
    /// <returns>true if the <see cref="LibSQLConnectionStringBuilder"/> contains an element with the specified key; otherwise, false.</returns>
    public override bool ContainsKey(string keyword)
    {
        // A connection string builder "contains" a key if it's a valid keyword,
        // regardless of whether it has a value
        return IsValidKeyword(keyword);
    }

    /// <summary>
    /// Removes the entry with the specified key from the <see cref="LibSQLConnectionStringBuilder"/> instance.
    /// </summary>
    /// <param name="keyword">The key of the key/value pair to be removed.</param>
    /// <returns>true if the key existed within the connection string and was removed; otherwise, false.</returns>
    public override bool Remove(string keyword)
    {
        if (!IsValidKeyword(keyword))
        {
            return false;
        }

        var normalizedKeyword = NormalizeKeyword(keyword);
        var result = base.Remove(normalizedKeyword);

        if (result)
        {
            switch (normalizedKeyword)
            {
                case "Data Source":
                    _dataSource = null;
                    UpdateMode();
                    break;
                case "Auth Token":
                    _authToken = null;
                    break;
                case "Encryption Key":
                    _encryptionKey = null;
                    break;
                case "Sync URL":
                    _syncUrl = null;
                    UpdateMode();
                    break;
                case "Sync Auth Token":
                    _syncAuthToken = null;
                    break;
                case "Read Your Writes":
                    _readYourWrites = true;
                    break;
            }
        }

        return result;
    }

    /// <summary>
    /// Retrieves a value corresponding to the supplied key from this <see cref="LibSQLConnectionStringBuilder"/>.
    /// </summary>
    /// <param name="keyword">The key of the item to retrieve.</param>
    /// <param name="value">The value corresponding to the key.</param>
    /// <returns>true if keyword was found within the connection string; otherwise, false.</returns>
    public override bool TryGetValue(string keyword, out object? value)
    {
        if (!IsValidKeyword(keyword))
        {
            value = null;
            return false;
        }

        return base.TryGetValue(NormalizeKeyword(keyword), out value);
    }

    /// <summary>
    /// Refreshes the internal fields from the base class values.
    /// </summary>
    private void RefreshFromBase()
    {
        // Parse the values from the base class
        if (base.TryGetValue("Data Source", out var dataSource))
            _dataSource = dataSource?.ToString();
        
        if (base.TryGetValue("Auth Token", out var authToken))
            _authToken = authToken?.ToString();
            
        if (base.TryGetValue("Encryption Key", out var encryptionKey))
            _encryptionKey = encryptionKey?.ToString();
            
        if (base.TryGetValue("Sync URL", out var syncUrl))
            _syncUrl = syncUrl?.ToString();
            
        if (base.TryGetValue("Sync Auth Token", out var syncAuthToken))
            _syncAuthToken = syncAuthToken?.ToString();
            
        if (base.TryGetValue("Read Your Writes", out var readYourWrites))
            _readYourWrites = Convert.ToBoolean(readYourWrites);
            
        UpdateMode();
    }
}

/// <summary>
/// Specifies the connection mode for libSQL.
/// </summary>
public enum LibSQLConnectionMode
{
    /// <summary>
    /// Local file-based database connection.
    /// </summary>
    Local,

    /// <summary>
    /// Remote HTTP-based database connection.
    /// </summary>
    Remote,

    /// <summary>
    /// Embedded replica with sync capabilities.
    /// </summary>
    EmbeddedReplica
}