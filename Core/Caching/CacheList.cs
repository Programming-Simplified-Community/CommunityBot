namespace Core.Caching;

public class CacheList<T>
{
    private List<T> items = new();
    private readonly TimeSpan _refreshInterval = TimeSpan.FromSeconds(30);
    private DateTime _nextUpdate = DateTime.Now.AddSeconds(-30);
    private Func<Task<List<T>>> _refreshFunc;

    public CacheList(Func<Task<List<T>>> refreshFunc)
    {
        _refreshFunc = refreshFunc;
    }

    private async Task<bool> Contains(T value)
    {
        var now = DateTime.Now;

        if (items.Any() && now < _nextUpdate) return items.Contains(value);
        
        items = await _refreshFunc();
        _nextUpdate = DateTime.Now.Add(_refreshInterval);

        return items.Contains(value);
    }

    public async Task<List<T>> GetAll()
    {
        var now = DateTime.Now;
        if (!items.Any() || now >= _nextUpdate)
            items = await _refreshFunc();

        return items;
    }

    public async Task<T?> Get(T value)
    {
        if (await Contains(value))
            return items.FirstOrDefault(x => x.Equals(value));
        return default;
    }
}