namespace Backend.Application.Abstractions;

public interface ICacheService
{
    /// <summary>
    /// Lấy dữ liệu từ cache theo key. Trả về null nếu cache miss hoặc có lỗi.
    /// </summary>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lưu dữ liệu vào cache với thời gian hết hạn (TTL).
    /// </summary>
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Xóa một key khỏi cache.
    /// </summary>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Xóa tất cả các keys bắt đầu bằng tiền tố (prefix) chỉ định.
    /// </summary>
    Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default);
}
