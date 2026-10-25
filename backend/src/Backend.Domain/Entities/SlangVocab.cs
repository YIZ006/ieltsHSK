namespace Backend.Domain.Entities;

public class SlangVocab
{
    public int Id { get; set; }

    /// <summary>
    /// Thuật ngữ / Mật mã (vd: "520", "1314", "996", "躺平", "摆烂", "显眼包", "种草", "破防", "社恐/社牛")
    /// </summary>
    public required string Term { get; set; }

    /// <summary>
    /// Phiên âm Pinyin (vd: "wǔ èr líng", "tǎng píng", "bǎi làn", "xiǎn yǎn bāo")
    /// </summary>
    public required string Pinyin { get; set; }

    /// <summary>
    /// Ý nghĩa tiếng Việt chi tiết
    /// </summary>
    public required string MeaningVi { get; set; }

    /// <summary>
    /// Phân loại: "number_cipher" (Mật mã số mạng), "slang_trend" (Từ lóng / Hot trend mạng xã hội)
    /// </summary>
    public string Category { get; set; } = "slang_trend";

    /// <summary>
    /// Nguồn gốc / Ngữ cảnh xuất xứ / Giải thích hiện tượng ngữ âm
    /// </summary>
    public string? Origin { get; set; }

    /// <summary>
    /// Câu đối thoại ví dụ thực tế chữ Hán
    /// </summary>
    public string? DialogueExampleZh { get; set; }

    /// <summary>
    /// Dịch nghĩa câu đối thoại ví dụ tiếng Việt
    /// </summary>
    public string? DialogueExampleVi { get; set; }

    /// <summary>
    /// Đường dẫn audio phát âm
    /// </summary>
    public string? AudioUrl { get; set; }

    /// <summary>
    /// Điểm thịnh hành / Độ hot (1 - 100)
    /// </summary>
    public int PopularityScore { get; set; } = 90;

    /// <summary>
    /// Thứ tự hiển thị ưu tiên
    /// </summary>
    public int DisplayOrder { get; set; } = 0;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}
