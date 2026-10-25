namespace Backend.Application.DTOs;

public class SlangVocabDto
{
    public int Id { get; set; }
    public string Term { get; set; } = string.Empty;
    public string Pinyin { get; set; } = string.Empty;
    public string MeaningVi { get; set; } = string.Empty;
    public string Category { get; set; } = "slang_trend";
    public string? Origin { get; set; }
    public string? DialogueExampleZh { get; set; }
    public string? DialogueExampleVi { get; set; }
    public string? AudioUrl { get; set; }
    public int PopularityScore { get; set; } = 90;
    public int DisplayOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateSlangVocabRequest
{
    public string Term { get; set; } = string.Empty;
    public string Pinyin { get; set; } = string.Empty;
    public string MeaningVi { get; set; } = string.Empty;
    public string Category { get; set; } = "slang_trend";
    public string? Origin { get; set; }
    public string? DialogueExampleZh { get; set; }
    public string? DialogueExampleVi { get; set; }
    public string? AudioUrl { get; set; }
    public int? PopularityScore { get; set; } = 90;
    public int? DisplayOrder { get; set; } = 0;
    public bool? IsActive { get; set; } = true;
}

public class UpdateSlangVocabRequest : CreateSlangVocabRequest
{
}

public class ImportSlangVocabJsonRequest
{
    public string JsonContent { get; set; } = string.Empty;
}
