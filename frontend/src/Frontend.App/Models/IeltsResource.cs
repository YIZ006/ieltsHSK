namespace Frontend.App.Models;

public class CourseDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Thumbnail { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
}

public class WebsiteDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsFree { get; set; }
    public string? RecommendedLevel { get; set; }
    public string? ThumbnailUrl { get; set; }
}

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

public class UpdateListenVideoRequest
{
    public string Title { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}

