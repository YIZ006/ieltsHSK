namespace Backend.Domain.Entities;

public class IeltsVocabulary
{
    public int Id { get; set; }
    public required string Word { get; set; }
    public string? Phonetic { get; set; }        // IPA: /əˈtʃiːv/
    public string? PartOfSpeech { get; set; }    // noun, verb, adj, adv, phrase...
    public required string Meaning { get; set; } // Nghĩa tiếng Việt
    public string? Example { get; set; }
    public string? ExampleMeaning { get; set; }
    public string? Topic { get; set; }           // Environment, Education, Technology...
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
