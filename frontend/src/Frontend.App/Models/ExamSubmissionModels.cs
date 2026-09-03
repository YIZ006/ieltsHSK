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
    public int? MockTestId { get; set; }
    public string? CollectionName { get; set; }
    public string? TestTitle { get; set; }
    public string? StudentName { get; set; }
    public int AttemptNumber { get; set; } = 1;
    public string? R2StorageKey { get; set; }
    public double? BandScore { get; set; }
    public string? AnswerUrl { get; set; }
    public GradingResultRecord? Grading { get; set; }
    public WritingSubmissionData? Writing { get; set; }
    public SpeakingSubmissionData? Speaking { get; set; }
    public IeltsScoreReport? Score { get; set; }
    public string? TeacherFeedback { get; set; }

    // Điểm riêng cho TOEIC (null với bài IELTS cũ)
    public int? CorrectCount { get; set; }
    public int? TotalQuestions { get; set; }
    public int? ListeningScore { get; set; }
    public int? ReadingScore { get; set; }
    public int? TotalScore { get; set; }
}

public sealed class GradingResultRecord
{
    public int CorrectCount { get; set; }
    public int TotalCount { get; set; }
    public double BandScore { get; set; }
    public Dictionary<int, QuestionResultRecord> Questions { get; set; } = new();
}

public sealed class QuestionResultRecord
{
    public int QuestionNumber { get; set; }
    public string StudentAnswer { get; set; } = "";
    public string CorrectAnswer { get; set; } = "";
    public List<string> AcceptedAnswers { get; set; } = new();
    public bool IsCorrect { get; set; }
    public bool IsBlank { get; set; }
}

public sealed class MockTestSummaryModel
{
    public string CollectionName { get; set; } = "";
    public string TestTitle { get; set; } = "";
    public string? SessionId { get; set; }
    public double OverallBandScore { get; set; }
    public int CompletedSkillsCount { get; set; }
    public int TotalSkillsCount => 4;
    public DateTimeOffset? LastAttemptAt { get; set; }
    
    public SkillSummaryItem? Listening { get; set; }
    public SkillSummaryItem? Reading { get; set; }
    public SkillSummaryItem? Writing { get; set; }
    public SkillSummaryItem? Speaking { get; set; }
}

public sealed class SkillSummaryItem
{
    public string Skill { get; set; } = "";
    public bool IsCompleted { get; set; }
    public bool IsGraded { get; set; }
    public string Status { get; set; } = "Pending";
    public string? TeacherFeedback { get; set; }
    public double BandScore { get; set; }
    public int? CorrectCount { get; set; }
    public int? TotalCount { get; set; }
    public int DurationSeconds { get; set; }
    public DateTimeOffset SubmittedAt { get; set; }
    public string ExamUrl { get; set; } = "";
    public string? AnswerUrl { get; set; }
    public string? SubmissionId { get; set; }
    public IeltsScoreReport? ScoreReport { get; set; }
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

public sealed class SubmissionSummaryDto
{
    public string Skill { get; set; } = "";
    public double BandScore { get; set; }
    public int CorrectCount { get; set; }
    public int TotalCount { get; set; }
    public DateTimeOffset SubmittedAt { get; set; }
}

public sealed class TestSubmissionSyncDto
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public string? StudentName { get; set; }
    public string? SessionId { get; set; }
    public string Skill { get; set; } = "";
    public string ExamUrl { get; set; } = "";
    public string? ExamTitle { get; set; }
    public double BandScore { get; set; }
    public int CorrectCount { get; set; }
    public int TotalCount { get; set; }
    public string? DetailsJson { get; set; }
    public string Status { get; set; } = "Pending";
    public string? TeacherFeedback { get; set; }
    public DateTimeOffset SubmittedAt { get; set; }
}
