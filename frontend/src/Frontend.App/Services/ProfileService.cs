using Blazored.LocalStorage;
using System.Net.Http.Json;

namespace Frontend.App.Services;

public sealed class UserProfile
{
    public int? Id { get; set; }
    public string AvatarEmoji { get; set; } = "🎓";
    public string AvatarColor { get; set; } = "#6c5ce7";
    public string DisplayName { get; set; } = "";
    public string Bio { get; set; } = "";
    public string TargetExam { get; set; } = "TOEIC";
    public string TargetScore { get; set; } = "800";
    public string StudyLevel { get; set; } = "Intermediate";
    public string LastName { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public int Xp { get; set; }
    public int Streak { get; set; }
}

public sealed class ProfileService(ILocalStorageService localStorage, HttpClient? httpClient = null)
{
    private const string StorageKey = "user_profile";
    private static UserProfile? _inMemoryProfile;

    public static UserProfile? CachedProfile => _inMemoryProfile;

    public async Task<UserProfile> GetAsync(bool forceRefresh = false)
    {
        if (!forceRefresh && _inMemoryProfile != null)
        {
            return _inMemoryProfile;
        }

        UserProfile local;
        try
        {
            local = await localStorage.GetItemAsync<UserProfile>(StorageKey) ?? new UserProfile();
        }
        catch
        {
            local = new UserProfile();
        }
        _inMemoryProfile = local;

        if (httpClient != null)
        {
            try
            {
                var srvUser = await httpClient.GetFromJsonAsync<BackendUserDto>("api/user/me");
                if (srvUser != null)
                {
                    if (srvUser.Id > 0) local.Id = srvUser.Id;
                    if (!string.IsNullOrWhiteSpace(srvUser.FullName))
                    {
                        local.FullName = srvUser.FullName;
                    }
                    if (!string.IsNullOrWhiteSpace(srvUser.Email)) local.Email = srvUser.Email;
                    if (!string.IsNullOrWhiteSpace(srvUser.Avatar)) local.AvatarEmoji = srvUser.Avatar;
                    if (!string.IsNullOrWhiteSpace(srvUser.Level)) local.StudyLevel = srvUser.Level;
                    local.Xp = srvUser.Xp;
                    local.Streak = srvUser.Streak;
                    _inMemoryProfile = local;
                    await localStorage.SetItemAsync(StorageKey, local);
                }
            }
            catch
            {
                // Non-authenticated or offline fallback
            }
        }
        return local;
    }

    public async Task<(bool Success, string? ErrorMessage)> SaveAsync(UserProfile profile)
    {
        if (httpClient != null)
        {
            try
            {
                var response = await httpClient.PutAsJsonAsync("api/user/profile", new
                {
                    FullName = profile.FullName,
                    Username = string.IsNullOrWhiteSpace(profile.DisplayName) ? null : profile.DisplayName.Trim(),
                    Avatar = profile.AvatarEmoji,
                    Level = profile.StudyLevel
                });

                if (!response.IsSuccessStatusCode)
                {
                    var errorObj = await response.Content.ReadFromJsonAsync<BackendErrorDto>();
                    return (false, errorObj?.Message ?? "Không thể lưu thông tin vào máy chủ.");
                }
            }
            catch
            {
                // Non-fatal if offline
            }
        }

        _inMemoryProfile = profile;
        await localStorage.SetItemAsync(StorageKey, profile);
        return (true, null);
    }

    public static void InvalidateCache()
    {
        _inMemoryProfile = null;
    }

    private sealed record BackendErrorDto(string? Message);

    private sealed record BackendUserDto(
        int Id,
        string Username,
        string? FullName,
        string Email,
        string? Role,
        string? Avatar,
        string? Level,
        int Xp,
        int Streak,
        DateTime? LastActive);
}
