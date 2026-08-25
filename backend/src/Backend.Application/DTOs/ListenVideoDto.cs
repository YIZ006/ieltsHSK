namespace Backend.Application.DTOs;

public class ListenVideoDto
{
    public int Id { get; set; }
    public string YoutubeUrl { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string ChannelName { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool IsApproved { get; set; }
    public string? TranscriptUrl { get; set; }
    public int WordCount { get; set; }
    public DateTime SubmittedAt { get; set; }
}

public class ListenVideoSubmitRequest
{
    public string YoutubeUrl { get; set; } = string.Empty;
}

public class ManualTranscriptRequest
{
    public string TranscriptText { get; set; } = string.Empty;
}

public class UpdateListenVideoRequest
{
    public string Title { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}

