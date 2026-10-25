namespace Backend.Application.DTOs;

public class CultureStoryListItemDto
{
    public int Id { get; set; }
    public string TitleZh { get; set; } = string.Empty;
    public string TitleVi { get; set; } = string.Empty;
    public string Pinyin { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string? SummaryVi { get; set; }
    public string? AudioUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public int EstimatedMinutes { get; set; } = 3;
    public int ViewsCount { get; set; }
    public bool IsPublished { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}

public class CultureStoryDetailDto : CultureStoryListItemDto
{
    public string ContentZh { get; set; } = string.Empty;
    public string ContentVi { get; set; } = string.Empty;
    public string ParagraphsJson { get; set; } = "[]";
    public string VocabularyJson { get; set; } = "[]";
    public string QuestionsJson { get; set; } = "[]";
    public DateTime? UpdatedAt { get; set; }
}

public class CreateCultureStoryRequest
{
    public string TitleZh { get; set; } = string.Empty;
    public string TitleVi { get; set; } = string.Empty;
    public string Pinyin { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string Category { get; set; } = "chengyu";
    public string Level { get; set; } = "HSK 3";
    public string? SummaryVi { get; set; }
    public string ContentZh { get; set; } = string.Empty;
    public string ContentVi { get; set; } = string.Empty;
    public string ParagraphsJson { get; set; } = "[]";
    public string VocabularyJson { get; set; } = "[]";
    public string QuestionsJson { get; set; } = "[]";
    public string? AudioUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public int? EstimatedMinutes { get; set; } = 3;
    public bool? IsPublished { get; set; } = true;
}

public class UpdateCultureStoryRequest : CreateCultureStoryRequest
{
}

public class ImportCultureStoryJsonRequest
{
    public string JsonContent { get; set; } = string.Empty;
}

public class CultureStoryQuizSubmissionRequest
{
    public int StoryId { get; set; }
    public List<int> Answers { get; set; } = new();
}

public class CultureStoryQuizResultDto
{
    public int StoryId { get; set; }
    public int TotalQuestions { get; set; }
    public int CorrectCount { get; set; }
    public double ScorePercentage { get; set; }
    public List<bool> AnswerCorrectness { get; set; } = new();
    public List<int> CorrectIndices { get; set; } = new();
    public List<string> Explanations { get; set; } = new();
}
