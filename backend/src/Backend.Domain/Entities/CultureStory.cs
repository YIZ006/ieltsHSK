namespace Backend.Domain.Entities;

public class CultureStory
{
    public int Id { get; set; }

    /// <summary>
    /// Tiêu đề chữ Hán (vd: "塞翁失马", "年兽的传说", "端午节与屈原")
    /// </summary>
    public required string TitleZh { get; set; }

    /// <summary>
    /// Tiêu đề tiếng Việt (vd: "Tái Ông Mất Ngựa", "Truyền Thuyết Niên Thú")
    /// </summary>
    public required string TitleVi { get; set; }

    /// <summary>
    /// Phiên âm Pinyin của tiêu đề (vd: "Sài Wēng Shī Mǎ")
    /// </summary>
    public required string Pinyin { get; set; }

    /// <summary>
    /// Đường dẫn thân thiện SEO (vd: "sai-weng-shi-ma-tai-ong-mat-ngua")
    /// </summary>
    public required string Slug { get; set; }

    /// <summary>
    /// Phân loại chủ đề: "chengyu", "history", "custom", "comic"
    /// </summary>
    public string Category { get; set; } = "chengyu";

    /// <summary>
    /// Cấp độ tương đương: "HSK 2", "HSK 3", "HSK 4", "HSK 5", "Intermediate", "All"
    /// </summary>
    public string Level { get; set; } = "HSK 3";

    /// <summary>
    /// Tóm tắt ngắn gọn ý nghĩa tiếng Việt
    /// </summary>
    public string? SummaryVi { get; set; }

    /// <summary>
    /// Toàn văn nội dung chữ Hán
    /// </summary>
    public required string ContentZh { get; set; }

    /// <summary>
    /// Toàn văn dịch nghĩa tiếng Việt
    /// </summary>
    public required string ContentVi { get; set; }

    /// <summary>
    /// JSON cấu trúc từng câu hoặc đoạn văn gồm chữ Hán, Pinyin và dịch nghĩa:
    /// Format: [{"index":1,"zh":"...","pinyin":"...","vi":"..."}]
    /// </summary>
    public string ParagraphsJson { get; set; } = "[]";

    /// <summary>
    /// JSON danh sách từ vựng trọng tâm trong bài:
    /// Format: [{"word":"边塞","pinyin":"biānsài","meaning":"biên ải","sinoVi":"Biên tái"}]
    /// </summary>
    public string VocabularyJson { get; set; } = "[]";

    /// <summary>
    /// JSON danh sách câu hỏi đọc hiểu trắc nghiệm:
    /// Format: [{"question":"...","options":["..."],"correctIndex":0,"explanation":"..."}]
    /// </summary>
    public string QuestionsJson { get; set; } = "[]";

    /// <summary>
    /// Đường dẫn file âm thanh phát âm mẫu chuẩn (R2 storage hoặc local)
    /// </summary>
    public string? AudioUrl { get; set; }

    /// <summary>
    /// Ảnh bìa minh họa
    /// </summary>
    public string? ThumbnailUrl { get; set; }

    /// <summary>
    /// Thời gian đọc ước tính (phút)
    /// </summary>
    public int EstimatedMinutes { get; set; } = 3;

    /// <summary>
    /// Lượt xem / đọc
    /// </summary>
    public int ViewsCount { get; set; } = 0;

    public bool IsPublished { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}
