namespace Frontend.App.Models;

public class WritingExamData
{
    public string Title { get; set; } = "";
    public int TotalMinutes { get; set; } = 60;
    public string? HskLevel { get; set; } // null for IELTS, e.g. "HSK3" for HSK
    public List<WritingTask> Tasks { get; set; } = new();
}

public class WritingTask
{
    public int TaskNumber { get; set; }
    public string TaskTitle { get; set; } = "";
    public int TimeRecommended { get; set; }
    public int MinWords { get; set; }
    public string Instruction { get; set; } = "";
    public string Prompt { get; set; } = "";
    public string RequireWords { get; set; } = "";
    public string? ImageUrl { get; set; }
    public string? ImageAlt { get; set; }

    // User's essay answer (not from JSON)
    [System.Text.Json.Serialization.JsonIgnore]
    public string Answer { get; set; } = "";

    [System.Text.Json.Serialization.JsonIgnore]
    public int WordCount => string.IsNullOrWhiteSpace(Answer)
        ? 0
        : Answer.Trim().Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
}
