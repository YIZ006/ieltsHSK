using Blazored.LocalStorage;
using System.Net.Http.Json;

namespace Frontend.App.Services;

/// <summary>
/// Tính chuỗi ngày học tập: một ngày được tính "active" khi người dùng
/// đăng nhập/truy cập web hoặc học trong ngày đó. Dữ liệu lưu database PostgreSQL và cache localStorage.
/// </summary>
public class StreakService(ILocalStorageService localStorage, HttpClient? httpClient = null)
{
    private const string StorageKey = "streak_active_days";
    private const int MaxTrackedDays = 400;
    private static bool _migrationAttempted = false;

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
                await httpClient.PostAsJsonAsync("api/user/study-time", new { Seconds = 60, Date = DateTime.UtcNow });
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

    private static List<DateTime>? _cachedDays;
    private static DateTime _lastFetchTime = DateTime.MinValue;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public static void InvalidateCache()
    {
        _cachedDays = null;
        _lastFetchTime = DateTime.MinValue;
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

    private async Task<List<DateTime>> LoadAsync(bool forceRefresh = false)
    {
        if (!forceRefresh && _cachedDays != null && (DateTime.UtcNow - _lastFetchTime) < CacheDuration)
        {
            return _cachedDays;
        }

        List<DateTime> localDays = new();
        try
        {
            var raw = await localStorage.GetItemAsync<List<string>>(StorageKey);
            if (raw != null && raw.Count > 0)
            {
                localDays = raw
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => DateTime.ParseExact(s, "yyyy-MM-dd", null))
                    .Distinct()
                    .OrderBy(d => d)
                    .ToList();
            }
        }
        catch { }

        if (httpClient != null)
        {
            try
            {
                var srv = await httpClient.GetFromJsonAsync<StudyActivityResponseDto>("api/user/study-activity");
                if (srv != null)
                {
                    var srvDays = (srv.ActiveDays ?? new List<string>())
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .Select(s => DateTime.ParseExact(s, "yyyy-MM-dd", null))
                        .Distinct()
                        .OrderBy(d => d)
                        .ToList();

                    // Check if we need to migrate existing local days to database
                    if (!_migrationAttempted && localDays.Count > 0)
                    {
                        var unmerged = localDays.Except(srvDays).ToList();
                        if (unmerged.Count > 0)
                        {
                            _migrationAttempted = true;
                            _ = httpClient.PostAsJsonAsync("api/user/study-activity/migrate", new
                            {
                                ActiveDays = unmerged.Select(d => d.ToString("yyyy-MM-dd")).ToList()
                            });
                        }
                    }

                    // Database is source of truth
                    if (srvDays.Count > 0)
                    {
                        await SaveAsync(srvDays);
                        _cachedDays = srvDays;
                        _lastFetchTime = DateTime.UtcNow;
                        return srvDays;
                    }
                }
            }
            catch
            {
                // Offline fallback
            }
        }

        _cachedDays = localDays;
        _lastFetchTime = DateTime.UtcNow;
        return localDays;
    }

    private async Task SaveAsync(List<DateTime> days)
    {
        _cachedDays = days;
        _lastFetchTime = DateTime.UtcNow;
        try
        {
            await localStorage.SetItemAsync(StorageKey, days.Select(d => d.ToString("yyyy-MM-dd")).ToList());
        }
        catch { }
    }

    private sealed record StudyActivityResponseDto(
        int CurrentStreak,
        int BestStreak,
        int TodaySeconds,
        List<DailyStudyItemDto>? DailyActivities,
        List<string>? ActiveDays);

    private sealed record DailyStudyItemDto(DateTime Date, int Seconds);
}
