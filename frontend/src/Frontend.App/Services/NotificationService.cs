using System.Net.Http.Json;
using Blazored.LocalStorage;
using Frontend.App.Models;

namespace Frontend.App.Services;

public class NotificationService
{
    private readonly HttpClient _http;
    private readonly ILocalStorageService _localStorage;
    private const string LocalReadStorageKey = "read_notification_ids_v1";

    public event Action? OnChange;
    private List<NotificationDto> _cachedNotifications = new();
    private bool _hasLoaded;

    public IReadOnlyList<NotificationDto> Notifications => _cachedNotifications;
    public int UnreadCount => _cachedNotifications.Count(n => !n.IsRead);

    public NotificationService(HttpClient http, ILocalStorageService localStorage)
    {
        _http = http;
        _localStorage = localStorage;
    }

    public async Task<List<NotificationDto>> GetNotificationsAsync(bool forceRefresh = false)
    {
        if (_hasLoaded && !forceRefresh)
        {
            return _cachedNotifications;
        }

        try
        {
            var notifs = await _http.GetFromJsonAsync<List<NotificationDto>>("api/notifications") ?? new();

            // Đọc thêm danh sách id đã đọc từ localStorage (hỗ trợ cả guest và sync nhanh)
            var localReadIds = await GetLocalReadIdsAsync();
            foreach (var n in notifs)
            {
                if (localReadIds.Contains(n.Id))
                {
                    n.IsRead = true;
                }
            }

            _cachedNotifications = notifs;
            _hasLoaded = true;
            NotifyStateChanged();
            return _cachedNotifications;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NotificationService] Error fetching notifications: {ex.Message}");
            return _cachedNotifications;
        }
    }

    public async Task MarkAsReadAsync(int id)
    {
        var target = _cachedNotifications.FirstOrDefault(n => n.Id == id);
        if (target != null && !target.IsRead)
        {
            target.IsRead = true;
            NotifyStateChanged();
        }

        // Lưu vào localStorage
        var localReadIds = await GetLocalReadIdsAsync();
        if (!localReadIds.Contains(id))
        {
            localReadIds.Add(id);
            await SaveLocalReadIdsAsync(localReadIds);
        }

        // Gửi lên server
        try
        {
            await _http.PostAsync($"api/notifications/{id}/read", null);
        }
        catch { }
    }

    public async Task MarkAllAsReadAsync()
    {
        var unreadItems = _cachedNotifications.Where(n => !n.IsRead).ToList();
        if (unreadItems.Count == 0) return;

        foreach (var n in unreadItems)
        {
            n.IsRead = true;
        }
        NotifyStateChanged();

        var localReadIds = await GetLocalReadIdsAsync();
        foreach (var n in _cachedNotifications)
        {
            if (!localReadIds.Contains(n.Id))
            {
                localReadIds.Add(n.Id);
            }
        }
        await SaveLocalReadIdsAsync(localReadIds);

        try
        {
            await _http.PostAsync("api/notifications/read-all", null);
        }
        catch { }
    }

    private async Task<HashSet<int>> GetLocalReadIdsAsync()
    {
        try
        {
            var list = await _localStorage.GetItemAsync<List<int>>(LocalReadStorageKey);
            return list != null ? list.ToHashSet() : new HashSet<int>();
        }
        catch
        {
            return new HashSet<int>();
        }
    }

    private async Task SaveLocalReadIdsAsync(HashSet<int> ids)
    {
        try
        {
            await _localStorage.SetItemAsync(LocalReadStorageKey, ids.ToList());
        }
        catch { }
    }

    // ── ADMIN METHODS ──
    public async Task<List<AdminNotificationDto>> GetAdminNotificationsAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<List<AdminNotificationDto>>("api/admin/notifications") ?? new();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NotificationService] Admin fetch error: {ex.Message}");
            return new List<AdminNotificationDto>();
        }
    }

    public async Task<bool> CreateAsync(CreateNotificationRequest request)
    {
        try
        {
            var res = await _http.PostAsJsonAsync("api/admin/notifications", request);
            if (res.IsSuccessStatusCode)
            {
                await GetNotificationsAsync(forceRefresh: true);
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NotificationService] Create error: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> UpdateAsync(int id, UpdateNotificationRequest request)
    {
        try
        {
            var res = await _http.PutAsJsonAsync($"api/admin/notifications/{id}", request);
            if (res.IsSuccessStatusCode)
            {
                await GetNotificationsAsync(forceRefresh: true);
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NotificationService] Update error: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        try
        {
            var res = await _http.DeleteAsync($"api/admin/notifications/{id}");
            if (res.IsSuccessStatusCode)
            {
                _cachedNotifications.RemoveAll(n => n.Id == id);
                NotifyStateChanged();
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NotificationService] Delete error: {ex.Message}");
            return false;
        }
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
