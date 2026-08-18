namespace Backend.Domain.Entities;

public class MockTest
{
    public int Id { get; set; }
    
    // Tên bộ đề, ví dụ: "IELTS Mock Test 2025 December"
    public required string CollectionName { get; set; }
    
    // Tên bài thi cụ thể, ví dụ: "Practise Test 1"
    public required string Title { get; set; }

    // Đường dẫn file JSON/Media trên Cloudflare R2
    public string? ListeningUrl { get; set; }
    public string? ReadingUrl { get; set; }
    public string? WritingUrl { get; set; }
    public string? SpeakingUrl { get; set; }
    public string? ListeningAnswerUrl { get; set; }
    public string? ReadingAnswerUrl { get; set; }
    public string? WritingAnswerUrl { get; set; }
    public string? SpeakingAnswerUrl { get; set; }
    public string? ToeicUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}
