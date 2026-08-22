namespace Backend.Domain.Entities;

public class HskVocabulary
{
    public int Id { get; set; }
    public required string HskLevel { get; set; } // HSK1-HSK9
    public required string Hanzi { get; set; }
    public required string Pinyin { get; set; }
    public required string Meaning { get; set; }
    public string? WordType { get; set; } // noun, verb, adj, adv, prep, conj, classifier, phrase
    public string? ExampleSentence { get; set; }
    public string? ExamplePinyin { get; set; }
    public string? ExampleMeaning { get; set; }
    public string? AudioUrl { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}