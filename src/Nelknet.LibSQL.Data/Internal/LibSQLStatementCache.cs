using System;
using Nelknet.LibSQL.Bindings;

namespace Nelknet.LibSQL.Data.Internal
{
    /// <summary>
    /// Manages a cache of prepared statements for improved performance.
    /// </summary>
    internal sealed class LibSQLStatementCache : IDisposable
    {
        private readonly LruCache<string, CachedStatement> _cache;
        private readonly int _maxStatements;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="LibSQLStatementCache"/> class.
        /// </summary>
        /// <param name="maxStatements">Maximum number of statements to cache. Default is 100.</param>
        public LibSQLStatementCache(int maxStatements = 100)
        {
            _maxStatements = maxStatements;
            _cache = new LruCache<string, CachedStatement>(maxStatements);
        }

        /// <summary>
        /// Gets the current number of cached statements.
        /// </summary>
        public int Count => _cache.Count;

        /// <summary>
        /// Gets the maximum number of statements that can be cached.
        /// </summary>
        public int MaxStatements => _maxStatements;

        /// <summary>
        /// Tries to get a cached statement.
        /// </summary>
        /// <param name="sql">The SQL command text.</param>
        /// <param name="statement">The cached statement if found.</param>
        /// <returns>True if the statement was found in the cache; otherwise, false.</returns>
        public bool TryGetStatement(string sql, out LibSQLStatementHandle? statement)
        {
            if (_cache.TryGetValue(sql, out var cached))
            {
                statement = cached.Statement;
                cached.LastUsed = DateTime.UtcNow;
                cached.UsageCount++;
                return true;
            }

            statement = null;
            return false;
        }

        /// <summary>
        /// Adds a statement to the cache.
        /// </summary>
        /// <param name="sql">The SQL command text.</param>
        /// <param name="statement">The prepared statement handle.</param>
        public void AddStatement(string sql, LibSQLStatementHandle statement)
        {
            var cached = new CachedStatement
            {
                Statement = statement,
                Sql = sql,
                CreatedAt = DateTime.UtcNow,
                LastUsed = DateTime.UtcNow,
                UsageCount = 1
            };

            _cache.AddOrUpdate(sql, cached);
        }

        /// <summary>
        /// Removes a statement from the cache.
        /// </summary>
        /// <param name="sql">The SQL command text.</param>
        /// <returns>True if the statement was removed; otherwise, false.</returns>
        public bool RemoveStatement(string sql)
        {
            return _cache.Remove(sql);
        }

        /// <summary>
        /// Clears all cached statements.
        /// </summary>
        public void Clear()
        {
            _cache.Clear();
        }

        /// <summary>
        /// Disposes of all cached statements and releases resources.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            Clear();
            _disposed = true;
        }

        private sealed class CachedStatement : IDisposable
        {
            public LibSQLStatementHandle? Statement { get; set; }
            public string Sql { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; }
            public DateTime LastUsed { get; set; }
            public long UsageCount { get; set; }

            public void Dispose()
            {
                Statement?.Dispose();
            }
        }
    }
}