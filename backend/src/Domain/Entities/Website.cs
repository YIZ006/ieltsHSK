namespace Backend.Domain.Entities;

public class Website
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Description { get; set; }
    
    public int LanguageId { get; set; }
    public Language Language { get; set; } = null!;

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public bool IsFree { get; set; } = true;
    public string? RecommendedLevel { get; set; } // e.g., 'HSK 3-4', 'Beginner', 'IELTS 6.0'
    public string? ThumbnailUrl { get; set; }
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
