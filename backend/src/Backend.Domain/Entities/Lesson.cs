namespace Backend.Domain.Entities;

public class Lesson
{
    public int Id { get; set; }
    
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
    
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Content { get; set; }
    
    public string Type { get; set; } = "reading"; // 'video', 'reading', 'exercise', 'quiz'
    public string? VideoUrl { get; set; }
    
    public int DurationMinutes { get; set; } = 0;
    public int Order { get; set; } = 0;
    public string Status { get; set; } = "draft"; // 'draft', 'published'
    
    public int XpReward { get; set; } = 10;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
