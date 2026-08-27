using Blazored.LocalStorage;

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
}

public sealed class ProfileService(ILocalStorageService localStorage)
{
    private const string StorageKey = "user_profile";

    public async Task<UserProfile> GetAsync()
    {
        try
        {
            return await localStorage.GetItemAsync<UserProfile>(StorageKey) ?? new UserProfile();
        }
        catch
        {
            return new UserProfile();
        }
    }

    public async Task SaveAsync(UserProfile profile)
    {
        await localStorage.SetItemAsync(StorageKey, profile);
    }
}
