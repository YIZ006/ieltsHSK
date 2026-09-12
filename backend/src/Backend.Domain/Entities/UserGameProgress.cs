namespace Backend.Domain.Entities;

public class UserGameProgress
{
    public int Id { get; set; }
    
    public int UserId { get; set; }
    public User? User { get; set; }
    
    // Ví dụ: "HskShooter", "Wordle"
    public string GameType { get; set; } = string.Empty;
    
    // Ví dụ: "HSK1", "HSK2", "IELTS"
    public string Level { get; set; } = string.Empty;
    
    public int CurrentStage { get; set; } = 0;
    public int MaxUnlockedStage { get; set; } = 0;
    public int HighScore { get; set; } = 0;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
