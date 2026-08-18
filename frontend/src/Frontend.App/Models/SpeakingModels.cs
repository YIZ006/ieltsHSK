namespace Frontend.App.Models;

public class SpeakingExamData
{
    public string Title { get; set; } = "";
    public int TotalMinutes { get; set; } = 14;
    public List<SpeakingPart> Parts { get; set; } = new();
}

public class SpeakingPart
{
    public int PartNumber { get; set; }
    public string PartTitle { get; set; } = "";
    public string Caption { get; set; } = "";
    public int TimerSeconds { get; set; }
    public int ThinkTimeSeconds { get; set; }
    public bool IsGridLayout { get; set; }
    public string? TopicDescription { get; set; }
    public List<SpeakingQuestion> Questions { get; set; } = new();
}

public class SpeakingQuestion
{
    public int Id { get; set; }
    public string Question { get; set; } = "";
    public string VideoUrl { get; set; } = "";
}
