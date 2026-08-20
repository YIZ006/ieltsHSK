namespace Backend.Domain.Entities;

public class ListenVideo
{
    public int Id { get; set; }
    public string YoutubeUrl { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string ChannelName { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty; // e.g., A1, B1, B2
    public string Category { get; set; } = string.Empty; // e.g., IELTS, Giao tiếp
    
    public bool IsApproved { get; set; } = false; // Admin approval status
    
    public string? TranscriptUrl { get; set; } // Link to JSON transcript on R2
    public int WordCount { get; set; } = 0;
    
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public string SubmittedByUserId { get; set; } = string.Empty;
}
