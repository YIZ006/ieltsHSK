using Frontend.App.Models;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;

namespace Frontend.App.Services;

public class GrammarStructureService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _configuration;
    private List<GrammarStructureDto>? _cache;

    public GrammarStructureService(HttpClient http, IConfiguration configuration)
    {
        _http = http;
        _configuration = configuration;
    }

    private string GetApiUrl(string path)
    {
        var baseUrl = _configuration["BackendApi:BaseUrl"] ?? "http://localhost:5101/";
        return $"{baseUrl.TrimEnd('/')}/{path.TrimStart('/')}";
    }

    public async Task<List<GrammarStructureDto>> GetStructuresAsync(
        bool forceRefresh = false,
        string? search = null,
        string? bandLevel = null,
        string? category = null,
        string? topic = null)
    {
        if (!forceRefresh && _cache != null && string.IsNullOrEmpty(search) && string.IsNullOrEmpty(bandLevel) && string.IsNullOrEmpty(category) && string.IsNullOrEmpty(topic))
        {
            return _cache;
        }

        try
        {
            var queryParams = new List<string>();
            var ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            queryParams.Add($"_t={ts}");

            if (!string.IsNullOrWhiteSpace(search)) queryParams.Add($"search={Uri.EscapeDataString(search)}");
            if (!string.IsNullOrWhiteSpace(bandLevel) && bandLevel != "all") queryParams.Add($"bandLevel={Uri.EscapeDataString(bandLevel)}");
            if (!string.IsNullOrWhiteSpace(category) && category != "all") queryParams.Add($"category={Uri.EscapeDataString(category)}");
            if (!string.IsNullOrWhiteSpace(topic) && topic != "all") queryParams.Add($"grammarTopic={Uri.EscapeDataString(topic)}");

            var url = GetApiUrl($"api/grammar-structures?{string.Join("&", queryParams)}");
            var result = await _http.GetFromJsonAsync<List<GrammarStructureDto>>(url);
            
            if (string.IsNullOrEmpty(search) && (string.IsNullOrEmpty(bandLevel) || bandLevel == "all") && (string.IsNullOrEmpty(category) || category == "all") && (string.IsNullOrEmpty(topic) || topic == "all"))
            {
                _cache = result ?? new List<GrammarStructureDto>();
            }

            return result ?? new List<GrammarStructureDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching grammar structures: {ex.Message}");
            return _cache ?? new List<GrammarStructureDto>();
        }
    }

    public async Task<GrammarStructureDto?> GetByIdAsync(int id)
    {
        try
        {
            return await _http.GetFromJsonAsync<GrammarStructureDto>(GetApiUrl($"api/grammar-structures/{id}"));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching grammar structure by ID: {ex.Message}");
            return null;
        }
    }

    public async Task<GrammarStructureDto?> CreateAsync(CreateGrammarStructureDto dto)
    {
        try
        {
            var res = await _http.PostAsJsonAsync(GetApiUrl("api/admin/grammar-structures"), dto);
            if (res.IsSuccessStatusCode)
            {
                InvalidateCache();
                return await res.Content.ReadFromJsonAsync<GrammarStructureDto>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating grammar structure: {ex.Message}");
        }
        return null;
    }

    public async Task<bool> UpdateAsync(int id, UpdateGrammarStructureDto dto)
    {
        try
        {
            var res = await _http.PutAsJsonAsync(GetApiUrl($"api/admin/grammar-structures/{id}"), dto);
            if (res.IsSuccessStatusCode)
            {
                InvalidateCache();
                return true;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating grammar structure: {ex.Message}");
        }
        return false;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        try
        {
            var res = await _http.DeleteAsync(GetApiUrl($"api/admin/grammar-structures/{id}"));
            if (res.IsSuccessStatusCode)
            {
                InvalidateCache();
                return true;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting grammar structure: {ex.Message}");
        }
        return false;
    }

    public async Task<int> BulkDeleteAsync(List<int> ids)
    {
        try
        {
            var res = await _http.PostAsJsonAsync(GetApiUrl("api/admin/grammar-structures/bulk-delete"), new GrammarBulkDeleteDto { Ids = ids });
            if (res.IsSuccessStatusCode)
            {
                InvalidateCache();
                var result = await res.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
                if (result.TryGetProperty("deletedCount", out var countElem))
                {
                    return countElem.GetInt32();
                }
                return ids.Count;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error bulk deleting grammar structures: {ex.Message}");
        }
        return 0;
    }

    public async Task<GrammarImportExcelResponse?> ImportMultipleExcelAsync(IReadOnlyList<IBrowserFile> files, string mode = "upsert")
    {
        try
        {
            using var content = new MultipartFormDataContent();
            foreach (var file in files)
            {
                var fileContent = new StreamContent(file.OpenReadStream(25 * 1024 * 1024));
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
                    string.IsNullOrWhiteSpace(file.ContentType) ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" : file.ContentType);
                content.Add(fileContent, "files", file.Name);
            }
            content.Add(new StringContent(mode), "mode");

            var response = await _http.PostAsync(GetApiUrl("api/admin/grammar-structures/import-multiple"), content);
            if (!response.IsSuccessStatusCode) return null;
            InvalidateCache();
            return await response.Content.ReadFromJsonAsync<GrammarImportExcelResponse>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error importing grammar excel files: {ex.Message}");
            return null;
        }
    }

    public string GetTemplateDownloadUrl()
    {
        return GetApiUrl("api/admin/grammar-structures/template");
    }

    public void InvalidateCache()
    {
        _cache = null;
    }
}
