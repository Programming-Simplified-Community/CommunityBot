namespace Core.Caching;

/// <summary>
/// Simplified version of <see cref="CacheItem"/>. Except, instead of grouping values we're only
/// working with a List.
///
/// <p>
///     Worth mentioning that the data doesn't get refreshed every <see cref="_refreshInterval"/>.
///     It is only when the data is ACCESSED that this "time to live" is checked.
/// </p>
/// </summary>
/// <typeparam name="T">Type of data to cache</typeparam>
public class CacheList<T>
{
    private List<T> items = new();
    
    /// <summary>
    /// <see cref="items"/> gets refreshed after this period of time.
    /// </summary>
    private readonly TimeSpan _refreshInterval = TimeSpan.FromSeconds(30);
    private DateTime _nextUpdate = DateTime.UtcNow.AddSeconds(-30);
    private Func<Task<List<T>>> _refreshFunc;

    public CacheList(Func<Task<List<T>>> refreshFunc, TimeSpan? refreshInterval = null)
    {
        _refreshFunc = refreshFunc;

        if (refreshInterval is not null)
            _refreshInterval = refreshInterval.Value;
    }
    
    /// <summary>
    /// Checks to determine if <paramref name="value"/> is within our cache
    /// </summary>
    /// <remarks>
    ///  When accessed, if TTL is up, data will be refreshed THEN checked.
    /// </remarks>
    /// <param name="value">Value to check for</param>
    /// <returns>True if present, otherwise false.</returns>
    private async Task<bool> Contains(T value)
    {
        var now = DateTime.UtcNow;

        if (items.Any() && now < _nextUpdate) return items.Contains(value);
        
        items = await _refreshFunc();
        _nextUpdate = DateTime.UtcNow.Add(_refreshInterval);

        return items.Contains(value);
    }
    
    /// <summary>
    /// Retrieve all items. 
    /// </summary>
    /// <remarks>
    /// When accessed, if TTL is up, data will be refreshed THEN checked.
    /// </remarks>
    /// <returns>All items associated with cache</returns>
    public async Task<List<T>> GetAll()
    {
        var now = DateTime.UtcNow;
        if (!items.Any() || now >= _nextUpdate)
            items = await _refreshFunc();

        return items;
    }

    /// <summary>
    /// Retrieve a specific item from cache
    /// </summary>
    /// <remarks>
    ///     This is the only method that does not update the cache
    /// </remarks>
    /// <param name="value">Value to retrieve</param>
    /// <returns></returns>
    public async Task<T?> Get(T value)
    {
        if (await Contains(value))
            return items.FirstOrDefault(x => x.Equals(value));
        return default;
    }
}