using System.Net.Http.Json;
using Frontend.App.Models;

namespace Frontend.App.Services;

/// <summary>
/// Service fetch đề thi IELTS Reading từ Cloudflare R2 qua public URL
/// </summary>
public class ExamService
{
    private readonly HttpClient _http;
    private readonly Dictionary<string, ExamData> _cache = new(); // cache tránh gọi lại

    public ExamService(HttpClient http)
    {
        _http = http;
    }

    /// <summary>
    /// Load đề thi từ Cloudflare R2 URL
    /// Ví dụ: https://pub-xxx.r2.dev/exams/cambridge-reading-test-1.json
    /// </summary>
    public async Task<ExamData?> LoadExamAsync(string dataUrl)
    {
        if (string.IsNullOrWhiteSpace(dataUrl)) return null;

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
            Console.WriteLine($"[ExamService] Lỗi load đề: {ex.Message}");
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
