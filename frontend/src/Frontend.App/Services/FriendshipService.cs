using System.Net.Http.Json;
using Frontend.App.Models;

namespace Frontend.App.Services;

public class FriendshipService(HttpClient httpClient)
{
    public bool IsDrawerOpen { get; private set; } = false;
    public int PendingIncomingCount { get; private set; } = 0;

    public event Action? OnChange;
    public event Action? OnDrawerStateChanged;

    public void OpenDrawer()
    {
        if (!IsDrawerOpen)
        {
            IsDrawerOpen = true;
            OnDrawerStateChanged?.Invoke();
        }
    }

    public void CloseDrawer()
    {
        if (IsDrawerOpen)
        {
            IsDrawerOpen = false;
            OnDrawerStateChanged?.Invoke();
        }
    }

    public void ToggleDrawer()
    {
        IsDrawerOpen = !IsDrawerOpen;
        OnDrawerStateChanged?.Invoke();
    }

    public async Task<FriendsSummaryDto?> GetSummaryAsync()
    {
        try
        {
            var summary = await httpClient.GetFromJsonAsync<FriendsSummaryDto>("api/friends/summary");
            if (summary != null)
            {
                PendingIncomingCount = summary.PendingIncomingCount;
                OnChange?.Invoke();
            }
            return summary;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FriendshipService] GetSummaryAsync error: {ex.Message}");
            return null;
        }
    }

    public async Task<List<FriendDto>> GetFriendsAsync()
    {
        try
        {
            return await httpClient.GetFromJsonAsync<List<FriendDto>>("api/friends") ?? new List<FriendDto>();
        }
        catch
        {
            return new List<FriendDto>();
        }
    }

    public async Task<List<UserSearchResultDto>> SearchUsersAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return new List<UserSearchResultDto>();

        try
        {
            var encoded = Uri.EscapeDataString(query.Trim());
            var results = await httpClient.GetFromJsonAsync<List<UserSearchResultDto>>($"api/friends/search?q={encoded}") 
                   ?? new List<UserSearchResultDto>();
            Console.WriteLine($"[FriendshipService] Search '{query}' returned {results.Count} user(s)");
            return results;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FriendshipService] Search error for '{query}': {ex.Message}");
            return new List<UserSearchResultDto>();
        }
    }

    public async Task<bool> SendFriendRequestAsync(int targetUserId)
    {
        try
        {
            var res = await httpClient.PostAsync($"api/friends/request/{targetUserId}", null);
            return res.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> AcceptFriendRequestAsync(int friendshipId)
    {
        try
        {
            var res = await httpClient.PostAsync($"api/friends/accept/{friendshipId}", null);
            if (res.IsSuccessStatusCode)
            {
                if (PendingIncomingCount > 0) PendingIncomingCount--;
                OnChange?.Invoke();
                return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DeclineFriendRequestAsync(int friendshipId)
    {
        try
        {
            var res = await httpClient.PostAsync($"api/friends/decline/{friendshipId}", null);
            if (res.IsSuccessStatusCode)
            {
                if (PendingIncomingCount > 0) PendingIncomingCount--;
                OnChange?.Invoke();
                return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> CancelFriendRequestAsync(int friendshipId)
    {
        try
        {
            var res = await httpClient.DeleteAsync($"api/friends/cancel/{friendshipId}");
            return res.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> RemoveFriendAsync(int friendId)
    {
        try
        {
            var res = await httpClient.DeleteAsync($"api/friends/{friendId}");
            return res.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> ToggleFavoriteAsync(int friendId)
    {
        try
        {
            var res = await httpClient.PostAsync($"api/friends/favorite/{friendId}", null);
            return res.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task HeartbeatAsync()
    {
        try
        {
            await httpClient.PostAsync("api/friends/heartbeat", null);
        }
        catch { }
    }
}
