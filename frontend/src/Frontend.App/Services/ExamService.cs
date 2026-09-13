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
        // Nếu có mockTestId và offline hoặc đã tải về, ưu tiên lấy từ IndexedDB khi offline
        if (mockTestId.HasValue)
        {
            var isOnline = await _offlineStorage.IsOnlineAsync();
            if (!isOnline)
            {
                var offlineExam = await _offlineStorage.GetOfflineExamModelAsync(mockTestId.Value, skill);
                if (offlineExam != null)
                {
                    NormalizeExam(offlineExam);
                    return offlineExam;
                }
            }
        }

        if (string.IsNullOrWhiteSpace(dataUrl))
        {
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

        // Trả từ cache nếu đã load rồi
        if (_cache.TryGetValue(dataUrl, out var cached)) return cached;

        try
        {
            var exam = await _http.GetFromJsonAsync<ExamData>(dataUrl);
            NormalizeExam(exam);
            if (exam != null) _cache[dataUrl] = exam;
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
