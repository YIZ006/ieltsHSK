using System.Net.Http.Json;
using Frontend.App.Models;

namespace Frontend.App.Services;

/// <summary>
/// Service fetch đề thi IELTS Reading từ Cloudflare R2 qua public URL
/// </summary>
public class ExamService
{
    private readonly HttpClient _http;
    private readonly OfflineStorageService _offlineStorage;
    private readonly Dictionary<string, ExamData> _cache = new(); // cache tránh gọi lại

    public ExamService(HttpClient http, OfflineStorageService offlineStorage)
    {
        _http = http;
        _offlineStorage = offlineStorage;
    }

    /// <summary>
    /// Load đề thi từ Cloudflare R2 URL hoặc IndexedDB nếu offline
    /// </summary>
    public async Task<ExamData?> LoadExamAsync(string dataUrl, int? mockTestId = null, string skill = "Reading")
    {
        // 1. Phục vụ 0ms từ RAM Cache nếu đã tải trong phiên hiện tại
        if (!string.IsNullOrWhiteSpace(dataUrl) && _cache.TryGetValue(dataUrl, out var ramCached))
        {
            return ramCached;
        }

        // 2. Hiển thị trước cái nào đã tải rồi từ IndexedDB (Tức thì <10ms, không phụ thuộc mạng)
        if (mockTestId.HasValue)
        {
            var offlineExam = await _offlineStorage.GetOfflineExamModelAsync(mockTestId.Value, skill);
            if (offlineExam != null)
            {
                NormalizeExam(offlineExam);
                if (!string.IsNullOrWhiteSpace(dataUrl)) _cache[dataUrl] = offlineExam;
                return offlineExam;
            }
        }
        else if (!string.IsNullOrWhiteSpace(dataUrl))
        {
            var offlineExam = await _offlineStorage.GetOfflineExamModelByUrlAsync(dataUrl);
            if (offlineExam != null)
            {
                NormalizeExam(offlineExam);
                _cache[dataUrl] = offlineExam;
                return offlineExam;
            }
        }

        if (string.IsNullOrWhiteSpace(dataUrl)) return null;

        // 3. Nếu chưa có bản tải offline, nạp từ Cloudflare R2 / Server mạng
        try
        {
            var exam = await _http.GetFromJsonAsync<ExamData>(dataUrl);
            if (exam != null)
            {
                NormalizeExam(exam);
                _cache[dataUrl] = exam;
                // Lưu đệm ngầm vào IndexedDB để các lần truy cập sau hiển thị ngay
                _ = _offlineStorage.SaveExamByUrlAsync(dataUrl, exam);
            }
            return exam;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ExamService] Lỗi load đề mạng ({ex.Message}), thử fallback offline...");
            if (mockTestId.HasValue)
            {
                var offlineExam = await _offlineStorage.GetOfflineExamModelAsync(mockTestId.Value, skill);
                if (offlineExam != null)
                {
                    NormalizeExam(offlineExam);
                    return offlineExam;
                }
            }
            return null;
        }
    }

    private static void NormalizeExam(ExamData? exam)
    {
        if (exam?.Parts == null) return;

        foreach (var part in exam.Parts)
        {
            if (part.QuestionGroups.Count == 0 && part.Questions.Count > 0)
            {
                part.QuestionGroups.Add(new QuestionGroup
                {
                    Instruction = string.Empty,
                    GroupType = "Normal",
                    Questions = part.Questions
                });
            }
        }
    }
}
