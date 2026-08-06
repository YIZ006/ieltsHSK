namespace Backend.Domain.Entities;

public class LearningResource
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required string Url { get; set; }
    public string? ImageUrl { get; set; }
    
    // Category: "IELTS", "HSK", "General"
    public required string Category { get; set; } 
    
    // Optional: for sorting or grouping
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
