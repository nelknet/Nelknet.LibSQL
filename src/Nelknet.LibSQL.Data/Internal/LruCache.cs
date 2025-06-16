using System;
using System.Collections.Generic;

namespace Nelknet.LibSQL.Data.Internal
{
    /// <summary>
    /// A simple thread-safe LRU (Least Recently Used) cache implementation.
    /// </summary>
    internal sealed class LruCache<TKey, TValue> where TKey : notnull
    {
        private readonly int _capacity;
        private readonly Dictionary<TKey, LinkedListNode<CacheItem>> _cache;
        private readonly LinkedList<CacheItem> _lruList;
        private readonly object _lock = new();

        public LruCache(int capacity)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be greater than 0");

            _capacity = capacity;
            _cache = new Dictionary<TKey, LinkedListNode<CacheItem>>(capacity);
            _lruList = new LinkedList<CacheItem>();
        }

        /// <summary>
        /// Gets the current number of items in the cache.
        /// </summary>
        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _cache.Count;
                }
            }
        }

        /// <summary>
        /// Tries to get a value from the cache.
        /// </summary>
        public bool TryGetValue(TKey key, out TValue value)
        {
            lock (_lock)
            {
                if (_cache.TryGetValue(key, out var node))
                {
                    // Move to front (most recently used)
                    _lruList.Remove(node);
                    _lruList.AddFirst(node);
                    value = node.Value.Value;
                    return true;
                }

                value = default!;
                return false;
            }
        }

        /// <summary>
        /// Adds or updates a value in the cache.
        /// </summary>
        public void AddOrUpdate(TKey key, TValue value)
        {
            lock (_lock)
            {
                if (_cache.TryGetValue(key, out var node))
                {
                    // Update existing item and move to front
                    node.Value.Value = value;
                    _lruList.Remove(node);
                    _lruList.AddFirst(node);
                }
                else
                {
                    // Add new item
                    if (_cache.Count >= _capacity)
                    {
                        // Remove least recently used item
                        var lru = _lruList.Last;
                        if (lru != null)
                        {
                            _lruList.RemoveLast();
                            _cache.Remove(lru.Value.Key);
                            lru.Value.Dispose();
                        }
                    }

                    var cacheItem = new CacheItem(key, value);
                    var newNode = _lruList.AddFirst(cacheItem);
                    _cache[key] = newNode;
                }
            }
        }

        /// <summary>
        /// Removes an item from the cache.
        /// </summary>
        public bool Remove(TKey key)
        {
            lock (_lock)
            {
                if (_cache.TryGetValue(key, out var node))
                {
                    _lruList.Remove(node);
                    _cache.Remove(key);
                    node.Value.Dispose();
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Clears all items from the cache.
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                foreach (var node in _lruList)
                {
                    node.Dispose();
                }
                _cache.Clear();
                _lruList.Clear();
            }
        }

        private sealed class CacheItem : IDisposable
        {
            public TKey Key { get; }
            public TValue Value { get; set; }

            public CacheItem(TKey key, TValue value)
            {
                Key = key;
                Value = value;
            }

            public void Dispose()
            {
                if (Value is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }
    }
}