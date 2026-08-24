namespace Backend.Domain.Entities;

public class UserActivityLog
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Action { get; set; } = string.Empty; // "login", "login_google", "password_reset", "disabled", "enabled"
    public string? Detail { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User User { get; set; } = null!;
}
