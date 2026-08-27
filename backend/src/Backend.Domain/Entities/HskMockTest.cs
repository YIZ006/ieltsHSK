namespace Backend.Domain.Entities;

public class HskMockTest
{
    public int Id { get; set; }
    
    // Tên bộ đề, ví dụ: "HSK 3 Chuẩn"
    public required string CollectionName { get; set; }
    
    // Tên bài thi cụ thể, ví dụ: "HSK 3 - Đề số 01"
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
    public string? HskUrl { get; set; } // Link JSON của toàn bộ đề HSK

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}
