using DATN.Application.Interfaces.Services;
using Microsoft.Extensions.Caching.Memory;

namespace DATN.Infrastructure.Services;

public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly HashSet<string> _keys = new(StringComparer.Ordinal);
    private readonly object _lock = new();

    public MemoryCacheService(IMemoryCache cache) => _cache = cache;

    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan ttl,
        CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(key, out T? cached) && cached is not null)
        {
            return cached;
        }

        var value = await factory(cancellationToken);

        _cache.Set(key, value, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl
        });

        lock (_lock)
        {
            _keys.Add(key);
        }

        return value;
    }

    public void Remove(string key)
    {
        _cache.Remove(key);
        lock (_lock)
        {
            _keys.Remove(key);
        }
    }

    public void RemoveByPrefix(string prefix)
    {
        List<string> toRemove;
        lock (_lock)
        {
            toRemove = _keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal)).ToList();
        }

        foreach (var key in toRemove)
        {
            Remove(key);
        }
    }
}

