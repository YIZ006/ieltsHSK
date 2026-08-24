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
            if (sections != null && sections.Any()) return sections;
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
            new HskLearningSection { Name = "Thi thử HSK", Route = "/hsk/mock-tests", Icon = "bi-journal-check", Description = "Bộ đề thi thử mô phỏng thời gian thực" }
        };
    }

    // === Vocabulary ===
    // Trả về null khi lỗi kết nối API; trả về list (có thể rỗng) khi gọi thành công.
    public async Task<List<HskVocabularyItem>?> GetVocabularyAsync(string? level = null)
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
            return null;
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