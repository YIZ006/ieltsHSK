namespace Backend.Domain.Entities;

public class ToeicVocabularyProgress
{
    public int Id { get; set; }
    
    public int UserId { get; set; }
    public User? User { get; set; }
    
    public int VocabularyId { get; set; }
    public ToeicVocabulary? Vocabulary { get; set; }
    
    // "Learned" hoặc "Again"
    public string Status { get; set; } = "Learned";
    
    public DateTime LearnedAt { get; set; } = DateTime.UtcNow;
}
