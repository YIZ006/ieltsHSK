namespace Backend.Application.DTOs;

public class GradeWritingRequest
{
    public int TaskNumber { get; set; } = 1;
    public string Prompt { get; set; } = string.Empty;
    public string EssayText { get; set; } = string.Empty;
    public int MinWords { get; set; } = 150;
}

public class GradeWritingResponse
{
    public double OverallBand { get; set; }
    public double TaskResponseBand { get; set; }
    public double CoherenceBand { get; set; }
    public double LexicalBand { get; set; }
    public double GrammarBand { get; set; }
    public string GeneralFeedback { get; set; } = string.Empty;
    public List<string> Strengths { get; set; } = new();
    public List<string> Improvements { get; set; } = new();
    public List<WritingGrammarError> GrammarErrors { get; set; } = new();
    public List<VocabularyUpgradeDto> VocabularyUpgrades { get; set; } = new();
    public int WordCount { get; set; }
    public string GradedBy { get; set; } = "AI Engine";
}

public class WritingGrammarError
{
    public string Original { get; set; } = string.Empty;
    public string Suggestion { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
}

public class VocabularyUpgradeDto
{
    public string OriginalWord { get; set; } = string.Empty;
    public string UpgradedWord { get; set; } = string.Empty;
    public string ContextExample { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
}

public class GradeSpeakingRequest
{
    public int PartNumber { get; set; } = 1;
    public string QuestionText { get; set; } = string.Empty;
    public string Transcript { get; set; } = string.Empty;
    public double DurationMs { get; set; }
}

public class GradeSpeakingResponse
{
    public double OverallBand { get; set; }
    public double FluencyBand { get; set; }
    public double LexicalBand { get; set; }
    public double GrammarBand { get; set; }
    public double PronunciationBand { get; set; }
    public string GeneralFeedback { get; set; } = string.Empty;
    public int WordCount { get; set; }
    public int Wpm { get; set; }
    public List<string> Strengths { get; set; } = new();
    public List<string> Improvements { get; set; } = new();
    public string GradedBy { get; set; } = "AI Engine";
}

public class UpdateSubmissionGradeRequest
{
    public double BandScore { get; set; }
    public string Status { get; set; } = "Graded";
    public string? TeacherFeedback { get; set; }
    public string? DetailsJson { get; set; }
}
