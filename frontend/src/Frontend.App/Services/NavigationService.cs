using System.Net.Http.Json;
using Frontend.App.Models;

namespace Frontend.App.Services;

public sealed class NavigationService(HttpClient http)
{
    private readonly Dictionary<string, (DateTime CachedAt, List<LearningSectionDto> Items)> _cache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    public async Task<List<LearningSectionDto>> GetAsync(string? language = null, bool forceRefresh = false)
    {
        var key = language ?? "ALL";

        if (!forceRefresh && _cache.TryGetValue(key, out var cached) && (DateTime.UtcNow - cached.CachedAt) < CacheDuration)
        {
            return [.. cached.Items];
        }

        var url = string.IsNullOrWhiteSpace(language)
            ? "api/navigation"
            : $"api/navigation?language={Uri.EscapeDataString(language)}";

        try
        {
            var data = await http.GetFromJsonAsync<List<LearningSectionDto>>(url);
            var result = data ?? new();
            _cache[key] = (DateTime.UtcNow, result);
            return [.. result];
        }
        catch 
        { 
            if (_cache.TryGetValue(key, out var fallback))
            {
                return [.. fallback.Items];
            }
            return new(); 
        }
    }

    public void InvalidateCache()
    {
        _cache.Clear();
    }

    public async Task<List<LearningSectionDto>> GetAllForAdminAsync()
    {
        try
        {
            var data = await http.GetFromJsonAsync<List<LearningSectionDto>>("api/admin/navigation");
            return data ?? new();
        }
        catch { return new(); }
    }

    public async Task<LearningSectionDto?> CreateAsync(LearningSectionDto dto)
    {
        var res = await http.PostAsJsonAsync("api/admin/navigation", dto);
        if (!res.IsSuccessStatusCode) return null;
        InvalidateCache();
        return await res.Content.ReadFromJsonAsync<LearningSectionDto>();
    }

    public async Task<bool> UpdateAsync(LearningSectionDto dto)
    {
        var res = await http.PutAsJsonAsync($"api/admin/navigation/{dto.Id}", dto);
        if (res.IsSuccessStatusCode) InvalidateCache();
        return res.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var res = await http.DeleteAsync($"api/admin/navigation/{id}");
        if (res.IsSuccessStatusCode) InvalidateCache();
        return res.IsSuccessStatusCode;
    }
}
