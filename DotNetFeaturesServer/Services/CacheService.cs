using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace DotNetFeaturesServer.Services;

/// <summary>
/// Service for caching operations with multiple cache providers
/// </summary>
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class;
    Task RemoveAsync(string key);
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> getItem, TimeSpan? expiry = null) where T : class;
    Task ClearAsync();
    Task<Dictionary<string, object>> GetCacheStatsAsync();
}

public class CacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<CacheService> _logger;
    private readonly Dictionary<string, DateTime> _cacheAccessTimes;
    private readonly object _lockObject = new();
    
    public CacheService(IMemoryCache memoryCache, ILogger<CacheService> logger)
    {
        _memoryCache = memoryCache;
        _logger = logger;
        _cacheAccessTimes = new Dictionary<string, DateTime>();
    }
    
    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        try
        {
            _logger.LogDebug("Getting cache value for key: {Key}", key);
            
            if (_memoryCache.TryGetValue(key, out var cachedValue))
            {
                lock (_lockObject)
                {
                    _cacheAccessTimes[key] = DateTime.UtcNow;
                }
                
                _logger.LogDebug("Cache hit for key: {Key}", key);
                
                if (cachedValue is string jsonString)
                {
                    return JsonSerializer.Deserialize<T>(jsonString);
                }
                
                return cachedValue as T;
            }
            
            _logger.LogDebug("Cache miss for key: {Key}", key);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache value for key: {Key}", key);
            return null;
        }
    }
    
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class
    {
        try
        {
            _logger.LogDebug("Setting cache value for key: {Key}", key);
            
            var options = new MemoryCacheEntryOptions();
            
            if (expiry.HasValue)
            {
                options.AbsoluteExpirationRelativeToNow = expiry.Value;
            }
            else
            {
                options.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30); // Default 30 minutes
            }
            
            // Add eviction callback for logging
            options.RegisterPostEvictionCallback((key, value, reason, state) =>
            {
                _logger.LogDebug("Cache entry evicted - Key: {Key}, Reason: {Reason}", key, reason);
                lock (_lockObject)
                {
                    _cacheAccessTimes.Remove(key.ToString() ?? string.Empty);
                }
            });
            
            // Serialize complex objects to JSON
            object cacheValue;
            if (value is string stringValue)
            {
                cacheValue = stringValue;
            }
            else
            {
                cacheValue = JsonSerializer.Serialize(value);
            }
            
            _memoryCache.Set(key, cacheValue, options);
            
            lock (_lockObject)
            {
                _cacheAccessTimes[key] = DateTime.UtcNow;
            }
            
            _logger.LogDebug("Cache value set for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache value for key: {Key}", key);
        }
    }
    
    public async Task RemoveAsync(string key)
    {
        try
        {
            _logger.LogDebug("Removing cache value for key: {Key}", key);
            _memoryCache.Remove(key);
            
            lock (_lockObject)
            {
                _cacheAccessTimes.Remove(key);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache value for key: {Key}", key);
        }
    }
    
    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> getItem, TimeSpan? expiry = null) where T : class
    {
        var cached = await GetAsync<T>(key);
        if (cached != null)
        {
            return cached;
        }
        
        _logger.LogDebug("Cache miss for key: {Key}, fetching fresh data", key);
        var item = await getItem();
        
        if (item != null)
        {
            await SetAsync(key, item, expiry);
        }
        
        return item;
    }
    
    public async Task ClearAsync()
    {
        try
        {
            _logger.LogInformation("Clearing all cache entries");

            lock (_lockObject)
            {
                if (_memoryCache is IDisposable disposableCache)
                {
                    disposableCache.Dispose();
                }
                _memoryCache = new MemoryCache(new MemoryCacheOptions());
                _cacheAccessTimes.Clear();
            }
            _logger.LogInformation("Cache cleared successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cache");
        }
    }
    
    public async Task<Dictionary<string, object>> GetCacheStatsAsync()
    {
        var stats = new Dictionary<string, object>();
        
        lock (_lockObject)
        {
            stats["TotalEntries"] = _cacheAccessTimes.Count;
            stats["OldestEntry"] = _cacheAccessTimes.Values.Any() ? _cacheAccessTimes.Values.Min() : (DateTime?)null;
            stats["NewestEntry"] = _cacheAccessTimes.Values.Any() ? _cacheAccessTimes.Values.Max() : (DateTime?)null;
            stats["CacheKeys"] = _cacheAccessTimes.Keys.ToList();
        }
        
        return stats;
    }
}