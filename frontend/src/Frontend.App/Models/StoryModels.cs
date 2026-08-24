namespace Frontend.App.Models;

public class StoryListItemModel
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

public class StoryModel
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

    // Parsed objects
    public List<StoryParagraph> Paragraphs { get; set; } = new();
    public List<StoryVocabulary> Vocabulary { get; set; } = new();
    public List<StoryQuestion> Questions { get; set; } = new();
}

public class StoryParagraph
{
    public string En { get; set; } = string.Empty;
    public string Vi { get; set; } = string.Empty;
}

public class StoryVocabulary
{
    public string Word { get; set; } = string.Empty;
    public string Phonetic { get; set; } = string.Empty;
    public string Pos { get; set; } = string.Empty;
    public string Meaning { get; set; } = string.Empty;
    public string Example { get; set; } = string.Empty;
    public List<string> Collocations { get; set; } = new();
}

public class StoryQuestion
{
    public string Question { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new();
    public int CorrectIndex { get; set; }
    public string Explanation { get; set; } = string.Empty;
}

public class StoryQuizResultModel
{
    public int StoryId { get; set; }
    public int TotalQuestions { get; set; }
    public int CorrectCount { get; set; }
    public double ScorePercentage { get; set; }
    public List<bool> AnswerCorrectness { get; set; } = new();
    public List<int> CorrectIndices { get; set; } = new();
    public List<string> Explanations { get; set; } = new();
}
