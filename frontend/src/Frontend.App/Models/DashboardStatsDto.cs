namespace Frontend.App.Models;

public class DashboardStatsDto
{
    // User Statistics
    public int TotalUsers { get; set; }
    public int ActiveNow { get; set; }
    public int ActiveToday { get; set; }
    public int ActiveThisWeek { get; set; }
    public int NewUsersToday { get; set; }
    public int NewUsersThisMonth { get; set; }
    public int TotalAdmins { get; set; }
    public int TotalStudents { get; set; }
    public int LockedUsers { get; set; }

    // Submissions
    public int TotalSubmissions { get; set; }
    public int SubmissionsToday { get; set; }
    public int PendingGrading { get; set; }
    public int GradedSubmissions { get; set; }

    // Mock Tests
    public int TotalIeltsTests { get; set; }
    public int TotalToeicTests { get; set; }
    public int TotalHskTests { get; set; }

    // Content Materials
    public int TotalIeltsVocab { get; set; }
    public int TotalHskVocab { get; set; }
    public int TotalStories { get; set; }
    public int TotalListenVideos { get; set; }
    public int PendingListenVideos { get; set; }

    // Recent Submissions & Users
    public List<RecentSubmissionDto> RecentSubmissions { get; set; } = new();
    public List<RecentUserDto> RecentUsers { get; set; } = new();

    public DateTime ServerTime { get; set; } = DateTime.UtcNow;
}

public class RecentSubmissionDto
{
    public int Id { get; set; }
    public string? StudentName { get; set; }
    public string? UserEmail { get; set; }
    public string Skill { get; set; } = string.Empty;
    public string? ExamTitle { get; set; }
    public double BandScore { get; set; }
    public int CorrectCount { get; set; }
    public int TotalCount { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTimeOffset SubmittedAt { get; set; }
}

public class RecentUserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "user";
    public string Level { get; set; } = "A1";
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }
    public DateTime? LastActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ChartAnalyticsDto
{
    public string Range { get; set; } = "30d";
    public string Granularity { get; set; } = "day";
    public int TotalRequests { get; set; }
    public long TotalTokens { get; set; }
    public double TotalCost { get; set; }
    public double ErrorRate { get; set; }
    public List<ChartTimePointDto> TimePoints { get; set; } = new();
    public List<ModelBreakdownItemDto> ModelBreakdown { get; set; } = new();
}

public class ChartTimePointDto
{
    public string Date { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string FullDate { get; set; } = string.Empty;
    public int Requests { get; set; }
    public long Tokens { get; set; }
    public double Cost { get; set; }
    public int Errors { get; set; }
}

public class ModelBreakdownItemDto
{
    public string ItemName { get; set; } = string.Empty;
    public long Tokens { get; set; }
    public double Percentage { get; set; }
    public string Icon { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
}
