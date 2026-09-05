using System.Net.Http.Json;
using Frontend.App.Models;

namespace Frontend.App.Services;

/// <summary>
/// Service fetch đề thi TOEIC từ Cloudflare R2 hoặc sample-data
/// </summary>
public class ToeicService
{
    /// <summary>
    /// Thư mục gốc chứa các đề thi TOEIC trên Cloudflare R2
    /// </summary>
    public const string ToeicDataBaseUrl = "https://pub-91655bd1442d498b9788d1f8f8575587.r2.dev/Cuongkeng/Toeic%20Data/";

    private readonly HttpClient _http;
    private readonly Dictionary<string, string> _rawJsonCache = new();
    private static readonly System.Text.Json.JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
    };

    public ToeicService(HttpClient http)
    {
        _http = http;
    }

    /// <summary>
    /// Xoá cache JSON trong bộ nhớ để buộc tải lại dữ liệu mới nhất
    /// </summary>
    public void ClearCache()
    {
        _rawJsonCache.Clear();
    }

    /// <summary>
    /// Load đề thi TOEIC từ URL (Cloudflare R2 hoặc sample-data)
    /// Ví dụ: "sample-data/toeic-test-1.json"
    /// Hoặc tên file trên R2: "TOEIC ETS 2026-Test 1.json"
    /// Luôn trả về đối tượng ToeicExamData mới tinh để tránh lỗi mutate trạng thái khi chọn từng part.
    /// </summary>
    public async Task<ToeicExamData?> LoadExamAsync(string dataUrl, bool forceReload = false)
    {
        if (string.IsNullOrWhiteSpace(dataUrl)) return null;

        var requestUrl = ResolveUrl(dataUrl);
        string json;

        if (!forceReload && _rawJsonCache.TryGetValue(requestUrl, out var cachedJson))
        {
            json = cachedJson;
        }
        else
        {
            try
            {
                using var response = await _http.GetAsync(requestUrl);
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"HTTP {(int)response.StatusCode} ({response.StatusCode}) khi tải: {requestUrl}");
                }

                json = await response.Content.ReadAsStringAsync();
                _rawJsonCache[requestUrl] = json;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ToeicService] Lỗi load đề từ '{requestUrl}': {ex.Message}");
                throw;
            }
        }

        ToeicExamData? exam;
        try
        {
            exam = System.Text.Json.JsonSerializer.Deserialize<ToeicExamData>(json, _jsonOptions);
        }
        catch (Exception dex)
        {
            throw new Exception($"Lỗi parse JSON ({json.Length:N0} ký tự): {dex.Message}");
        }

        if (exam == null || exam.Parts.Count == 0)
        {
            throw new Exception($"JSON hợp lệ nhưng không có phần thi (parts={(exam?.Parts.Count ?? 0)}).");
        }

        return exam;
    }

    /// <summary>
    /// Path tương đối (không phải http/sample-data) sẽ được nối vào thư mục TOEIC trên R2,
    /// đồng thời encode khoảng trắng trong đường dẫn.
    /// </summary>
    public static string ResolveUrl(string dataUrl)
    {
        var url = dataUrl.Trim();

        var isAbsolute = url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                      || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
        var isSampleData = url.StartsWith("sample-data/", StringComparison.OrdinalIgnoreCase);

        if (!isAbsolute && !isSampleData)
        {
            url = ToeicDataBaseUrl + url.TrimStart('/');
        }

        if (!isSampleData)
        {
            url = url.Replace(" ", "%20");
        }

        return url;
    }
}
