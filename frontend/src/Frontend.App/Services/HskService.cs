using System.Net.Http.Json;
using Frontend.App.Models;

namespace Frontend.App.Services;

public class HskService
{
    private readonly HttpClient _http;
    private readonly Dictionary<string, HskExamData> _examCache = new();

    public HttpClient Client => _http;

    public HskService(HttpClient http)
    {
        _http = http;
    }

    // === Exam loading ===
    public async Task<HskExamData?> LoadExamAsync(string dataUrl)
    {
        if (string.IsNullOrWhiteSpace(dataUrl)) return null;
        if (_examCache.TryGetValue(dataUrl, out var cached)) return cached;

        try
        {
            var exam = await _http.GetFromJsonAsync<HskExamData>(dataUrl);
            if (exam != null) _examCache[dataUrl] = exam;
            return exam;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HskService] Lỗi load đề: {ex.Message}");
            throw;
        }
    }

    // === Sections ===
    public async Task<List<HskLearningSection>> GetSectionsAsync()
    {
        try
        {
            var sections = await _http.GetFromJsonAsync<List<HskLearningSection>>("/api/hsk/sections");
            return sections ?? new List<HskLearningSection>();
        }
        catch
        {
            return new List<HskLearningSection>();
        }
    }

    // === Vocabulary ===
    public async Task<List<HskVocabularyItem>> GetVocabularyAsync(string? level = null)
    {
        try
        {
            var url = "/api/hsk/vocab";
            if (!string.IsNullOrEmpty(level)) url += $"?level={Uri.EscapeDataString(level)}";
            var items = await _http.GetFromJsonAsync<List<HskVocabularyItem>>(url);
            return items ?? new List<HskVocabularyItem>();
        }
        catch
        {
            return new List<HskVocabularyItem>();
        }
    }

    // === Admin: Save exam ===
    public async Task<HskSaveExamResponse?> SaveExamAsync(HskSaveExamRequest request)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("/api/hsk/save-exam", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<HskSaveExamResponse>();
        }
        catch
        {
            return null;
        }
    }

    // === Admin: Upload media ===
    public async Task<string?> UploadMediaAsync(Stream stream, string fileName, string contentType)
    {
        try
        {
            var content = new MultipartFormDataContent();
            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            content.Add(fileContent, "file", fileName);

            var response = await _http.PostAsync("/api/hsk/upload-media", content);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<HskUploadMediaResponse>();
            return result?.Url;
        }
        catch
        {
            return null;
        }
    }

    // === Admin: Create/Update vocabulary ===
    public async Task<bool> CreateVocabularyAsync(HskVocabularyItem item)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("/api/hsk/vocab", item);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UpdateVocabularyAsync(int id, HskVocabularyItem item)
    {
        try
        {
            var response = await _http.PutAsJsonAsync($"/api/hsk/vocab/{id}", item);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DeleteVocabularyAsync(int id)
    {
        try
        {
            var response = await _http.DeleteAsync($"/api/hsk/vocab/{id}");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}

// === Request/Response DTOs ===
public record HskSaveExamRequest(string CollectionName, string Title, int? MockTestId, object ExamData);
public record HskSaveExamResponse(string Url, int Id);
public record HskUploadMediaResponse(string Url, string Type);