using Blazored.LocalStorage;

namespace Frontend.App.Services;

/// <summary>
/// Tính chuỗi ngày học tập: một ngày được tính "active" khi người dùng
/// đăng nhập/truy cập web trong ngày đó. Dữ liệu lưu localStorage và đồng bộ PostgreSQL khi có auth.
/// </summary>
public class StreakService(ILocalStorageService localStorage, HttpClient? httpClient = null)
{
    private const string StorageKey = "streak_active_days";
    private const int MaxTrackedDays = 400;

    /// <summary>Đánh dấu hôm nay là một ngày hoạt động (idempotent).</summary>
    public async Task MarkTodayAsync()
    {
        var days = await LoadAsync();
        var today = DateTime.Today;
        if (!days.Contains(today))
        {
            days.Add(today);
            days.Sort();
            if (days.Count > MaxTrackedDays)
            {
                days = days.Skip(days.Count - MaxTrackedDays).ToList();
            }
            await SaveAsync(days);
        }

        if (httpClient != null)
        {
            try
            {
                await httpClient.PostAsync("api/user/streak", null);
            }
            catch
            {
                // Non-fatal if offline
            }
        }
    }

    public async Task<HashSet<DateTime>> GetActiveDaysAsync()
    {
        return (await LoadAsync()).ToHashSet();
    }

    /// <summary>Chuỗi liên tiếp tính đến hôm nay (hoặc hôm qua nếu hôm nay chưa active).</summary>
    public async Task<int> GetCurrentStreakAsync()
    {
        var days = await LoadAsync();
        return CountCurrentStreak(days);
    }

    /// <summary>Chuỗi dài nhất từng đạt được trong dữ liệu đã lưu.</summary>
    public async Task<int> GetBestStreakAsync()
    {
        var days = await LoadAsync();
        var best = 0;
        var run = 0;
        DateTime? previous = null;

        foreach (var day in days)
        {
            run = previous.HasValue && (day - previous.Value).Days == 1 ? run + 1 : 1;
            if (run > best) best = run;
            previous = day;
        }

        return best;
    }

    private static int CountCurrentStreak(List<DateTime> days)
    {
        var day = DateTime.Today;
        if (!days.Contains(day)) day = day.AddDays(-1);

        var streak = 0;
        while (days.Contains(day))
        {
            streak++;
            day = day.AddDays(-1);
        }

        return streak;
    }

    private async Task<List<DateTime>> LoadAsync()
    {
        try
        {
            var raw = await localStorage.GetItemAsync<List<string>>(StorageKey);
            if (raw == null || raw.Count == 0) return new List<DateTime>();

            return raw
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => DateTime.ParseExact(s, "yyyy-MM-dd", null))
                .Distinct()
                .OrderBy(d => d)
                .ToList();
        }
        catch
        {
            return new List<DateTime>();
        }
    }

    private async Task SaveAsync(List<DateTime> days)
    {
        await localStorage.SetItemAsync(StorageKey, days.Select(d => d.ToString("yyyy-MM-dd")).ToList());
    }
}
