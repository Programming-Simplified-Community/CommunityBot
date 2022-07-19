using System.Collections.Concurrent;

namespace Core.Caching;

/// <summary>
/// Wrapper class created to assist with caching items, and refreshing their values overtime.
/// Originally designed to ingest Discord data to help speed things up and reduce rate-limiting
/// </summary>
/// <typeparam name="TKey">The primary key, such as a GuildId, to group things by</typeparam>
/// <typeparam name="TSubKey">Item under <typeparamref name="TKey"/>, such as a role or user Id</typeparam>
/// <typeparam name="TValue">Associated value we're after</typeparam>
/// <typeparam name="TSource">The object in which we will use to refresh our data</typeparam>
public class CacheItem<TKey, TSubKey, TValue, TSource>
{
    /// <summary>
    /// Behaves like a 'Time to live' or 'TTL'. 
    /// </summary>
    private readonly TimeSpan _refreshInterval = TimeSpan.FromSeconds(30);
    
    /// <summary>
    /// A function which assists with updating a cache of data after <see cref="_refreshInterval"/>
    /// </summary>
    private Func<TSource, Task<Dictionary<TSubKey, TValue>>> _updateCacheMethod;
    
    private ConcurrentDictionary<TKey, Dictionary<TSubKey, TValue>> _cache = new();
    
    /// <summary>
    /// When <see cref="DateTime.UtcNow"/> >= this value -- invoke <see cref="_updateCacheMethod"/>
    /// </summary>
    private DateTime _nextUpdate = DateTime.UtcNow.AddSeconds(-30);
    
    public CacheItem(Func<TSource, Task<Dictionary<TSubKey, TValue>>> updateMethod, TimeSpan? refreshInterval = null)
    {
        _updateCacheMethod = updateMethod;

        if (refreshInterval is not null)
            _refreshInterval = refreshInterval.Value;
    }
    
    /// <summary>
    /// Checks to see if <paramref name="key"/> is currently being tracked by our cache
    /// </summary>
    /// <param name="key"></param>
    /// <returns>True if it exists, otherwise false.</returns>
    public bool ContainsKey(TKey key) => _cache.ContainsKey(key);

    /// <summary>
    /// Checks to see if <paramref name="key"/> and <paramref name="sub"/> are both
    /// being tracked by our cache
    /// </summary>
    /// <param name="key"></param>
    /// <param name="sub"></param>
    /// <returns>True if they both exist, otherwise false</returns>
    public bool ContainsSubKey(TKey key, TSubKey sub)
         => ContainsKey(key) && _cache[key].ContainsKey(sub);

    /// <summary>
    /// Retrieve a group of data from cache
    /// </summary>
    /// <param name="key">Group Key to grab</param>
    /// <param name="source">The data source that we'll use to update our data if applicable</param>
    /// <returns>Our data!</returns>
    public async Task<Dictionary<TSubKey, TValue>> GetDict(TKey key, TSource source)
    {
        var now = DateTime.UtcNow;
        Dictionary<TSubKey, TValue> results;

        // If our cache is currently empty, or if it's time to update cache
        // we shall perform data retrieval!
        if (!_cache.Any() || now >= _nextUpdate || !_cache.ContainsKey(key))
        {
            results = await _updateCacheMethod(source);

            if (!_cache.ContainsKey(key))
                _cache.TryAdd(key, results);
            else _cache[key] = results;

            _nextUpdate = now + _refreshInterval;
        }
        else 
            results = _cache[key];

        return results;
    }

    /// <summary>
    /// Set a value under <paramref name="key"/>, associated to <paramref name="sub"/>
    /// </summary>
    /// <param name="key">Group Id</param>
    /// <param name="sub">Value Id</param>
    /// <param name="value">Value itself</param>
    public void Set(TKey key, TSubKey sub, TValue value)
    {
        // We shall only update value if the group exists
        if (!ContainsKey(key)) return;
            
        if (_cache[key].ContainsKey(sub))
            _cache[key][sub] = value;
        else
        {
            _cache[key] = new();
            _cache[key].TryAdd(sub, value);
        }
    }
}