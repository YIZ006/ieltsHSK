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
    public DateTime? TargetDeadline { get; set; }
    public string? IeltsLevel { get; set; }
    public string? HskLevel { get; set; }
    public string StudyLevel { get; set; } = "Intermediate";
    public string LastName { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public int Streak { get; set; }
    public DateTime? UsernameChangedAt { get; set; }

    public bool CanChangeUsername => !UsernameChangedAt.HasValue || (DateTime.UtcNow - UsernameChangedAt.Value).TotalDays >= 30;
    public int UsernameDaysRemaining => !CanChangeUsername && UsernameChangedAt.HasValue ? Math.Max(1, (int)Math.Ceiling(30 - (DateTime.UtcNow - UsernameChangedAt.Value).TotalDays)) : 0;
    public DateTime? UsernameNextChangeAllowedAt => UsernameChangedAt?.AddDays(30);
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

        var token = await localStorage.GetItemAsync<string>("authToken");
        if (httpClient != null && !string.IsNullOrWhiteSpace(token))
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
                    if (!string.IsNullOrWhiteSpace(srvUser.Username)) local.DisplayName = srvUser.Username;
                    local.UsernameChangedAt = srvUser.UsernameChangedAt;
                    if (!string.IsNullOrWhiteSpace(srvUser.Avatar)) local.AvatarEmoji = srvUser.Avatar;
                    if (!string.IsNullOrWhiteSpace(srvUser.AvatarColor)) local.AvatarColor = srvUser.AvatarColor;
                    if (!string.IsNullOrWhiteSpace(srvUser.Bio)) local.Bio = srvUser.Bio;
                    if (!string.IsNullOrWhiteSpace(srvUser.TargetExam)) local.TargetExam = srvUser.TargetExam;
                    if (!string.IsNullOrWhiteSpace(srvUser.TargetScore)) local.TargetScore = srvUser.TargetScore;
                    if (srvUser.TargetDeadline.HasValue) local.TargetDeadline = srvUser.TargetDeadline;
                    if (!string.IsNullOrWhiteSpace(srvUser.IeltsLevel)) local.IeltsLevel = srvUser.IeltsLevel;
                    if (!string.IsNullOrWhiteSpace(srvUser.HskLevel)) local.HskLevel = srvUser.HskLevel;
                    if (!string.IsNullOrWhiteSpace(srvUser.Level)) local.StudyLevel = srvUser.Level;
                    local.Streak = srvUser.Streak;

                    // Migrate local customizations to server if server doesn't have them yet
                    bool hasLocalDataToMigrate = (string.IsNullOrWhiteSpace(srvUser.Bio) && !string.IsNullOrWhiteSpace(local.Bio))
                        || (string.IsNullOrWhiteSpace(srvUser.TargetExam) && !string.IsNullOrWhiteSpace(local.TargetExam))
                        || (string.IsNullOrWhiteSpace(srvUser.TargetScore) && !string.IsNullOrWhiteSpace(local.TargetScore));

                    if (hasLocalDataToMigrate)
                    {
                        _ = SaveAsync(local);
                    }

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
                    AvatarColor = profile.AvatarColor,
                    Bio = profile.Bio,
                    TargetExam = profile.TargetExam,
                    TargetScore = profile.TargetScore,
                    TargetDeadline = profile.TargetDeadline,
                    IeltsLevel = profile.IeltsLevel,
                    HskLevel = profile.HskLevel,
                    Level = profile.StudyLevel
                });

                if (!response.IsSuccessStatusCode)
                {
                    var errorObj = await response.Content.ReadFromJsonAsync<BackendErrorDto>();
                    return (false, errorObj?.Message ?? "Không thể lưu thông tin vào máy chủ.");
                }

                try
                {
                    var updated = await response.Content.ReadFromJsonAsync<BackendUserDto>();
                    if (updated != null)
                    {
                        profile.UsernameChangedAt = updated.UsernameChangedAt;
                        if (!string.IsNullOrWhiteSpace(updated.Username))
                        {
                            profile.DisplayName = updated.Username;
                        }
                    }
                }
                catch { }
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
        string? AvatarColor,
        string? Bio,
        string? TargetExam,
        string? TargetScore,
        DateTime? TargetDeadline,
        string? IeltsLevel,
        string? HskLevel,
        string? Level,
        int Streak,
        DateTime? LastActive,
        DateTime? UsernameChangedAt = null);
}
