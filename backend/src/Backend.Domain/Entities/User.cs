namespace Backend.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    
    // LMS & Gamification Fields
    public string Role { get; set; } = "user"; // 'user' or 'admin'
    public string? Avatar { get; set; }
    public string Level { get; set; } = "A1";
    public int Xp { get; set; } = 0;
    public int Streak { get; set; } = 0;
    public DateTime? LastActive { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime PasswordChangedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation Properties
    public ICollection<Course> CreatedCourses { get; set; } = new List<Course>();
}
