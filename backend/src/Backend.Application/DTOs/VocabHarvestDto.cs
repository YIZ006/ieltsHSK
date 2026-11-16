using System.Collections.Generic;

namespace Backend.Application.DTOs;

public class VocabHarvestRequest
{
    public string Text { get; set; } = string.Empty;
    public string TargetLevel { get; set; } = "All"; // "All", "A1", "A2", "B1", "B2", "C1", "C2"
}

public class VocabHarvestItemDto
{
    public string WordOrPhrase { get; set; } = string.Empty;
    public string Type { get; set; } = "collocation"; // "word", "phrasal_verb", "idiom", "collocation", "academic_word"
    public string CefrLevel { get; set; } = "B2";
    public string MeaningVi { get; set; } = string.Empty;
    public string ContextSentence { get; set; } = string.Empty;
    public string ExampleUsage { get; set; } = string.Empty;
}

public class VocabHarvestResponse
{
    public List<VocabHarvestItemDto> Items { get; set; } = new();
}
