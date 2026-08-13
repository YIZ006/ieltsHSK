namespace Frontend.App.Models;

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
}

public class UploadResponse
{
    public string Url { get; set; } = string.Empty;
}
