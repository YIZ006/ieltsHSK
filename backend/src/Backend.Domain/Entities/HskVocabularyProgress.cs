namespace Backend.Domain.Entities;

public class HskVocabularyProgress
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int VocabularyId { get; set; }
    public DateTime LearnedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}
