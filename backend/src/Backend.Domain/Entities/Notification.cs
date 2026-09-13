namespace Backend.Domain.Entities;

public class Notification
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "system"; // system, exam, update, reminder, reward
    public string Icon { get; set; } = "bi-bell-fill";
    public string? TargetUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedByAdmin { get; set; }
    public bool IsBroadcast { get; set; } = true;
    public int? UserId { get; set; }
    public User? User { get; set; }
    public bool IsActive { get; set; } = true;
}
