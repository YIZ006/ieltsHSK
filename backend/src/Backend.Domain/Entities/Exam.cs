namespace Backend.Domain.Entities;

public class Exam
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public required string DataUrl { get; set; }
    
    // Category: "IELTS", "HSK", etc.
    public string Category { get; set; } = "IELTS";
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}
