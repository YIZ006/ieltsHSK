using Blazored.LocalStorage;
using System.Net.Http.Json;

namespace Frontend.App.Services;

public sealed record GameProgressState(
    string GameType,
    string Level,
    int CurrentStage,
    int MaxUnlockedStage,
    int HighScore,
    DateTime UpdatedAt
);

public sealed class UserGameProgressService
{
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorage;
    private static bool _migrated = false;

    public UserGameProgressService(HttpClient httpClient, ILocalStorageService localStorage)
    {
        _httpClient = httpClient;
        _localStorage = localStorage;
    }

    public async Task<(int CurrentStage, int MaxUnlockedStage, int HighScore)> LoadProgressAsync(string gameType, string level)
    {
        int localCurrent = 0;
        int localMax = 0;
        int localScore = 0;

        try
        {
            var curStr = await _localStorage.GetItemAsync<string>($"{gameType}_progress_{level}");
            var maxStr = await _localStorage.GetItemAsync<string>($"{gameType}_max_unlocked_{level}");
            var scoreStr = await _localStorage.GetItemAsync<string>($"{gameType}_score_{level}");
            int.TryParse(curStr, out localCurrent);
            int.TryParse(maxStr, out localMax);
            int.TryParse(scoreStr, out localScore);
        }
        catch { }

        try
        {
            var token = await _localStorage.GetItemAsync<string>("authToken");
            if (!string.IsNullOrWhiteSpace(token))
            {
                var serverItems = await _httpClient.GetFromJsonAsync<List<GameProgressResponseDto>>($"api/user/game-progress?gameType={Uri.EscapeDataString(gameType)}");
                var item = serverItems?.FirstOrDefault(i => string.Equals(i.Level, level, StringComparison.OrdinalIgnoreCase));
                if (item != null)
                {
                    var cur = gameType == "wordle" ? localCurrent : Math.Max(item.CurrentStage, localCurrent);
                    var max = Math.Max(item.MaxUnlockedStage, localMax);
                    var score = Math.Max(item.HighScore, localScore);

                    await SaveLocalAsync(gameType, level, cur, max, score);
                    return (cur, max, score);
                }
                else if (!_migrated && (localCurrent > 0 || localMax > 0 || localScore > 0))
                {
                    _migrated = true;
                    _ = SaveProgressAsync(gameType, level, localCurrent, localMax, localScore);
                }
            }
        }
        catch
        {
            // Offline fallback
        }

        return (localCurrent, localMax, localScore);
    }

    public async Task SaveProgressAsync(string gameType, string level, int currentStage, int maxUnlockedStage, int? score = null)
    {
        await SaveLocalAsync(gameType, level, currentStage, maxUnlockedStage, score);

        try
        {
            var token = await _localStorage.GetItemAsync<string>("authToken");
            if (string.IsNullOrWhiteSpace(token)) return;

            await _httpClient.PostAsJsonAsync("api/user/game-progress", new
            {
                GameType = gameType,
                Level = level,
                CurrentStage = currentStage,
                MaxUnlockedStage = maxUnlockedStage,
                Score = score
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[UserGameProgressService] Save note: {ex.Message}");
        }
    }

    private async Task SaveLocalAsync(string gameType, string level, int currentStage, int maxUnlockedStage, int? score = null)
    {
        try
        {
            await _localStorage.SetItemAsync($"{gameType}_progress_{level}", currentStage.ToString());
            await _localStorage.SetItemAsync($"{gameType}_max_unlocked_{level}", maxUnlockedStage.ToString());
            if (score.HasValue)
            {
                await _localStorage.SetItemAsync($"{gameType}_score_{level}", score.Value.ToString());
            }
        }
        catch { }
    }

    private sealed record GameProgressResponseDto(
        string GameType,
        string Level,
        int CurrentStage,
        int MaxUnlockedStage,
        int HighScore,
        DateTime UpdatedAt);
}
