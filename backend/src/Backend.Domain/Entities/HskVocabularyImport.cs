namespace Backend.Domain.Entities;

public class HskVocabularyImport
{
    public int Id { get; set; }
    public required string FileName { get; set; }
    public required string JsonUrl { get; set; }
    public int TotalRows { get; set; }
    public int ImportedCount { get; set; }
    public int UpdatedCount { get; set; }
    public int DuplicateCount { get; set; }
    public int FailedCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
