namespace Backend.Domain.Entities;

public class HskReviewWord
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int VocabularyId { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    public string? Source { get; set; } // e.g. "HskShooter", "Manual"

    public User? User { get; set; }
    public HskVocabulary? Vocabulary { get; set; }
}
