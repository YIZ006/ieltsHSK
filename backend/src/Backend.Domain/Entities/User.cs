namespace Backend.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    
    // LMS & Gamification Fields
    public string Role { get; set; } = "user"; // 'user' or 'admin'
    public string? Avatar { get; set; }
    public string? AvatarColor { get; set; }
    public string? Bio { get; set; }
    public string? TargetExam { get; set; }
    public string? TargetScore { get; set; }
    public DateTime? TargetDeadline { get; set; }
    public string? IeltsLevel { get; set; }
    public string? HskLevel { get; set; }
    public string Level { get; set; } = "A1";
    public int Streak { get; set; } = 0;
    public DateTime? LastActive { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime PasswordChangedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UsernameChangedAt { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation Properties
    public ICollection<Course> CreatedCourses { get; set; } = new List<Course>();
    public ICollection<TestSubmission> TestSubmissions { get; set; } = new List<TestSubmission>();
    public ICollection<HskVocabularyProgress> HskVocabularyProgresses { get; set; } = new List<HskVocabularyProgress>();
    public ICollection<IeltsVocabularyProgress> IeltsVocabularyProgresses { get; set; } = new List<IeltsVocabularyProgress>();
    public ICollection<ToeicVocabularyProgress> ToeicVocabularyProgresses { get; set; } = new List<ToeicVocabularyProgress>();
    public ICollection<UserStudyActivity> StudyActivities { get; set; } = new List<UserStudyActivity>();
    public ICollection<UserGameProgress> GameProgresses { get; set; } = new List<UserGameProgress>();
    public ICollection<ListenVideo> ListenVideos { get; set; } = new List<ListenVideo>();
}
