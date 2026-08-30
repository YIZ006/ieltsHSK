namespace Backend.Application.DTOs;

public class ExamDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string DataUrl { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
