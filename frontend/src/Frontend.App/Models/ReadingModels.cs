namespace Frontend.App.Models;

/// <summary>
/// Dữ liệu toàn bộ đề thi Reading — được fetch từ Cloudflare R2 qua URL
/// </summary>
public class ExamData
{
    public string Title { get; set; } = "";
    public string AudioUrl { get; set; } = "";
    /// <summary>3 Part của bài thi</summary>
    public List<ExamPart> Parts { get; set; } = new();
}

/// <summary>Một Part (phần) của bài thi, bao gồm bài đọc và danh sách câu hỏi</summary>
public class ExamPart
{
    public int PartNumber { get; set; }
    public string PassageTitle { get; set; } = "";
    public string PassageHtml { get; set; } = "";
    public List<QuestionGroup> QuestionGroups { get; set; } = new();
}

public class QuestionGroup
{
    public string Instruction { get; set; } = "";
    public string GroupType { get; set; } = "Normal"; // "Normal", "HtmlBlock"
    public string GroupHtml { get; set; } = "";
    public List<QuestionData> Questions { get; set; } = new();
}

public class QuestionData
{
    public int Id { get; set; }
    public string Text { get; set; } = "";
    public List<OptionData> Options { get; set; } = new();
    public int CorrectOptionId { get; set; }
    public int? SelectedOptionId { get; set; }
    /// <summary>Dành cho dạng fill-in-blank (không có lựa chọn)</summary>
    public string FillAnswer { get; set; } = "";
}

public class OptionData
{
    public int Id { get; set; }
    public string Label { get; set; } = "";
    public string Text { get; set; } = "";
}
