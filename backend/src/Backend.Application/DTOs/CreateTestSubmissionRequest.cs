namespace Backend.Application.DTOs;

public class CreateTestSubmissionRequest
{
    public int? UserId { get; set; }
    public string? StudentName { get; set; }
    public string? UserEmail { get; set; }
    public string? SessionId { get; set; }
    public string Skill { get; set; } = string.Empty;
    public string ExamUrl { get; set; } = string.Empty;
    public string? ExamTitle { get; set; }
    public int? AttemptNumber { get; set; }
    public double BandScore { get; set; }
    public int CorrectCount { get; set; }
    public int TotalCount { get; set; }
    public string? DetailsJson { get; set; }
    public string Status { get; set; } = "Pending";
    public string? TeacherFeedback { get; set; }
    public string? AudioKey { get; set; }
    public string? R2StorageKey { get; set; }
}
