namespace Backend.Application.DTOs;

public class GrammarStructureDto
{
    public int Id { get; set; }
    public string StructureCode { get; set; } = string.Empty;
    public string BandLevel { get; set; } = "7.0 - 8.0";
    public string Category { get; set; } = "Writing Task 2";
    public string GrammarTopic { get; set; } = string.Empty;
    public string Formula { get; set; } = string.Empty;
    public string UsageFunction { get; set; } = string.Empty;
    public string? BasicExample { get; set; }
    public string AdvancedExample { get; set; } = string.Empty;
    public string VietnameseMeaning { get; set; } = string.Empty;
    public string? KeyCollocations { get; set; }
    public string? CommonMistakes { get; set; }
    public string? PracticeExercise { get; set; }
    public string? Tags { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateGrammarStructureDto
{
    public string StructureCode { get; set; } = string.Empty;
    public string BandLevel { get; set; } = "7.0 - 8.0";
    public string Category { get; set; } = "Writing Task 2";
    public string GrammarTopic { get; set; } = string.Empty;
    public string Formula { get; set; } = string.Empty;
    public string UsageFunction { get; set; } = string.Empty;
    public string? BasicExample { get; set; }
    public string AdvancedExample { get; set; } = string.Empty;
    public string VietnameseMeaning { get; set; } = string.Empty;
    public string? KeyCollocations { get; set; }
    public string? CommonMistakes { get; set; }
    public string? PracticeExercise { get; set; }
    public string? Tags { get; set; }
    public int DisplayOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
}

public class UpdateGrammarStructureDto : CreateGrammarStructureDto
{
}

public class GrammarBulkDeleteDto
{
    public List<int> Ids { get; set; } = new();
}

public class GrammarImportExcelResponse
{
    public int TotalRows { get; set; }
    public int Success { get; set; }
    public int Updated { get; set; }
    public int Skipped { get; set; }
    public int Fail { get; set; }
    public List<string> Errors { get; set; } = new();
}
