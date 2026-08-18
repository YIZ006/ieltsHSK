using System.Net.Http.Json;
using Frontend.App.Models;

namespace Frontend.App.Services;

/// <summary>
/// Service fetch đề thi TOEIC từ Cloudflare R2 hoặc sample-data
/// </summary>
public class ToeicService
{
    private readonly HttpClient _http;
    private readonly Dictionary<string, ToeicExamData> _cache = new();

    public ToeicService(HttpClient http)
    {
        _http = http;
    }

    /// <summary>
    /// Load đề thi TOEIC từ URL (Cloudflare R2 hoặc sample-data)
    /// Ví dụ: "sample-data/toeic-test-1.json"
    /// </summary>
    public async Task<ToeicExamData?> LoadExamAsync(string dataUrl)
    {
        if (string.IsNullOrWhiteSpace(dataUrl)) return null;

        if (_cache.TryGetValue(dataUrl, out var cached)) return cached;

        try
        {
            var exam = await _http.GetFromJsonAsync<ToeicExamData>(dataUrl);
            if (exam != null) _cache[dataUrl] = exam;
            return exam;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ToeicService] Lỗi load đề: {ex.Message}");
            throw; // Let ToeicTest catch it
        }
    }
}
