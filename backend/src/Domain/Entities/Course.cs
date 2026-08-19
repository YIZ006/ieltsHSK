namespace Backend.Domain.Entities;

public class Course
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Thumbnail { get; set; }
    public string Level { get; set; } = "A1"; // 'A1', 'A2', 'B1', 'B2', 'C1', 'C2'
    public string Category { get; set; } = "vocabulary"; // 'vocabulary', 'grammar', 'listening', 'speaking', 'reading', 'writing'
    public string Status { get; set; } = "draft"; // 'draft', 'published', 'archived'
    public int DurationMinutes { get; set; } = 0;
    public int Order { get; set; } = 0;
    
    public int CreatedById { get; set; }
    public User CreatedBy { get; set; } = null!;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation Property
    public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
}
