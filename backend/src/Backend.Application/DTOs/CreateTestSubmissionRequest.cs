namespace Backend.Application.DTOs;

public class CreateTestSubmissionRequest
{
    public int? UserId { get; set; }
    public string? SessionId { get; set; }
    public string Skill { get; set; } = string.Empty;
    public string ExamUrl { get; set; } = string.Empty;
    public double BandScore { get; set; }
    public int CorrectCount { get; set; }
    public int TotalCount { get; set; }
    public string? DetailsJson { get; set; }
}
