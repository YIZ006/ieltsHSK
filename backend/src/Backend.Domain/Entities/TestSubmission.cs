namespace Backend.Domain.Entities;

public class TestSubmission
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public string? SessionId { get; set; }
    
    // E.g., "Listening", "Reading", "Writing", "Speaking"
    public string Skill { get; set; } = string.Empty;
    
    // The test URL or title to identify which test was taken
    public string ExamUrl { get; set; } = string.Empty;
    
    // Overall score, e.g., 7.5
    public double BandScore { get; set; }
    
    // Specific to Listening/Reading
    public int CorrectCount { get; set; }
    public int TotalCount { get; set; }
    
    // JSON details (could be the list of answers or criteria)
    public string? DetailsJson { get; set; }

    public DateTimeOffset SubmittedAt { get; set; } = DateTimeOffset.UtcNow;
}
