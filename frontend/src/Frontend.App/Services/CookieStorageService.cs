using Blazored.LocalStorage;
using Microsoft.JSInterop;

namespace Frontend.App.Services;

/// <summary>
/// Quản lý lưu trữ phiên làm việc qua Cookie trên trình duyệt (document.cookie).
/// Tự động migrate dữ liệu từ localStorage sang Cookie nếu phát hiện phiên cũ để tránh bị đăng xuất đột ngột.
/// </summary>
public class CookieStorageService(IJSRuntime jsRuntime, ILocalStorageService localStorage)
{
    public async Task<string?> GetItemAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;

        try
        {
            // 1. Thử lấy từ Cookie
            var cookieVal = await jsRuntime.InvokeAsync<string?>("cookieStorage.getItem", key);
            if (!string.IsNullOrWhiteSpace(cookieVal))
            {
                return cookieVal;
            }

            // 2. Migration fallback: Nếu chưa có trong Cookie nhưng còn lưu trong localStorage cũ
            var localVal = await localStorage.GetItemAsync<string>(key);
            if (!string.IsNullOrWhiteSpace(localVal))
            {
                // Tự động lưu vào Cookie (mặc định 30 ngày cho phiên)
                await SetItemAsync(key, localVal, days: 30);
                // Dọn dẹp key khỏi localStorage sau khi đã di dời sang cookie
                await localStorage.RemoveItemAsync(key);
                return localVal;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CookieStorageService] GetItemAsync '{key}' error: {ex.Message}");
        }

        return null;
    }

    public async Task SetItemAsync(string key, string value, int? days = null)
    {
        if (string.IsNullOrWhiteSpace(key)) return;

        try
        {
            await jsRuntime.InvokeVoidAsync("cookieStorage.setItem", key, value, days);
            // Đồng thời dọn dẹp khỏi localStorage để phiên hoàn toàn nằm trong cookie
            try { await localStorage.RemoveItemAsync(key); } catch { }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CookieStorageService] SetItemAsync '{key}' error: {ex.Message}");
        }
    }

    public async Task RemoveItemAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return;

        try
        {
            await jsRuntime.InvokeVoidAsync("cookieStorage.removeItem", key);
            try { await localStorage.RemoveItemAsync(key); } catch { }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CookieStorageService] RemoveItemAsync '{key}' error: {ex.Message}");
        }
    }
}
