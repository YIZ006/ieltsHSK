namespace Backend.Application.DTOs;

public class StoryDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Level { get; set; } = "B1";
    public string IeltsBand { get; set; } = "5.0 - 6.0";
    public string Category { get; set; } = "Đời sống";
    public string? Summary { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? AudioUrl { get; set; }
    public string? JsonUrl { get; set; }
    public int WordCount { get; set; }
    public int EstimatedMinutes { get; set; } = 5;
    public string ContentJson { get; set; } = "[]";
    public string VocabularyJson { get; set; } = "[]";
    public string QuestionsJson { get; set; } = "[]";
    public bool IsPublished { get; set; } = true;
    public int ViewsCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class StoryListItemDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Level { get; set; } = "B1";
    public string IeltsBand { get; set; } = "5.0 - 6.0";
    public string Category { get; set; } = "Đời sống";
    public string? Summary { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? AudioUrl { get; set; }
    public string? JsonUrl { get; set; }
    public int WordCount { get; set; }
    public int EstimatedMinutes { get; set; } = 5;
    public int TargetVocabCount { get; set; }
    public int QuestionsCount { get; set; }
    public bool IsPublished { get; set; } = true;
    public int ViewsCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateStoryRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string Level { get; set; } = "B1";
    public string IeltsBand { get; set; } = "5.0 - 6.0";
    public string Category { get; set; } = "Đời sống";
    public string? Summary { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? AudioUrl { get; set; }
    public string? JsonUrl { get; set; }
    public int WordCount { get; set; }
    public int EstimatedMinutes { get; set; } = 5;
    public string ContentJson { get; set; } = "[]";
    public string VocabularyJson { get; set; } = "[]";
    public string QuestionsJson { get; set; } = "[]";
    public bool IsPublished { get; set; } = true;
}

public class StoryQuizSubmissionRequest
{
    public int StoryId { get; set; }
    public List<int> Answers { get; set; } = new();
}

public class StoryQuizResultDto
{
    public int StoryId { get; set; }
    public int TotalQuestions { get; set; }
    public int CorrectCount { get; set; }
    public double ScorePercentage { get; set; }
    public List<bool> AnswerCorrectness { get; set; } = new();
    public List<int> CorrectIndices { get; set; } = new();
    public List<string> Explanations { get; set; } = new();
}

public class ImportStoryJsonRequest
{
    public string JsonContent { get; set; } = string.Empty;
}

