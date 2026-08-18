namespace Frontend.App.Models;

/// <summary>
/// Dß╗» liß╗çu to├án bß╗Ö ─æß╗ü thi TOEIC ΓÇö ─æ╞░ß╗úc fetch tß╗½ Cloudflare R2 hoß║╖c sample-data
/// </summary>
public class ToeicExamData
{
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public int TotalTimeSeconds { get; set; } = 7200; // 120 ph├║t
    public int TotalQuestions { get; set; } = 200;
    public List<ToeicPart> Parts { get; set; } = new();
}

/// <summary>
/// Mß╗Öt Part trong ─æß╗ü thi TOEIC (Part 1-7)
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
/// Nh├│m c├óu hß╗Åi trong mß╗Öt Part (c├│ thß╗â c├╣ng h├¼nh ß║únh, audio, hoß║╖c ─æoß║ín v─ân)
/// </summary>
public class ToeicQuestionGroup
{
    public string GroupId { get; set; } = "";
    /// <summary>URL h├¼nh ß║únh (Part 1, Part 3/4 ─æ├┤i khi c├│ ß║únh)</summary>
    public string ImageUrl { get; set; } = "";
    /// <summary>URL audio (Part 1-4)</summary>
    public string AudioUrl { get; set; } = "";
    /// <summary>─Éoß║ín v─ân HTML (Part 6, Part 7)</summary>
    public string PassageHtml { get; set; } = "";
    /// <summary>Ti├¬u ─æß╗ü ─æoß║ín v─ân (Part 7)</summary>
    public string PassageTitle { get; set; } = "";
    /// <summary>Transcript audio Part 3-4, chß╗ë hiß╗ân thß╗ï ß╗ƒ chß║┐ ─æß╗Ö xem ─æ├íp ├ín</summary>
    public string Transcript { get; set; } = "";
    /// <summary>Bß║ún dß╗ïch ─æoß║ín v─ân, hiß╗ân thß╗ï ß╗ƒ chß║┐ ─æß╗Ö xem ─æ├íp ├ín</summary>
    public string Translation { get; set; } = "";
    /// <summary>H╞░ß╗¢ng dß║½n ri├¬ng cho nh├│m</summary>
    public string Instructions { get; set; } = "";
    public List<ToeicQuestion> Questions { get; set; } = new();
}

/// <summary>
/// Mß╗Öt c├óu hß╗Åi TOEIC
/// </summary>
public class ToeicQuestion
{
    public int Id { get; set; }
    /// <summary>Nß╗Öi dung c├óu hß╗Åi (Part 3-7); ─æß╗â trß╗æng vß╗¢i Part 1-2 v├¼ c├óu hß╗Åi trong audio</summary>
    public string Text { get; set; } = "";
    public List<ToeicOption> Options { get; set; } = new();
    public int CorrectOptionId { get; set; }
    public int? SelectedOptionId { get; set; }
}

/// <summary>
/// Lß╗▒a chß╗ìn ─æ├íp ├ín
/// </summary>
public class ToeicOption
{
    public int Id { get; set; }
    /// <summary>Nh├ún: A, B, C, D</summary>
    public string Label { get; set; } = "";
    /// <summary>Nß╗Öi dung ─æ├íp ├ín (Part 5-7); ─æß╗â trß╗æng vß╗¢i Part 1-4 v├¼ ─æß╗ìc tß╗½ audio</summary>
    public string Text { get; set; } = "";
}
