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
        catch (HttpRequestException)
        {
            try
            {
                await Task.Delay(500);
                var sections = await _http.GetFromJsonAsync<List<HskLearningSection>>("/api/hsk/sections");
                if (sections != null && sections.Any())
                {
                    _sectionsCache = sections;
                    return _sectionsCache;
                }
            }
            catch { }
        }
        catch
        {
        }

        return new List<HskLearningSection>
        {
            new HskLearningSection { Name = "Luyện đề HSK", Route = "/hsk/luyen-de", Icon = "bi-journal-check", Description = "Bộ đề thi thử mô phỏng thời gian thực" },
            new HskLearningSection { Name = "Từ vựng HSK", Route = "/hsk/tu-vung", Icon = "bi-book-half", Description = "Flashcard và tra cứu từ vựng chuẩn HSK 1–6" },
            new HskLearningSection { Name = "Trò chơi", Route = "/hsk/games", Icon = "bi-controller", Description = "Game học từ & phản xạ" },
            new HskLearningSection { Name = "Bắn Từ Vựng", Route = "/games?game=hsk-shooter", Icon = "bi-crosshair", Description = "Gõ pinyin bắn từ vựng rơi" }
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

        var url = "/api/hsk/vocab";
        if (!string.IsNullOrEmpty(level)) url += $"?level={Uri.EscapeDataString(level)}";

        try
        {
            var items = await _http.GetFromJsonAsync<List<HskVocabularyItem>>(url);
            if (items != null)
            {
                _vocabCache[cacheKey] = items;
            }
            return items ?? new List<HskVocabularyItem>();
        }
        catch (HttpRequestException)
        {
            try
            {
                await Task.Delay(500);
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
            var mime = ResolveMimeType(file);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mime);
            content.Add(fileContent, "file", file.Name);
            content.Add(new StringContent(mode), "mode");

            var response = await _http.PostAsync("/api/hsk/vocab/import-excel", content);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                var msg = response.StatusCode == System.Net.HttpStatusCode.Unauthorized || response.StatusCode == System.Net.HttpStatusCode.Forbidden
                    ? "Phiên đăng nhập Quản trị viên (Admin) đã hết hạn hoặc không đủ quyền. Vui lòng đăng nhập lại."
                    : (!string.IsNullOrWhiteSpace(err) ? err : $"Máy chủ phản hồi mã lỗi {(int)response.StatusCode}: {response.ReasonPhrase}");
                return new HskImportExcelResponse(0, 1, 0, 0, null, new List<string> { $"[{file.Name}] {msg}" });
            }
            InvalidateVocabCache();
            return await response.Content.ReadFromJsonAsync<HskImportExcelResponse>();
        }
        catch (Exception ex)
        {
            return new HskImportExcelResponse(0, 1, 0, 0, null, new List<string> { $"[{file.Name}] Lỗi đọc/gửi file: {ex.Message}" });
        }
    }

    public async Task<HskImportExcelResponse?> ImportMultipleVocabularyExcelAsync(IReadOnlyList<Microsoft.AspNetCore.Components.Forms.IBrowserFile> files, string mode = "skip")
    {
        try
        {
            Console.WriteLine($"[HskService] Chuẩn bị gửi {files.Count} file tới /api/hsk/vocab/import-multiple (mode={mode})...");
            using var content = new MultipartFormDataContent();
            foreach (var file in files)
            {
                var fileContent = new StreamContent(file.OpenReadStream(20 * 1024 * 1024));
                var mime = ResolveMimeType(file);
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mime);
                content.Add(fileContent, "files", file.Name);
                Console.WriteLine($"[HskService] -> Đính kèm: {file.Name} ({file.Size} bytes, MIME={mime})");
            }
            content.Add(new StringContent(mode), "mode");

            var response = await _http.PostAsync("/api/hsk/vocab/import-multiple", content);
            Console.WriteLine($"[HskService] Server phản hồi: {(int)response.StatusCode} {response.ReasonPhrase}");

            // Nếu endpoint import-multiple chưa có hoặc 404, tự động fallback gọi import-excel tuần tự cho từng file
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Console.WriteLine("[HskService] Endpoint import-multiple trả về 404, chuyển sang gọi tuần tự import-excel...");
                int totSuccess = 0, totFail = 0, totDup = 0, totUpd = 0;
                var allErrs = new List<string>();
                string? lastJson = null;

                foreach (var file in files)
                {
                    var singleRes = await ImportVocabularyExcelAsync(file, mode);
                    if (singleRes != null)
                    {
                        totSuccess += singleRes.Success;
                        totFail += singleRes.Fail;
                        totDup += singleRes.Duplicate;
                        totUpd += singleRes.Updated;
                        lastJson = singleRes.JsonUrl ?? lastJson;
                        if (singleRes.Errors?.Any() == true) allErrs.AddRange(singleRes.Errors);
                    }
                    else
                    {
                        totFail++;
                        allErrs.Add($"[{file.Name}] Không thể tải lên file.");
                    }
                }
                InvalidateVocabCache();
                return new HskImportExcelResponse(totSuccess, totFail, totDup, totUpd, lastJson, allErrs);
            }

            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[HskService Lỗi] Chi tiết: {err}");
                var msg = response.StatusCode == System.Net.HttpStatusCode.Unauthorized || response.StatusCode == System.Net.HttpStatusCode.Forbidden
                    ? "Phiên đăng nhập Quản trị viên (Admin) đã hết hạn hoặc không đủ quyền. Vui lòng đăng nhập lại."
                    : (!string.IsNullOrWhiteSpace(err) ? err : $"Máy chủ phản hồi mã lỗi {(int)response.StatusCode}: {response.ReasonPhrase}");
                return new HskImportExcelResponse(0, files.Count, 0, 0, null, new List<string> { msg });
            }

            InvalidateVocabCache();
            var res = await response.Content.ReadFromJsonAsync<HskImportExcelResponse>();
            Console.WriteLine($"[HskService] Nhận kết quả: Success={res?.Success}, Fail={res?.Fail}, Dup={res?.Duplicate}, Upd={res?.Updated}");
            return res;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HskService Exception] {ex.Message}");
            return new HskImportExcelResponse(0, files.Count, 0, 0, null, new List<string> { $"Lỗi tải lên: {ex.Message}" });
        }
    }

    private static string ResolveMimeType(Microsoft.AspNetCore.Components.Forms.IBrowserFile file)
    {
        if (!string.IsNullOrWhiteSpace(file.ContentType) && System.Net.Http.Headers.MediaTypeHeaderValue.TryParse(file.ContentType, out _))
            return file.ContentType;
        if (file.Name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) || file.Name.EndsWith(".cvs", StringComparison.OrdinalIgnoreCase))
            return "text/csv";
        if (file.Name.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            return "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        return "application/octet-stream";
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
            if (response.IsSuccessStatusCode) InvalidateVocabCache();
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<(bool Success, int Deleted)> DeleteAllVocabularyAsync()
    {
        try
        {
            var response = await _http.DeleteAsync("/api/hsk/vocab/all");
            if (!response.IsSuccessStatusCode) return (false, 0);
            InvalidateVocabCache();
            var result = await response.Content.ReadFromJsonAsync<HskDeleteAllResult>();
            return (true, result?.Deleted ?? 0);
        }
        catch
        {
            return (false, 0);
        }
    }

    public async Task<byte[]?> GetTemplateExcelAsync()
    {
        try
        {
            return await _http.GetByteArrayAsync("/api/hsk/vocab/template-excel");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HskService] Lỗi tải file mẫu Excel: {ex.Message}");
            return null;
        }
    }
}

public record HskDeleteAllResult(int Deleted, int R2FilesDeleted);

// === Request/Response DTOs ===
public record HskSaveExamRequest(string CollectionName, string Title, int? MockTestId, object ExamData);
public record HskSaveExamResponse(string Url, int Id);
public record HskUploadMediaResponse(string Url, string Type);
public record HskImportExcelResponse(int Success, int Fail, int Duplicate, int Updated, string? JsonUrl, List<string>? Errors, List<string>? Logs = null);
public record HskVocabProgressResponse(List<int> VocabularyIds);