namespace Backend.Domain.Entities;

public class Story
{
    public int Id { get; set; }
    
    // Tiêu đề truyện (vd: "The School Race", "The Mountain That Learned to Speak")
    public required string Title { get; set; }
    
    // Đường dẫn thân thiện (vd: "the-school-race")
    public required string Slug { get; set; }
    
    // Cấp độ chuẩn CEFR: A1, A2, B1, B2, C1, C2
    public string Level { get; set; } = "B1";
    
    // Band điểm IELTS tương đương (vd: "5.0 - 6.0")
    public string IeltsBand { get; set; } = "5.0 - 6.0";
    
    // Chủ đề: Đời sống, Khoa học, Xã hội, Lịch sử, Tự nhiên, Công nghệ...
    public string Category { get; set; } = "Đời sống";
    
    // Tóm tắt ngắn gọn (~1-2 câu)
    public string? Summary { get; set; }
    
    // Link ảnh thumbnail bìa truyện
    public string? ThumbnailUrl { get; set; }
    
    // Link audio thu âm giọng bản xứ (tùy chọn)
    public string? AudioUrl { get; set; }
    
    // Đường dẫn file JSON nội dung truyện trên Cloudflare R2 (vd: https://pub-xxx.r2.dev/stories/xxx.json)
    public string? JsonUrl { get; set; }
    
    // Tổng số lượng từ trong bài
    public int WordCount { get; set; }
    
    // Thời gian đọc ước tính (phút, mặc định 3-5 phút)
    public int EstimatedMinutes { get; set; } = 5;
    
    // JSON lưu cấu trúc đoạn văn, câu tiếng Anh & câu dịch tiếng Việt
    // Format: [{"en":"...","vi":"..."}]
    public string ContentJson { get; set; } = "[]";
    
    // JSON lưu danh sách từ vựng mục tiêu (Target Vocabulary)
    // Format: [{"word":"whisper","phonetic":"/ˈwɪs.pər/","pos":"verb","meaning":"thì thầm","example":"...","collocations":["..."]}]
    public string VocabularyJson { get; set; } = "[]";
    
    // JSON lưu câu hỏi đọc hiểu trắc nghiệm cuối truyện
    // Format: [{"question":"...","options":["A","B","C","D"],"correctIndex":0,"explanation":"..."}]
    public string QuestionsJson { get; set; } = "[]";
    
    // Trạng thái xuất bản
    public bool IsPublished { get; set; } = true;
    
    // Lượt xem/đọc
    public int ViewsCount { get; set; } = 0;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
