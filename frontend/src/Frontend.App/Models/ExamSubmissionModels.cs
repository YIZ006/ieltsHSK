namespace Frontend.App.Models;

public sealed class IeltsSubmissionRecord
{
    public string Id { get; set; } = "";
    public string Skill { get; set; } = "";
    public string ExamTitle { get; set; } = "";
    public string ExamUrl { get; set; } = "";
    public string? SessionId { get; set; }
    public DateTimeOffset SubmittedAt { get; set; }
    public bool TimedOut { get; set; }
    public int DurationSeconds { get; set; }
    public string Status { get; set; } = "Submitted";
    public WritingSubmissionData? Writing { get; set; }
    public SpeakingSubmissionData? Speaking { get; set; }
    public IeltsScoreReport? Score { get; set; }
}

public sealed class WritingSubmissionData
{
    public List<WritingTaskSubmission> Tasks { get; set; } = new();
}

public sealed class WritingTaskSubmission
{
    public int TaskNumber { get; set; }
    public string TaskTitle { get; set; } = "";
    public string Prompt { get; set; } = "";
    public string Answer { get; set; } = "";
    public int WordCount { get; set; }
    public int MinWords { get; set; }
}

public sealed class SpeakingSubmissionData
{
    public List<SpeakingAnswerSubmission> Answers { get; set; } = new();
}

public sealed class SpeakingAnswerSubmission
{
    public int PartNumber { get; set; }
    public string PartTitle { get; set; } = "";
    public int QuestionId { get; set; }
    public string Question { get; set; } = "";
    public string Transcript { get; set; } = "";
    public double DurationMs { get; set; }
    public SpeakingAnswerScore? Score { get; set; }
}

public sealed class SpeakingAnswerScore
{
    public int Fluency { get; set; }
    public int Lexical { get; set; }
    public int Grammar { get; set; }
    public int Coherence { get; set; }
    public double Overall { get; set; }
    public int Wpm { get; set; }
    public int WordCount { get; set; }
}

public sealed class IeltsScoreReport
{
    public double Overall { get; set; }
    public string Summary { get; set; } = "";
    public List<IeltsCriterionScore> Criteria { get; set; } = new();
}

public sealed class IeltsCriterionScore
{
    public string Name { get; set; } = "";
    public double Score { get; set; }
    public string Feedback { get; set; } = "";
}
