namespace Backend.Domain.Entities;

public class IeltsVocabularyProgress
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int VocabularyId { get; set; }
    public string Status { get; set; } = "Learned";
    public DateTime LearnedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public IeltsVocabulary Vocabulary { get; set; } = null!;
}
