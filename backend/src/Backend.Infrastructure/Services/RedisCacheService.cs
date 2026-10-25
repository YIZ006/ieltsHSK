using System.Collections.Concurrent;
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
    private readonly int _maxItemSizeBytes;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ConcurrentDictionary<string, byte> _memoryCacheKeys = new();

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
        
        // Ngưỡng dung lượng tối đa cho 1 key gửi lên Redis (mặc định: 512 KB).
        // Giới hạn Upstash Free là 10MB/request. Chặn tại 512KB để bảo vệ network bandwidth và không bao giờ vi phạm quota.
        _maxItemSizeBytes = configuration.GetValue<int?>("Redis:MaxItemSizeBytes") ?? (512 * 1024);

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var fullKey = _prefix + key;

        // 1. Kiểm tra L1 MemoryCache trước để tiết kiệm quota Upstash Redis Free Tier (10.000 req/ngày)
        if (_memoryCache.TryGetValue(fullKey, out T? memoryValue))
        {
            return memoryValue;
        }

        // 2. Thử lấy từ Redis nếu có kết nối
        if (_redisEnabled && _redis != null && _redis.IsConnected)
        {
            try
            {
                var db = _redis.GetDatabase();
                var redisValue = await db.StringGetAsync(fullKey);

                if (redisValue.HasValue)
                {
                    var result = JsonSerializer.Deserialize<T>(redisValue.ToString(), _jsonOptions);
                    if (result != null)
                    {
                        // Lưu vào L1 MemoryCache với sliding expiration ngắn (2 phút) và absolute expiration (10 phút) để chặn các query lặp lại và ngăn stale vĩnh viễn
                        var entryOptions = new MemoryCacheEntryOptions
                        {
                            SlidingExpiration = TimeSpan.FromMinutes(2),
                            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                        };
                        entryOptions.RegisterPostEvictionCallback((evictedKey, _, _, _) =>
                        {
                            if (evictedKey is string sk)
                            {
                                _memoryCacheKeys.TryRemove(sk, out _);
                            }
                        });
                        _memoryCache.Set(fullKey, result, entryOptions);
                        _memoryCacheKeys.TryAdd(fullKey, 0);
                    }
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis GET failed for key {Key}", fullKey);
            }
        }

        return default;
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        if (value == null) return;

        var fullKey = _prefix + key;
        var ttl = expiry ?? _defaultTtl;

        // 1. Lưu vào Redis nếu payload nằm trong ngưỡng an toàn (< 512KB)
        if (_redisEnabled && _redis != null && _redis.IsConnected)
        {
            try
            {
                var json = JsonSerializer.Serialize(value, _jsonOptions);
                var byteCount = System.Text.Encoding.UTF8.GetByteCount(json);

                if (byteCount > _maxItemSizeBytes)
                {
                    _logger.LogWarning(
                        "[Redis Protection] Key {Key} payload size ({SizeKB:N1} KB) exceeds safe Redis threshold ({LimitKB:N1} KB). Skipping Redis to prevent Upstash 10MB limit violation. Storing in local MemoryCache.",
                        fullKey, byteCount / 1024.0, _maxItemSizeBytes / 1024.0);
                }
                else
                {
                    var db = _redis.GetDatabase();
                    await db.StringSetAsync(fullKey, json, ttl);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis SET failed for key {Key}, storing in MemoryCache", fullKey);
            }
        }

        // 2. Lưu đồng thời vào MemoryCache để dự phòng (RAM cục bộ không bị trần 10MB của Upstash)
        var memOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl
        };
        memOptions.RegisterPostEvictionCallback((evictedKey, _, _, _) =>
        {
            if (evictedKey is string sk)
            {
                _memoryCacheKeys.TryRemove(sk, out _);
            }
        });
        _memoryCache.Set(fullKey, value, memOptions);
        _memoryCacheKeys.TryAdd(fullKey, 0);
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
        _memoryCacheKeys.TryRemove(fullKey, out _);
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        var fullPrefix = _prefix + prefix;
        var searchPattern = fullPrefix + "*";

        // 1. Quét và xóa các keys trong L1 MemoryCache matching prefix để tránh lệch dữ liệu (cache desynchronization)
        var memoryKeysToRemove = _memoryCacheKeys.Keys
            .Where(k => k.StartsWith(fullPrefix, StringComparison.Ordinal) || k.StartsWith(prefix, StringComparison.Ordinal))
            .ToList();

        foreach (var k in memoryKeysToRemove)
        {
            _memoryCache.Remove(k);
            _memoryCacheKeys.TryRemove(k, out _);
        }

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
                            // Xóa theo batch (chunking) 100 keys/lần để tránh gửi payload mảng quá lớn
                            const int batchSize = 100;
                            for (int i = 0; i < keys.Length; i += batchSize)
                            {
                                var chunk = keys.Skip(i).Take(batchSize).ToArray();
                                await db.KeyDeleteAsync(chunk);
                            }
                            _logger.LogInformation("Deleted {Count} Redis keys with pattern {Pattern} in chunks", keys.Length, searchPattern);
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
