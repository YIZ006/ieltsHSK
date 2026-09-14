using System.Net.Http.Json;
using System.Text.Json;
using Blazored.LocalStorage;

namespace Frontend.App.Services;

/// <summary>
/// Hệ thống token ghi nhớ tài khoản:
/// - Access token (JWT) + refresh token được lưu trong Cookie, phiên sống dai qua nhiều lần đóng/mở trình duyệt.
/// - Khi access token hết hạn, tự động gọi /api/auth/refresh để gia hạn ngầm (silent refresh) mà không cần đăng nhập lại.
/// - Các request song song chia sẻ chung một lần refresh (semaphore) để tránh xoay vòng token loạn.
/// </summary>
public class TokenRefreshService(HttpClient httpClient, CookieStorageService cookieStorage)
{
    public const string AccessTokenKey = "authToken";
    public const string RefreshTokenKey = "authRefreshToken";

    // Làm mới sớm hơn 30 giây so với hạn thật để tránh trường hợp token chết giữa đường
    private static readonly TimeSpan ExpirySkew = TimeSpan.FromSeconds(30);

    // Sau một lần refresh thành công, bỏ qua các yêu cầu refresh lặp lại trong khoảng này
    private static readonly TimeSpan RefreshDedupeWindow = TimeSpan.FromSeconds(5);

    private static readonly SemaphoreSlim RefreshLock = new(1, 1);
    private long _lastRefreshSuccessTicks;

    /// <summary>Được bắn ra sau khi cặp token được gia hạn thành công (tham số: access token mới).</summary>
    public event Action<string>? TokensRefreshed;

    /// <summary>Được bắn ra khi phiên đã hết hạn hẳn (refresh token hết hạn/bị thu hồi) và không thể phục hồi.</summary>
    public event Action? SessionExpired;

    /// <summary>
    /// Trả về access token còn hiệu lực; tự động refresh nếu token đã hết hạn.
    /// null = chưa đăng nhập hoặc không thể phục hồi phiên.
    /// </summary>
    public async Task<string?> GetAccessTokenAsync()
    {
        var token = await cookieStorage.GetItemAsync(AccessTokenKey);
        if (string.IsNullOrWhiteSpace(token)) return null;

        if (IsTokenExpired(token))
        {
            if (!await RefreshAsync()) return null;
            token = await cookieStorage.GetItemAsync(AccessTokenKey);
        }

        return string.IsNullOrWhiteSpace(token) ? null : token;
    }

    /// <summary>
    /// Gia hạn cặp token. force = true bỏ qua kiểm tra "token còn hạn" (dùng khi vừa nhận 401).
    /// Trả về false khi không thể refresh (chưa đăng nhập, phiên chết, hoặc lỗi mạng).
    /// </summary>
    public async Task<bool> RefreshAsync(bool force = false)
    {
        await RefreshLock.WaitAsync();
        try
        {
            // Request khác vừa refresh xong (ví dụ khi 401 dồn cục) -> dùng kết quả đó luôn
            if (DateTimeOffset.UtcNow.UtcTicks - _lastRefreshSuccessTicks < RefreshDedupeWindow.Ticks)
                return true;

            var current = await cookieStorage.GetItemAsync(AccessTokenKey);
            if (!force && !string.IsNullOrWhiteSpace(current) && !IsTokenExpired(current))
                return true;

            var refreshToken = await cookieStorage.GetItemAsync(RefreshTokenKey);
            if (string.IsNullOrWhiteSpace(refreshToken)) return false;

            try
            {
                var response = await httpClient.PostAsJsonAsync("api/auth/refresh", new { RefreshToken = refreshToken });
                if (!response.IsSuccessStatusCode)
                {
                    // Server từ chối rõ ràng (hết hạn/bị thu hồi) -> phiên đã chết thật sự
                    await ClearSessionAsync();
                    SessionExpired?.Invoke();
                    return false;
                }

                var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
                if (result == null || string.IsNullOrWhiteSpace(result.Token) || string.IsNullOrWhiteSpace(result.RefreshToken))
                {
                    await ClearSessionAsync();
                    SessionExpired?.Invoke();
                    return false;
                }

                await cookieStorage.SetItemAsync(AccessTokenKey, result.Token, days: 1);
                await cookieStorage.SetItemAsync(RefreshTokenKey, result.RefreshToken, days: 30);
                _lastRefreshSuccessTicks = DateTimeOffset.UtcNow.UtcTicks;
                TokensRefreshed?.Invoke(result.Token);
                return true;
            }
            catch
            {
                // Lỗi mạng/máy chủ tạm thời — giữ nguyên phiên, người dùng thử lại khi có mạng
                return false;
            }
        }
        finally
        {
            RefreshLock.Release();
        }
    }

    /// <summary>Thông báo cho server thu hồi refresh token (gọi khi đăng xuất). Best-effort, không ném lỗi.</summary>
    public async Task RevokeOnServerAsync()
    {
        try
        {
            var refreshToken = await cookieStorage.GetItemAsync(RefreshTokenKey);
            if (!string.IsNullOrWhiteSpace(refreshToken))
            {
                await httpClient.PostAsJsonAsync("api/auth/logout", new { RefreshToken = refreshToken });
            }
        }
        catch
        {
            // Đăng xuất local vẫn phải tiếp tục dù server không phản hồi
        }
    }

    private async Task ClearSessionAsync()
    {
        await cookieStorage.RemoveItemAsync(AccessTokenKey);
        await cookieStorage.RemoveItemAsync(RefreshTokenKey);
    }

    public static bool IsTokenExpired(string jwt)
    {
        var exp = GetExpirationUtc(jwt);
        return exp.HasValue && exp.Value <= DateTime.UtcNow.Add(ExpirySkew);
    }

    public static DateTime? GetExpirationUtc(string jwt)
    {
        if (string.IsNullOrWhiteSpace(jwt)) return null;

        try
        {
            var parts = jwt.Split('.');
            if (parts.Length < 2) return null;

            var jsonBytes = ParseBase64WithoutPadding(parts[1]);
            var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonBytes);
            if (payload != null && payload.TryGetValue("exp", out var exp) &&
                exp.ValueKind == JsonValueKind.Number && exp.TryGetInt64(out var seconds))
            {
                return DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime;
            }
        }
        catch
        {
            // Token sai định dạng -> coi như không có hạn (bộ xử lý khác sẽ tự từ chối)
        }
        return null;
    }

    private static byte[] ParseBase64WithoutPadding(string base64)
    {
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        return Convert.FromBase64String(base64);
    }
}
