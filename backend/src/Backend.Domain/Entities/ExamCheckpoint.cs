namespace Backend.Domain.Entities;

public class ExamCheckpoint
{
    public int Id { get; set; }
    
    public int? UserId { get; set; }
    public User? User { get; set; }
    
    public string UserIdentifier { get; set; } = string.Empty;
    public string Skill { get; set; } = string.Empty;
    public string ExamUrl { get; set; } = string.Empty;
    public int? MockTestId { get; set; }
    
    // JSON lưu toàn bộ câu trả lời, trạng thái checkbox/radio, audio progress
    public string CheckpointDataJson { get; set; } = string.Empty;
    
    public int SecondsRemaining { get; set; }
    public DateTime LastSavedAt { get; set; } = DateTime.UtcNow;
}
