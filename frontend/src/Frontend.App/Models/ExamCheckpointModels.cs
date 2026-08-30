namespace Frontend.App.Models;

public class ExamCheckpointDto
{
    public string UserIdentifier { get; set; } = "guest";
    public string Skill { get; set; } = "";
    public string ExamUrl { get; set; } = "";
    public int? MockTestId { get; set; }
    public string? CollectionName { get; set; }
    public string? TestTitle { get; set; }
    public string? SessionId { get; set; }
    public int SecondsRemaining { get; set; }
    public int TotalDurationSeconds { get; set; }
    public int ActivePartIndex { get; set; }
    public DateTimeOffset LastSavedAt { get; set; } = DateTimeOffset.UtcNow;

    // IELTS Reading & Listening Answers
    public Dictionary<int, int?> SelectedOptions { get; set; } = new();
    public Dictionary<int, string> FillAnswers { get; set; } = new();

    // IELTS Writing Answers
    public Dictionary<int, string> WritingTasks { get; set; } = new();

    // IELTS Speaking Answers
    public Dictionary<int, string> SpeakingRecordings { get; set; } = new();

    public int AnsweredCount =>
        SelectedOptions.Count(o => o.Value.HasValue) +
        FillAnswers.Count(f => !string.IsNullOrWhiteSpace(f.Value)) +
        WritingTasks.Count(w => !string.IsNullOrWhiteSpace(w.Value)) +
        SpeakingRecordings.Count(s => !string.IsNullOrWhiteSpace(s.Value));
}
