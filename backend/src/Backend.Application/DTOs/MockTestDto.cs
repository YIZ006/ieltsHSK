namespace Backend.Application.DTOs;

public class MockTestDto
{
    public int Id { get; set; }
    public string CollectionName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? ListeningUrl { get; set; }
    public string? ReadingUrl { get; set; }
    public string? WritingUrl { get; set; }
    public string? SpeakingUrl { get; set; }
    public string? ListeningAnswerUrl { get; set; }
    public string? ReadingAnswerUrl { get; set; }
    public string? WritingAnswerUrl { get; set; }
    public string? SpeakingAnswerUrl { get; set; }
    public string? ToeicUrl { get; set; }
    public string? HskUrl { get; set; }
}

public class CreateMockTestRequest
{
    public string CollectionName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? ListeningUrl { get; set; }
    public string? ReadingUrl { get; set; }
    public string? WritingUrl { get; set; }
    public string? SpeakingUrl { get; set; }
    public string? ListeningAnswerUrl { get; set; }
    public string? ReadingAnswerUrl { get; set; }
    public string? WritingAnswerUrl { get; set; }
    public string? SpeakingAnswerUrl { get; set; }
    public string? ToeicUrl { get; set; }
    public string? HskUrl { get; set; }
}
