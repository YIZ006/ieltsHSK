namespace Backend.Domain.Entities;

public class GrammarStructure
{
    public int Id { get; set; }
    public string StructureCode { get; set; } = string.Empty; // W_INV_01, W_NOM_01, ...
    public string BandLevel { get; set; } = "7.0 - 8.0";      // 5.0 - 6.0, 6.5 - 7.0, 7.5 - 8.5, etc.
    public string Category { get; set; } = "Writing Task 2";  // Writing Task 1, Writing Task 2, Speaking, General
    public string GrammarTopic { get; set; } = string.Empty;  // Đảo ngữ, Câu chẻ, Danh từ hóa, Mệnh đề phân từ...
    public string Formula { get; set; } = string.Empty;       // Not only + Aux + S + V, but S + also + V
    public string UsageFunction { get; set; } = string.Empty; // Nhấn mạnh tác động kép, nâng cao điểm GRA
    public string? BasicExample { get; set; }                 // Câu gốc Band 5.0 - 5.5
    public string AdvancedExample { get; set; } = string.Empty;// Câu nâng cấp Band 7.5 - 8.5
    public string VietnameseMeaning { get; set; } = string.Empty; // Bản dịch nghĩa Tiếng Việt
    public string? KeyCollocations { get; set; }              // Từ vựng / Collocation học thuật đi kèm
    public string? CommonMistakes { get; set; }               // Lỗi sai kinh điển hay mắc phải
    public string? PracticeExercise { get; set; }             // Bài tập viết lại câu
    public string? Tags { get; set; }                         // Từ khóa lọc
    public int DisplayOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
