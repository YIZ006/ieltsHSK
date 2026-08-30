using System.Net.Http.Json;
using Frontend.App.Models;

namespace Frontend.App.Services;

public sealed class NavigationService(HttpClient http)
{
    public async Task<List<LearningSectionDto>> GetAsync(string? language = null)
    {
        var url = string.IsNullOrWhiteSpace(language)
            ? "api/navigation"
            : $"api/navigation?language={Uri.EscapeDataString(language)}";
        try
        {
            var data = await http.GetFromJsonAsync<List<LearningSectionDto>>(url);
            return data ?? new();
        }
        catch { return new(); }
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
        return await res.Content.ReadFromJsonAsync<LearningSectionDto>();
    }

    public async Task<bool> UpdateAsync(LearningSectionDto dto)
    {
        var res = await http.PutAsJsonAsync($"api/admin/navigation/{dto.Id}", dto);
        return res.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var res = await http.DeleteAsync($"api/admin/navigation/{id}");
        return res.IsSuccessStatusCode;
    }
}
