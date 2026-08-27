using Blazored.LocalStorage;
using Frontend.App.Models;

namespace Frontend.App.Services;

/// <summary>Một thành tích TOEIC.</summary>
public class ToeicAchievement
{
    public string Id { get; set; } = "";
    public string Icon { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public bool Unlocked { get; set; }
    public string ProgressText { get; set; } = "";
    public int ProgressPercent { get; set; }
}

/// <summary>
/// Hệ thống thành tích TOEIC — tính trực tiếp từ dữ liệu thật:
/// chuỗi học (StreakService), bài thi đã nộp (ExamSubmissionService),
/// số từ flashcard đã thuộc (localStorage).
/// </summary>
public class ToeicAchievementService(
    ILocalStorageService localStorage,
    ExamSubmissionService submissions,
    StreakService streak)
{
    private const string FlashcardStateKey = "toeic_flashcards_v1";

    public async Task<List<ToeicAchievement>> GetAllAsync()
    {
        var flashcardState = await localStorage.GetItemAsync<ToeicFlashcardState>(FlashcardStateKey);
        var learnedWords = flashcardState?.Learned?.Count ?? 0;

        var toeicSubs = (await submissions.GetAllAsync())
            .Where(s => (s.ExamUrl ?? "").Contains("toeic", StringComparison.OrdinalIgnoreCase)
                        || (s.Skill ?? "").Contains("toeic", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var examCount = toeicSubs.Count;
        var distinctExams = toeicSubs
            .Select(s => (s.ExamUrl ?? "").Trim())
            .Where(u => u.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
        var bestScore = toeicSubs
            .Where(s => s.TotalScore.HasValue)
            .Select(s => s.TotalScore.GetValueOrDefault())
            .DefaultIfEmpty(0)
            .Max();
        var hasPerfect = toeicSubs.Any(s =>
            s.CorrectCount.HasValue
            && s.TotalQuestions.GetValueOrDefault() > 0
            && s.CorrectCount.GetValueOrDefault() == s.TotalQuestions.GetValueOrDefault());

        var currentStreak = await streak.GetCurrentStreakAsync();
        var bestStreak = Math.Max(currentStreak, await streak.GetBestStreakAsync());

        return new List<ToeicAchievement>
        {
            Make("streak7", "🔥", "7 ngày liên tiếp", "Học 7 ngày không nghỉ", bestStreak, 7, "ngày"),
            Make("firstExam", "🎯", "Khởi đầu", "Hoàn thành bài thi TOEIC đầu tiên", examCount, 1, "bài"),
            Make("vocab50", "📚", "Từ điển sống", "Thuộc 50 từ vựng flashcard", learnedWords, 50, "từ"),
            Make("exam10", "⚡", "Học chăm chỉ", "Hoàn thành 10 bài thi TOEIC", examCount, 10, "bài"),
            Make("perfect", "✨", "Bài hoàn hảo", "Trả lời đúng 100% một bài thi", hasPerfect ? 1 : 0, 1, ""),
            Make("score780", "🏅", "Xuất sắc", "Đạt 780+ điểm TOEIC", bestScore, 780, "điểm"),
            Make("score990", "👑", "Điểm tuyệt đối", "Chinh phục 990 điểm TOEIC", bestScore, 990, "điểm"),
            Make("explore3", "🗺️", "Nhà thám hiểm", "Làm 3 đề thi khác nhau", distinctExams, 3, "đề"),
        };
    }

    private static ToeicAchievement Make(string id, string icon, string title, string description, int value, int target, string unit)
    {
        var unlocked = value >= target;
        var percent = target <= 0 ? 0 : Math.Min(100, value * 100 / target);

        return new ToeicAchievement
        {
            Id = id,
            Icon = icon,
            Title = title,
            Description = description,
            Unlocked = unlocked,
            ProgressText = unlocked ? "Đã đạt" : $"{value}/{target}{(unit.Length > 0 ? " " + unit : "")}",
            ProgressPercent = percent
        };
    }
}
