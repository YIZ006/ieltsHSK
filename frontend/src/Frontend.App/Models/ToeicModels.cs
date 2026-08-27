namespace Frontend.App.Models;

/// <summary>
/// Dữ liệu toàn bộ đề thi TOEIC — được fetch từ Cloudflare R2 hoặc sample-data
/// </summary>
public class ToeicExamData
{
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public int TotalTimeSeconds { get; set; } = 7200; // 120 phút
    public int TotalQuestions { get; set; } = 200;
    public List<ToeicPart> Parts { get; set; } = new();
}

/// <summary>
/// Một Part trong đề thi TOEIC (Part 1-7)
/// </summary>
public class ToeicPart
{
    public int PartNumber { get; set; }
    public string PartName { get; set; } = "";
    public string PartType { get; set; } = ""; // "Photographs","QuestionResponse","Conversations","Talks","IncompleteSentences","TextCompletion","ReadingComprehension"
    public string Instructions { get; set; } = "";
    public bool HasListening { get; set; } = false;
    public List<ToeicQuestionGroup> QuestionGroups { get; set; } = new();
}

/// <summary>
/// Nhóm câu hỏi trong một Part (có thể cùng hình ảnh, audio, hoặc đoạn văn)
/// </summary>
public class ToeicQuestionGroup
{
    public string GroupId { get; set; } = "";
    /// <summary>URL hình ảnh (Part 1, Part 3/4 đôi khi có ảnh)</summary>
    public string ImageUrl { get; set; } = "";
    /// <summary>URL audio (Part 1-4)</summary>
    public string AudioUrl { get; set; } = "";
    /// <summary>Đoạn văn HTML (Part 6, Part 7)</summary>
    public string PassageHtml { get; set; } = "";
    /// <summary>Tiêu đề đoạn văn (Part 7)</summary>
    public string PassageTitle { get; set; } = "";
    /// <summary>Transcript audio Part 3-4, chỉ hiển thị ở chế độ xem đáp án</summary>
    public string Transcript { get; set; } = "";
    /// <summary>Bản dịch đoạn văn, hiển thị ở chế độ xem đáp án</summary>
    public string Translation { get; set; } = "";
    /// <summary>Hướng dẫn riêng cho nhóm</summary>
    public string Instructions { get; set; } = "";
    public List<ToeicQuestion> Questions { get; set; } = new();
}

/// <summary>
/// Một câu hỏi TOEIC
/// </summary>
public class ToeicQuestion
{
    public int Id { get; set; }
    /// <summary>Nội dung câu hỏi (Part 3-7); để trống với Part 1-2 vì câu hỏi trong audio</summary>
    public string Text { get; set; } = "";
    public List<ToeicOption> Options { get; set; } = new();
    public int CorrectOptionId { get; set; }
    public int? SelectedOptionId { get; set; }
    /// <summary>Cờ đánh dấu câu hỏi (chỉ dùng ở part đọc 5-7, không lưu trong JSON đề).</summary>
    public bool IsFlagged { get; set; }
}

/// <summary>
/// Lựa chọn đáp án
/// </summary>
public class ToeicOption
{
    public int Id { get; set; }
    /// <summary>Nhãn: A, B, C, D</summary>
    public string Label { get; set; } = "";
    /// <summary>Nội dung đáp án (Part 5-7); để trống với Part 1-4 vì đọc từ audio</summary>
    public string Text { get; set; } = "";
}

/// <summary>
/// Một từ vựng TOEIC trong bộ flashcard
/// </summary>
public class ToeicVocabItem
{
    public int Id { get; set; }
    public string Word { get; set; } = "";
    public string Ipa { get; set; } = "";
    public string Meaning { get; set; } = "";
    public string Example { get; set; } = "";
    public string Topic { get; set; } = "";
}

/// <summary>
/// Trạng thái học flashcard (lưu localStorage)
/// </summary>
public class ToeicFlashcardState
{
    public List<int> Learned { get; set; } = new();
    public List<int> Again { get; set; } = new();
}
