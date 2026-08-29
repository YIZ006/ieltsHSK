using Blazored.LocalStorage;
using System.Net.Http.Json;

namespace Frontend.App.Services;

public sealed class UserProfile
{
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

    public async Task<UserProfile> GetAsync()
    {
        UserProfile local;
        try
        {
            local = await localStorage.GetItemAsync<UserProfile>(StorageKey) ?? new UserProfile();
        }
        catch
        {
            local = new UserProfile();
        }

        if (httpClient != null)
        {
            try
            {
                var srvUser = await httpClient.GetFromJsonAsync<BackendUserDto>("api/user/me");
                if (srvUser != null)
                {
                    if (!string.IsNullOrWhiteSpace(srvUser.FullName))
                    {
                        local.FullName = srvUser.FullName;
                        local.DisplayName = srvUser.FullName;
                    }
                    else if (!string.IsNullOrWhiteSpace(srvUser.Username))
                    {
                        local.DisplayName = srvUser.Username;
                    }
                    if (!string.IsNullOrWhiteSpace(srvUser.Email)) local.Email = srvUser.Email;
                    if (!string.IsNullOrWhiteSpace(srvUser.Avatar)) local.AvatarEmoji = srvUser.Avatar;
                    if (!string.IsNullOrWhiteSpace(srvUser.Level)) local.StudyLevel = srvUser.Level;
                    local.Xp = srvUser.Xp;
                    local.Streak = srvUser.Streak;
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

    public async Task SaveAsync(UserProfile profile)
    {
        await localStorage.SetItemAsync(StorageKey, profile);
        if (httpClient != null)
        {
            try
            {
                await httpClient.PutAsJsonAsync("api/user/profile", new
                {
                    FullName = profile.FullName,
                    Avatar = profile.AvatarEmoji,
                    Level = profile.StudyLevel
                });
            }
            catch
            {
                // Non-fatal if offline
            }
        }
    }

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
