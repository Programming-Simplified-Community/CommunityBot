using System.Collections.Concurrent;

namespace Core.Caching;

public class CacheItem<TKey, TSubKey, TValue, TSource>
{
    private readonly TimeSpan _refreshInterval = TimeSpan.FromSeconds(30);
    private Func<TSource, Task<Dictionary<TSubKey, TValue>>> _updateCacheMethod;
    private ConcurrentDictionary<TKey, Dictionary<TSubKey, TValue>> _cache = new();
    private DateTime _nextUpdate = DateTime.Now.AddSeconds(-30);
    
    public CacheItem(Func<TSource, Task<Dictionary<TSubKey, TValue>>> updateMethod)
    {
        
        _updateCacheMethod = updateMethod;
    }

    public bool ContainsKey(TKey key) => _cache.ContainsKey(key);

    public bool ContainsSubJey(TKey key, TSubKey sub)
    {
        if (!ContainsKey(key))
            return false;
        return _cache[key].ContainsKey(sub);
    }

    public async Task<Dictionary<TSubKey, TValue>> GetDict(TKey key, TSource source)
    {
        DateTime now = DateTime.Now;
        Dictionary<TSubKey, TValue> results;

        if (!_cache.Any() || now >= _nextUpdate || !_cache.ContainsKey(key))
        {
            results = await _updateCacheMethod(source);

            if (!_cache.ContainsKey(key))
                _cache.TryAdd(key, results);
            else _cache[key] = results;

            _nextUpdate = now + _refreshInterval;
        }
        else results = _cache[key];

        return results;
    }

    public void Set(TKey key, TSubKey sub, TValue value)
    {
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