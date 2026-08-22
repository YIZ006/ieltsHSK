namespace Frontend.App.Models;

// ══════════════════════════════════════════════════════
// EXISTING SPEAKING TEST MODELS
// ══════════════════════════════════════════════════════

public class SpeakingExamData
{
    public string Title { get; set; } = "";
    public int TotalMinutes { get; set; } = 14;
    public List<SpeakingPart> Parts { get; set; } = new();
}

public class SpeakingPart
{
    public int PartNumber { get; set; }
    public string PartTitle { get; set; } = "";
    public string Caption { get; set; } = "";
    public int TimerSeconds { get; set; }
    public int ThinkTimeSeconds { get; set; }
    public bool IsGridLayout { get; set; }
    public string? TopicDescription { get; set; }
    public List<SpeakingQuestion> Questions { get; set; } = new();
}

public class SpeakingQuestion
{
    public int Id { get; set; }
    public string Question { get; set; } = "";
    public string VideoUrl { get; set; } = "";
}

// ══════════════════════════════════════════════════════
// SPEAK ALONG (NÓI THEO / SHADOWING) MODELS
// ══════════════════════════════════════════════════════

public class SpeakAlongExamData
{
    public string Title { get; set; } = "";
    public string Level { get; set; } = ""; // e.g., "IELTS 6.0", "IELTS 7.0+"
    public string Part { get; set; } = ""; // "Part 1", "Part 2", "Part 3"
    public List<SpeakAlongItem> Items { get; set; } = new();
}

public class SpeakAlongItem
{
    public int Id { get; set; }
    public string Question { get; set; } = "";
    public string ModelAnswer { get; set; } = "";
    public string ModelAudioUrl { get; set; } = "";
    public string VideoUrl { get; set; } = "";
    public int EstimatedDurationSeconds { get; set; } = 30;
    public List<SpeakAlongSegment> Segments { get; set; } = new(); // For timed transcript sync
    public string Vocabulary { get; set; } = ""; // Key vocabulary/phrases
    public string Tips { get; set; } = "";
}

public class SpeakAlongSegment
{
    public double StartTime { get; set; } // seconds
    public double EndTime { get; set; } // seconds
    public string Text { get; set; } = "";
}

public class SpeakAlongResult
{
    public string Transcript { get; set; } = "";
    public double DurationMs { get; set; }
    public double ModelDurationMs { get; set; }
    public int SimilarityScore { get; set; } // 0-100 overall similarity
    public int PronunciationScore { get; set; } // 0-100
    public int FluencyScore { get; set; } // 0-100
    public int TimingScore { get; set; } // 0-100 (how well timing matches model)
    public int WordCount { get; set; }
    public int Wpm { get; set; }
    public List<string> MispronouncedWords { get; set; } = new();
    public List<string> MissingWords { get; set; } = new();
    public List<string> ExtraWords { get; set; } = new();
    public List<SpeakAlongSegmentResult> SegmentResults { get; set; } = new();
}

public class SpeakAlongSegmentResult
{
    public double StartTime { get; set; }
    public double EndTime { get; set; }
    public string ModelText { get; set; } = "";
    public string UserText { get; set; } = "";
    public int Similarity { get; set; }
    public bool IsMatched { get; set; }
}

public class SpeakAlongSessionRecord
{
    public int ItemId { get; set; }
    public string Question { get; set; } = "";
    public string ModelAnswer { get; set; } = "";
    public string UserTranscript { get; set; } = "";
    public double DurationMs { get; set; }
    public SpeakAlongResult Result { get; set; } = new();
    public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;
}