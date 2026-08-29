namespace Backend.Domain.Entities;

public class TestSubmission
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public User? User { get; set; }
    public string? StudentName { get; set; }
    public string? UserEmail { get; set; }
    public string? SessionId { get; set; }
    
    // E.g., "Listening", "Reading", "Writing", "Speaking"
    public string Skill { get; set; } = string.Empty;
    
    // The test URL or title to identify which test was taken
    public string ExamUrl { get; set; } = string.Empty;
    public string? ExamTitle { get; set; }
    public int AttemptNumber { get; set; } = 1;
    
    // Overall score, e.g., 7.5
    public double BandScore { get; set; }
    
    // Specific to Listening/Reading
    public int CorrectCount { get; set; }
    public int TotalCount { get; set; }
    
    // JSON details (could be the list of answers or criteria)
    public string? DetailsJson { get; set; }

    // R2 private storage key for the submission JSON file
    public string? R2StorageKey { get; set; }

    // Status: "Pending", "Graded", "Scored", "Reviewed"
    public string Status { get; set; } = "Pending";
    
    // Teacher / AI feedback
    public string? TeacherFeedback { get; set; }

    // R2 private storage key for speaking audio
    public string? AudioKey { get; set; }

    public DateTimeOffset SubmittedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? GradedAt { get; set; }
}
