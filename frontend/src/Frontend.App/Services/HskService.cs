using System.Net.Http.Json;
using Blazored.LocalStorage;
using Frontend.App.Models;

namespace Frontend.App.Services;

public class HskService
{
    private readonly HttpClient _http;
    private readonly ILocalStorageService _localStorage;
    private readonly Dictionary<string, HskExamData> _examCache = new();
    private readonly Dictionary<string, List<HskVocabularyItem>> _vocabCache = new();
    private List<HskLearningSection>? _sectionsCache;

    public HttpClient Client => _http;

    public HskService(HttpClient http, ILocalStorageService localStorage)
    {
        _http = http;
        _localStorage = localStorage;
    }

    public void InvalidateVocabCache() => _vocabCache.Clear();
    public bool HasVocabInCache(string? level = null) => _vocabCache.ContainsKey(string.IsNullOrEmpty(level) ? "all" : level);

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
    public async Task<List<HskLearningSection>> GetSectionsAsync(bool forceRefresh = false)
    {
        if (!forceRefresh && _sectionsCache != null) return _sectionsCache;

        try
        {
            var sections = await _http.GetFromJsonAsync<List<HskLearningSection>>("/api/hsk/sections");
            if (sections != null && sections.Any())
            {
                _sectionsCache = sections;
                return _sectionsCache;
            }
        }
        catch
        {
        }

        return new List<HskLearningSection>
        {
            new HskLearningSection { Name = "Từ vựng HSK", Route = "/hsk/vocabulary", Icon = "bi-book-half", Description = "Flashcard và tra cứu từ vựng chuẩn HSK 1–6" },
            new HskLearningSection { Name = "Luyện nghe", Route = "/hsk/listening", Icon = "bi-headphones", Description = "Nghe hội thoại và đoạn văn chuẩn phổ thông" },
            new HskLearningSection { Name = "Luyện đọc", Route = "/hsk/reading", Icon = "bi-journal-text", Description = "Đọc hiểu đoạn văn, nối câu và sắp xếp câu" },
            new HskLearningSection { Name = "Luyện viết", Route = "/hsk/writing", Icon = "bi-pencil-square", Description = "Tập viết chữ Hán, điền từ và dịch thuật" },
            new HskLearningSection { Name = "Luyện nói HSKK", Route = "/hsk/speaking", Icon = "bi-mic-fill", Description = "Luyện phát âm, đọc to và miêu tả tranh" },
            new HskLearningSection { Name = "Thi thử HSK", Route = "/hsk/mock-tests", Icon = "bi-journal-check", Description = "Bộ đề thi thử mô phỏng thời gian thực" },
            new HskLearningSection { Name = "Bắn Từ Vựng", Route = "/hsk/vocab-shooter", Icon = "bi-crosshair", Description = "Gõ pinyin bắn từ vựng rơi" }
        };
    }

    // === Vocabulary ===
    public async Task<List<HskVocabularyItem>?> GetVocabularyAsync(string? level = null, bool forceRefresh = false)
    {
        var cacheKey = string.IsNullOrEmpty(level) ? "all" : level;
        if (!forceRefresh && _vocabCache.TryGetValue(cacheKey, out var cachedVocab))
        {
            return cachedVocab;
        }

        try
        {
            var url = "/api/hsk/vocab";
            if (!string.IsNullOrEmpty(level)) url += $"?level={Uri.EscapeDataString(level)}";
            var items = await _http.GetFromJsonAsync<List<HskVocabularyItem>>(url);
            if (items != null)
            {
                _vocabCache[cacheKey] = items;
            }
            return items ?? new List<HskVocabularyItem>();
        }
        catch
        {
            return _vocabCache.TryGetValue(cacheKey, out var fallback) ? fallback : null;
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

    // === Admin: Import vocabulary from Excel/CSV (mode: "skip" | "upsert") ===
    public async Task<HskImportExcelResponse?> ImportVocabularyExcelAsync(Microsoft.AspNetCore.Components.Forms.IBrowserFile file, string mode = "skip")
    {
        try
        {
            using var content = new MultipartFormDataContent();
            var fileContent = new StreamContent(file.OpenReadStream(20 * 1024 * 1024));
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
            content.Add(fileContent, "file", file.Name);
            content.Add(new StringContent(mode), "mode");

            var response = await _http.PostAsync("/api/hsk/vocab/import-excel", content);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<HskImportExcelResponse>();
        }
        catch
        {
            return null;
        }
    }

    // === Vocabulary Progress (theo tài khoản) ===
    public async Task<List<int>?> GetVocabProgressAsync()
    {
        try
        {
            var result = await _http.GetFromJsonAsync<HskVocabProgressResponse>("/api/hsk/vocab/progress");
            return result?.VocabularyIds ?? new List<int>();
        }
        catch
        {
            return null; // chưa đăng nhập / lỗi -> dùng localStorage
        }
    }

    public async Task<bool> UpdateVocabProgressAsync(int vocabularyId, bool learned)
    {
        try
        {
            var response = await _http.PostAsJsonAsync($"/api/hsk/vocab/progress/{vocabularyId}", new { learned });
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    // === Gộp tiến độ cũ trong localStorage lên tài khoản (chạy khi đăng nhập) ===
    public async Task MigrateLocalProgressToAccountAsync()
    {
        const string MarkerKey = "hsk_progress_migrated";
        try
        {
            // Đã migrate rồi thì bỏ qua (tránh quét lại mỗi lần load)
            if (await _localStorage.GetItemAsync<bool>(MarkerKey)) return;

            var allIds = new HashSet<int>();
            var keysFound = new List<string>();
            foreach (var level in new[] { "HSK1", "HSK2", "HSK3", "HSK4", "HSK5", "HSK6", "HSK7", "HSK8", "HSK9" })
            {
                var key = $"hsk_learned_{level}";
                var stored = await _localStorage.GetItemAsync<string>(key);
                if (string.IsNullOrEmpty(stored)) continue;
                keysFound.Add(key);
                try
                {
                    var ids = System.Text.Json.JsonSerializer.Deserialize<List<int>>(stored);
                    if (ids != null)
                        foreach (var id in ids) allIds.Add(id);
                }
                catch { }
            }

            if (keysFound.Count == 0)
            {
                await _localStorage.SetItemAsync(MarkerKey, true);
                return;
            }

            var response = await _http.PostAsJsonAsync("/api/hsk/vocab/progress/migrate",
                new { vocabularyIds = allIds.ToList() });

            // Chỉ dọn key khi đẩy thành công, tránh mất dữ liệu nếu lỗi mạng
            if (response.IsSuccessStatusCode)
            {
                foreach (var key in keysFound)
                    await _localStorage.RemoveItemAsync(key);
                await _localStorage.SetItemAsync(MarkerKey, true);
            }
        }
        catch
        {
            // Lỗi mạng -> giữ nguyên localStorage, lần sau thử lại
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
public record HskImportExcelResponse(int Success, int Fail, int Duplicate, int Updated, string? JsonUrl, List<string>? Errors);
public record HskVocabProgressResponse(List<int> VocabularyIds);