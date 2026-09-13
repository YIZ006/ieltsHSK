namespace Backend.Domain.Entities;

public class ToeicVocabulary
{
    public int Id { get; set; }
    public string Word { get; set; } = string.Empty;
    public string Ipa { get; set; } = string.Empty;
    public string Meaning { get; set; } = string.Empty;
    public string? Example { get; set; }
    public string Topic { get; set; } = "Khác";
    public bool IsCustom { get; set; } = false;
    
    // Nếu từ vựng do người dùng tự thêm thì lưu UserId, null nếu là từ chuẩn hệ thống
    public int? UserId { get; set; }
    public User? User { get; set; }
    
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public ICollection<ToeicVocabularyProgress> Progresses { get; set; } = new List<ToeicVocabularyProgress>();
}
