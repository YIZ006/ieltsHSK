using System.Text.Json;
using Backend.Application.Abstractions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Backend.Infrastructure.Services;

public class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer? _redis;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly string _prefix;
    private readonly TimeSpan _defaultTtl;
    private readonly bool _redisEnabled;
    private readonly JsonSerializerOptions _jsonOptions;

    public RedisCacheService(
        IConnectionMultiplexer? redis,
        IMemoryCache memoryCache,
        IConfiguration configuration,
        ILogger<RedisCacheService> logger)
    {
        _redis = redis;
        _memoryCache = memoryCache;
        _logger = logger;
        
        _prefix = configuration["Redis:InstancePrefix"] ?? "ieltsHSK:";
        var ttlMinutes = configuration.GetValue<int?>("Redis:DefaultTtlMinutes") ?? 60;
        _defaultTtl = TimeSpan.FromMinutes(ttlMinutes);
        _redisEnabled = configuration.GetValue<bool?>("Redis:Enabled") ?? true;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var fullKey = _prefix + key;

        // 1. Thử lấy từ Redis nếu có kết nối
        if (_redisEnabled && _redis != null && _redis.IsConnected)
        {
            try
            {
                var db = _redis.GetDatabase();
                var redisValue = await db.StringGetAsync(fullKey);

                if (redisValue.HasValue)
                {
                    var result = JsonSerializer.Deserialize<T>(redisValue.ToString(), _jsonOptions);
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis GET failed for key {Key}, falling back to MemoryCache", fullKey);
            }
        }

        // 2. Fallback sang IMemoryCache
        if (_memoryCache.TryGetValue(fullKey, out T? memoryValue))
        {
            return memoryValue;
        }

        return default;
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        if (value == null) return;

        var fullKey = _prefix + key;
        var ttl = expiry ?? _defaultTtl;

        // 1. Lưu vào Redis
        if (_redisEnabled && _redis != null && _redis.IsConnected)
        {
            try
            {
                var db = _redis.GetDatabase();
                var json = JsonSerializer.Serialize(value, _jsonOptions);
                await db.StringSetAsync(fullKey, json, ttl);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis SET failed for key {Key}, storing in MemoryCache", fullKey);
            }
        }

        // 2. Lưu đồng thời vào MemoryCache để dự phòng
        _memoryCache.Set(fullKey, value, ttl);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        var fullKey = _prefix + key;

        if (_redisEnabled && _redis != null && _redis.IsConnected)
        {
            try
            {
                var db = _redis.GetDatabase();
                await db.KeyDeleteAsync(fullKey);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis REMOVE failed for key {Key}", fullKey);
            }
        }

        _memoryCache.Remove(fullKey);
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        var searchPattern = _prefix + prefix + "*";

        if (_redisEnabled && _redis != null && _redis.IsConnected)
        {
            try
            {
                foreach (var endpoint in _redis.GetEndPoints())
                {
                    var server = _redis.GetServer(endpoint);
                    if (server.IsConnected)
                    {
                        var keys = server.Keys(pattern: searchPattern).ToArray();
                        if (keys.Length > 0)
                        {
                            var db = _redis.GetDatabase();
                            await db.KeyDeleteAsync(keys);
                            _logger.LogInformation("Deleted {Count} Redis keys with pattern {Pattern}", keys.Length, searchPattern);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis RemoveByPrefix failed for pattern {Pattern}", searchPattern);
            }
        }
    }
}
