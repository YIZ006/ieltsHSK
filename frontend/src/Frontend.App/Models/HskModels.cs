namespace Frontend.App.Models;

/// <summary>
/// HSK 3.0 exam data loaded from JSON
/// </summary>
public class HskExamData
{
    public string Title { get; set; } = string.Empty;
    public string HskLevel { get; set; } = string.Empty; // HSK1-HSK9
    public string Skill { get; set; } = string.Empty; // listening, reading, writing, speaking
    public int TotalMinutes { get; set; }
    public string? AudioUrl { get; set; }
    public List<HskPart> Parts { get; set; } = new();
}

public class HskPart
{
    public int PartNumber { get; set; }
    public string Instruction { get; set; } = string.Empty;
    public string? InstructionHtml { get; set; }
    public string? PassageHtml { get; set; }
    public int? TimerSeconds { get; set; } // for speaking parts
    public int? ThinkTimeSeconds { get; set; } // for speaking part 2
    public string? TopicDescription { get; set; } // for speaking cue card
    public bool IsGridLayout { get; set; } // for speaking part 2 layout
    public List<HskQuestion> Questions { get; set; } = new();
}

public class HskQuestion
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = "mcq"; // mcq, fill, order, pinyin-write, char-write, match, speak-read, speak-describe, html-block
    public string Text { get; set; } = string.Empty;
    public string? AudioUrl { get; set; }
    public string? ImageUrl { get; set; }
    public List<HskOption>? Options { get; set; }
    public object? CorrectAnswer { get; set; } // string or string[] or int[]
    public string? PinyinHint { get; set; }
    public string? Hanzi { get; set; } // for pinyin-write
    public string? CorrectPinyin { get; set; } // for pinyin-write
    public string? Pinyin { get; set; } // for char-write
    public string? Meaning { get; set; } // for char-write
    public string? CorrectHanzi { get; set; } // for char-write
    public List<string>? Items { get; set; } // for order
    public List<int>? CorrectOrder { get; set; } // for order
    public List<HskMatchItem>? LeftItems { get; set; } // for match
    public List<HskMatchItem>? RightItems { get; set; } // for match
    public List<HskMatchPair>? CorrectPairs { get; set; } // for match
    public string? GroupHtml { get; set; } // for html-block
    public string? PassageText { get; set; } // for speak-read
    public string? TopicPrompt { get; set; } // for speak-describe
    public int? PrepSeconds { get; set; } // for speak-describe
    public string? ReferenceAudioUrl { get; set; } // for speak-read
    public string? SelectedOptionId { get; set; } // user selection
    public string? FillAnswer { get; set; } // user fill
    public string? UserAnswer { get; set; } // general user answer
}

public class HskOption
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}

public class HskMatchItem
{
    public string Id { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}

public class HskMatchPair
{
    public string LeftId { get; set; } = string.Empty;
    public string RightId { get; set; } = string.Empty;
}

/// <summary>
/// Vocabulary item for HSK
/// </summary>
public class HskVocabularyItem
{
    public int Id { get; set; }
    public string HskLevel { get; set; } = string.Empty;
    public string Hanzi { get; set; } = string.Empty;
    public string Pinyin { get; set; } = string.Empty;
    public string Meaning { get; set; } = string.Empty;
    public string? WordType { get; set; }
    public string? ExampleSentence { get; set; }
    public string? ExamplePinyin { get; set; }
    public string? ExampleMeaning { get; set; }
    public string? AudioUrl { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Dashboard data for HSK
/// </summary>
public class HskDashboardData
{
    public string CurrentLevel { get; set; } = string.Empty;
    public int VocabProgressPercent { get; set; }
    public int Streak { get; set; }
    public List<HskRecentResult> RecentResults { get; set; } = new();
    public List<HskLearningSection> Sections { get; set; } = new();
}

public class HskRecentResult
{
    public string Skill { get; set; } = string.Empty;
    public double Score { get; set; }
    public bool Passed { get; set; }
    public DateTime SubmittedAt { get; set; }
}

public class HskLearningSection
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
}