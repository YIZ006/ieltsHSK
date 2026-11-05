namespace Backend.Application.DTOs;

public record ReviewWordItemInput(
    int? Id,
    string Hanzi,
    string? Pinyin,
    string? Meaning,
    string? HskLevel,
    string? WordType = null,
    string? ExampleSentence = null,
    string? ExamplePinyin = null,
    string? ExampleMeaning = null,
    string? AudioUrl = null
);

public record AddHskReviewWordsRequest(
    List<int>? VocabularyIds = null,
    List<ReviewWordItemInput>? Words = null,
    string? Source = "HskShooter"
);

public record HskReviewWordDto
{
    public int Id { get; set; }
    public int VocabularyId { get; set; }
    public string HskLevel { get; set; } = string.Empty;
    public string Hanzi { get; set; } = string.Empty;
    public string Pinyin { get; set; } = string.Empty;
    public string Meaning { get; set; } = string.Empty;
    public string? WordType { get; set; }
    public string? ExampleSentence { get; set; }
    public string? ExamplePinyin { get; set; }
    public string? ExampleMeaning { get; set; }
    public string? AudioUrl { get; set; }
    public string? Source { get; set; }
    public DateTime AddedAt { get; set; }
}

public record AddHskReviewWordsResponse(int AddedCount, int TotalReviewWords, string Message);
