using System.Net.Http.Json;
using Frontend.App.Models;

namespace Frontend.App.Services;

public sealed class ExamAnswerKey
{
    public string Title { get; set; } = "";
    public Dictionary<string, List<string>> Answers { get; set; } = new();
}

public sealed class QuestionResult
{
    public int QuestionNumber { get; set; }
    public string StudentAnswer { get; set; } = "";
    public string CorrectAnswer { get; set; } = "";
    public List<string> AcceptedAnswers { get; set; } = new();
    public bool IsCorrect { get; set; }
    public bool IsBlank { get; set; }
}

public sealed class GradingResult
{
    public Dictionary<int, QuestionResult> Questions { get; set; } = new();
    public int CorrectCount { get; set; }
    public int TotalCount { get; set; }
    public double BandScore { get; set; }
}

/// <summary>
/// Tải đáp án tách biệt với dữ liệu đề thi từ URL công khai (Cloudflare R2).
/// </summary>
public sealed class AnswerKeyService
{
    private readonly HttpClient _http;
    private readonly Dictionary<string, ExamAnswerKey> _cache = new();

    public AnswerKeyService(HttpClient http) => _http = http;

    public async Task<ExamAnswerKey?> LoadAsync(string answerUrl)
    {
        if (string.IsNullOrWhiteSpace(answerUrl)) return null;
        if (_cache.TryGetValue(answerUrl, out var cached)) return cached;

        try
        {
            var key = await _http.GetFromJsonAsync<ExamAnswerKey>(answerUrl);
            if (key != null) _cache[answerUrl] = key;
            return key;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AnswerKeyService] Không thể tải đáp án: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Derives the answer URL from the exam JSON URL: replaces .json → .answers.json
    /// Works for both local sample-data/ and Cloudflare R2 public URLs.
    /// </summary>
    public Task<ExamAnswerKey?> LoadFromExamUrlAsync(string examUrl)
    {
        var answerUrl = examUrl.Replace(".json", ".answers.json", StringComparison.OrdinalIgnoreCase);
        return LoadAsync(answerUrl);
    }

    /// <summary>
    /// Grades the student answers against the key.
    /// studentAnswers: key = 1-based question number, value = student's answer string
    /// </summary>
    public static GradingResult Grade(ExamAnswerKey answerKey, Dictionary<int, string> studentAnswers)
    {
        var results = new Dictionary<int, QuestionResult>();
        int correct = 0;

        foreach (var (qNumStr, acceptedAnswers) in answerKey.Answers)
        {
            if (!int.TryParse(qNumStr, out int qNum)) continue;
            studentAnswers.TryGetValue(qNum, out string? student);
            student = (student ?? "").Trim();

            bool isCorrect = acceptedAnswers.Any(a =>
                string.Equals(a.Trim(), student, StringComparison.OrdinalIgnoreCase));

            results[qNum] = new QuestionResult
            {
                QuestionNumber = qNum,
                StudentAnswer = student,
                CorrectAnswer = acceptedAnswers.First(),
                AcceptedAnswers = acceptedAnswers,
                IsCorrect = isCorrect,
                IsBlank = string.IsNullOrWhiteSpace(student)
            };

            if (isCorrect) correct++;
        }

        return new GradingResult
        {
            Questions = results,
            CorrectCount = correct,
            TotalCount = results.Count,
            BandScore = CalcBandScore(correct, results.Count)
        };
    }

    private static double CalcBandScore(int correct, int total)
    {
        if (total == 0) return 0;
        // Scale to 40 if needed
        int scaled = total == 40 ? correct : (int)Math.Round((double)correct / total * 40);
        return scaled switch
        {
            >= 39 => 9.0,
            >= 37 => 8.5,
            >= 35 => 8.0,
            >= 32 => 7.5,
            >= 30 => 7.0,
            >= 26 => 6.5,
            >= 23 => 6.0,
            >= 18 => 5.5,
            >= 16 => 5.0,
            >= 13 => 4.5,
            >= 11 => 4.0,
            >= 9  => 3.5,
            >= 5  => 3.0,
            _     => 2.5
        };
    }
}
